//-----------------------------------------------------------------------
// <copyright file="BotController.cs" company="Phoenix Web Group">
//    BotController
// </copyright>
//-----------------------------------------------------------------------
namespace PwgTelegramBot.Controllers
{
    using System;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web.Mvc;
    using Harvest.Net;
    using Newtonsoft.Json;
    using Models;
    using Models.Telegram;
    using Models.Bot;

    /// <summary>
    /// Controller that gets web hook requests and processes them.
    /// </summary>
    public class BotController : Controller
    {
        // ReSharper disable once InconsistentNaming

        /// <summary>
        /// Set up an object to use with Log4Net logging.
        /// </summary>
        private static readonly log4net.ILog _Log = log4net.LogManager.GetLogger(typeof(BotController));

        /// <summary>
        /// A cron job that is trigged through wget on a cronjob on the mp server under the wquade user.
        /// </summary>
        /// <returns>
        /// "ok" in JSON format if everything is successful, otherwise a standard error page.
        /// </returns>
        [HttpGet]
        public ActionResult BotCron()
        {
            _Log.Info("Running cron");

            using (var database = new PwgTelegramBotEntities())
            {
                var expiredAuths = database.HarvestAuths.Where(x => x.HarvestTokenExpiration < DateTime.Now);
                foreach (var expiredAuth in expiredAuths)
                {
                    _Log.Info("Renewing expired HarvestAuth for UserId: " + expiredAuth.UserId);

                    var token = WebRequestHelper.PostHarvestOAuth(expiredAuth.HarvestCode, true);
                    expiredAuth.HarvestTokenExpiration = token.Expiration;
                    expiredAuth.HarvestRefreshToken = token.RefreshToken;
                    expiredAuth.HarvestToken = token.AccessToken;

                    database.SaveChanges();
                }
            }

            JsonResult jsonResult = new JsonResult
            {
                Data = "ok",
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };

            _Log.Info("Cron completed successfully");

            return jsonResult;
        }

        /// <summary>
        /// This page is where the user ends up after going through the Harvest OAuth process.
        /// The page has a line that they copy and paste into a message for the bot so that it can authenticate the user with Harvest.
        /// </summary>
        /// <returns>
        /// A view with the authentication code.
        /// </returns>
        public ActionResult HarvestAuthRedirect()
        {
            string authenticationCode = Request.QueryString["code"];
            _Log.Info("HarvestAuthRedirect");
            return View((object) authenticationCode);
        }

        /// <summary>
        /// This uses the Harvest API client from Nuget to authenticate a user with a Harvest OAuth token.
        /// It gets the information using the database and the userId.
        /// If the token is expired it will be refreshed here.
        /// </summary>
        /// <param name="database">
        /// The Entity Framework database object.
        /// </param>
        /// <param name="userId">
        /// The ID of the user who sent the message.
        /// </param>
        /// <returns>
        /// An object used to authenticate user requests.
        /// </returns>
        public static HarvestRestClient GetHarvestClient(PwgTelegramBotEntities database, int userId)
        {
            _Log.Info("GetHarvestClient for UserId: " + userId);
            var harvestAuth = database.HarvestAuths.FirstOrDefault(x => x.UserId == userId);

            if (harvestAuth != null)
            {
                bool isExpired = harvestAuth.HarvestTokenExpiration != null &&
                                 DateTime.Now > harvestAuth.HarvestTokenExpiration.Value;
                if (isExpired)
                {
                    var token = WebRequestHelper.PostHarvestOAuth(harvestAuth.HarvestCode, true);
                    harvestAuth.HarvestTokenExpiration = token.Expiration;
                    harvestAuth.HarvestRefreshToken = token.RefreshToken;
                    harvestAuth.HarvestToken = token.AccessToken;

                    database.SaveChanges();
                }
            }

            if (harvestAuth != null)
            {
                HarvestRestClient harvestClient =
                    new HarvestRestClient(ConfigurationManager.AppSettings["HarvestAccountName"],
                        ConfigurationManager.AppSettings["HarvestClientID"],
                        ConfigurationManager.AppSettings["HarvestClientSecret"], harvestAuth.HarvestToken);
                return harvestClient;
            }

            _Log.Info("Could not get Harvest client for user: " + userId);
            return null;
        }

        /// <summary>
        /// This is an empty about page, it was required for the Harvest OAuth client configuration.
        /// </summary>
        /// <returns>
        /// A basic page.
        /// </returns>
        public ActionResult About()
        {
            JsonResult jsonResult = new JsonResult
            {
                Data = "PWG Telegram Bot",
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };

            _Log.Info("About page");

            return jsonResult;
        }

        /// <summary>
        /// This is the main part of this controller.
        /// This is where Telegram sends POST requests with updates sent to the bot.
        /// The POST requests is converted into a dynamic JSON object that is then converted into an object that can be used with this program.
        /// That object is used to determine what to return back to the user.
        /// </summary>
        /// <returns>
        /// A simple "ok" message if everything works.
        /// </returns>
        [HttpPost]
        public ActionResult Webhook()
        {
            _Log.Info("Started Telegram webhook");

            StreamReader inputStream = new StreamReader(Request.InputStream);
            string inputString = inputStream.ReadToEnd();

            _Log.Info("Got input stream");

            dynamic inputJson = JsonConvert.DeserializeObject(inputString);

            _Log.Info("Got input JSON: " + inputJson);

            UpdateModel update = Models.Telegram.UpdateModel.FromJson(inputJson);

            _Log.Info("Got update model: " + JsonConvert.SerializeObject(update));

            int userId = 0;
            if (update.Message != null)
            {
                userId = update.Message.From.Id;
            }
            else if (update.EditedMessage != null)
            {
                userId = update.EditedMessage.From.Id;
            }
            else if (update.CallbackQuery != null)
            {
                userId = update.CallbackQuery.From.Id;
            }

            bool isValid = false;
            if (update.Message != null)
            {
                if (update.Message.Text != null)
                {
                    isValid = true;
                }
            }
            else if (update.CallbackQuery != null)
            {
                if (update.CallbackQuery.Data != null)
                {
                    isValid = true;
                }
            }

            _Log.Info("Is message valid: " + isValid);
            _Log.Info("Message is from UserId: " + userId);

            if (userId != 0 && isValid)
            {
                using (var database = new PwgTelegramBotEntities())
                {
                    var handledWebhook = database.HandledWebhooks.FirstOrDefault(x => x.UpdateId == update.UpdateId);
                    var userState = database.UserStates.FirstOrDefault(x => x.UserId == userId);

                    if (handledWebhook == null)
                    {
                        _Log.Info("Message hasn't been handled before.");
                        handledWebhook = new HandledWebhook
                        {
                            Id = Guid.NewGuid(),
                            UpdateId = update.UpdateId
                        };

                        database.HandledWebhooks.Add(handledWebhook);
                        database.SaveChanges();

                        _Log.Info("User state: " + JsonConvert.SerializeObject(userState));

                        if (userState == null)
                        {
                            // User doesn't have a state in the database, add the user to the database, default to a user without permissions.
                            userState = BotTelegramModel.HandleNewUser(database, update, userId);
                        }

                        if (userState.Approved.HasValue)
                        {
                            // ReSharper disable once PossibleInvalidOperationException
                            if (userState.Approved.Value) // If the user has permission to use the bot: display menus.
                            {
                                _Log.Info("User is approved");

                                string messageText = "";
                                int chatId = 0;
                                if (update.Message != null)

                                    // Get the message text, and the chat id. This allows the user to either type in a response (message) or click a button (callbackquery).
                                {
                                    messageText = update.Message.Text;
                                    chatId = update.Message.Chat.Id;
                                    userState.ChatId = chatId;
                                    database.SaveChanges();
                                }
                                else if (update.CallbackQuery != null)
                                {
                                    messageText = update.CallbackQuery.Data;
                                    chatId = update.CallbackQuery.Message.Chat.Id;
                                    userState.ChatId = chatId;
                                    database.SaveChanges();
                                }

                                // Possibilities:
                                // * Single string: 1
                                // * Multiple strings separated by a space: 1 1 a 9 z
                                // * Text
                                // * A specific command starting like /command
                                if (messageText.Length > "/authenticateharvest ".Length)
                                {
                                    // Works for havest and pivotal since the commands are the same length
                                    BotTelegramModel.HandleUserAuthenticationCommand(database, messageText, userId, chatId);
                                }

                                if (userState.IsStateTextEntry.HasValue)
                                {
                                    BotTelegramModel.HandleUserTextEntryMode(database, userState, messageText, userId, chatId);
                                }

                                database.SaveChanges();

                                if (userState.State == "0") // Main menu
                                {
                                    BotTelegramModel.SendMainMenuMessage(chatId);
                                }
                                else
                                {
                                    if (userState.State.Substring(0, 1) == "1") // Harvest
                                    {
                                        BotHarvestModel.HarvestWebhook(database, update, userId, chatId, userState);
                                    }
                                    else if (userState.State.Substring(0, 1) == "2") // Pivotal
                                    {
                                        BotPivotalModel.PivotalWebhook(database, update, userId, chatId, userState);
                                    }
                                }

                                database.SaveChanges();
                            }
                            else
                            {
                                // If the user does not have permission to use the bot: tell the user to message me to request permission.
                                BotTelegramModel.SendUnauthenticatedMessage(update, userState);
                            }
                        }
                    }
                    else
                    {
                        BotTelegramModel.SendErrorProcessingRequestMessage(database, userState);
                    }
                }
            }

            JsonResult jsonResult = new JsonResult
            {
                Data = "ok",
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };

            return jsonResult;
        }

        /// <summary>
        /// This page can be accessed to configure the bot to use the correct webhook address.
        /// This was used when the bot was initially created with the Bot Father, but wasn't configured.
        /// </summary>
        /// <returns>
        /// The results JSON that Telegram returns.
        /// </returns>
        public JsonResult EnableBot()
        {
            var result = new JsonResult { JsonRequestBehavior = JsonRequestBehavior.AllowGet};

            var request =
                (HttpWebRequest)
                    WebRequest.Create("https://api.telegram.org/bot" +
                                      ConfigurationManager.AppSettings["TelegramBotToken"] +
                                      "/setWebhook?url=https://pwgwebhooktestbot.quade.co/PwgTelegramBot/Bot/Webhook");
            request.ContentType = "application/json";
            request.Accept = "application/json";
            var response = (HttpWebResponse) request.GetResponse();
            Stream stream = response.GetResponseStream();
            if (stream != null)
            {
                StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                String responseString = reader.ReadToEnd();

                result.Data = responseString;
            }
            else
            {
                result.Data = "Error";
            }

            return result;
        }
    }
}