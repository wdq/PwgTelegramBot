using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using PwgTelegramBot.Models.Telegram;
using PwgTelegramBot.Models.Tracker.Projects;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using InlineKeyboardButton = PwgTelegramBot.Models.Telegram.InlineKeyboardButton;
using IReplyMarkup = Telegram.Bot.Types.ReplyMarkups.IReplyMarkup;

namespace PwgTelegramBot.Controllers
{
    public class BotController : Controller
    {
        // GET: Bot
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult HarvestAuthRedirect()
        {
            string authenticationCode = Request.QueryString["code"];

            return View((object) authenticationCode);
        }


        public ActionResult About()
        {
            JsonResult jsonResult = new JsonResult();
            jsonResult.Data = "PWG Telegram Bot";
            jsonResult.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            return jsonResult;
        }

        [HttpPost]
        public ActionResult Webhook()
        {
            StreamReader inputStream = new StreamReader(Request.InputStream);
            string inputString = inputStream.ReadToEnd();

            dynamic inputJson = JsonConvert.DeserializeObject(inputString);

            UpdateModel update = Models.Telegram.UpdateModel.FromJson(inputJson);

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

            if (userId != 0)
            {
                using(var database = new BotDatabaseDataContext())
                {
                    var userState = database.UserStates.FirstOrDefault(x => x.UserId == userId);
                    if (userState == null) // User doesn't have a state in the database, add the user to the database, default to a user without permissions.
                    {
                        userState = new UserState();
                        userState.UserId = userId;
                        userState.State = "0"; // Main menu
                        userState.Approved = false;
                        userState.IsAdmin = false;
                        userState.IsStateTextEntry = false;
                        userState.Notes = "New unapproved user " + DateTime.Now + ".";
                        database.UserStates.InsertOnSubmit(userState);
                        database.SubmitChanges();
                    }
                    if (userState.Approved.HasValue)
                    {
                        if (userState.Approved.Value) // If the user has permission to use the bot: display menus.
                        {
                            string messageText = "";
                            int chatId = 0;
                            if (update.Message != null) // Get the message text, and the chat id. This allows the user to either type in a response (message) or click a button (callbackquery).
                            {
                                messageText = update.Message.Text;
                                chatId = update.Message.Chat.Id;
                            }
                            else if (update.CallbackQuery != null)
                            {
                                messageText = update.CallbackQuery.Data;
                                chatId = update.CallbackQuery.Message.Chat.Id;
                            }



                            // Possibilities:
                            // * Single string: 1
                            // * Multiple strings separated by a space: 1 1 a 9 z
                            // * Text 

                            if (userState.IsStateTextEntry.HasValue)
                            {
                                if (userState.IsStateTextEntry.Value) // User is entering text, so don't try to parse out the commands
                                {

                                }
                                else // User is entering a command, either single string, or space separated strings
                                {
                                    string[] commands = messageText.Split(' ');
                                    if (messageText == "0" || messageText == "/start") // User entered 0, or start, go back to the main menu
                                    {
                                        userState.State = "0"; 

                                    } 
                                    else if (commands.Length > 1) // More than one command entered, overwrite previous state
                                    {
                                        userState.State = messageText;
                                    }
                                    else // Single command entered, append to state
                                    {
                                        if (userState.State == "0")
                                        {
                                            userState.State = messageText; // If the user is coming from the main menu, get rid of the 0 from the start
                                        }
                                        else
                                        {
                                            userState.State += " " + messageText;
                                        }
                                    }
                                }
                            }
                            database.SubmitChanges();

                            if (userState.State == "0") // Main menu
                            {
                                var messageSent = MessageModel.SendMessage(chatId,
                                    "Hello, I'm the PWG Telegram Bot.", "", null, null, null, null);
                                var messageSent2 = MessageModel.SendMessage(chatId,
                                    "Please choose a service to interact with.", "", null, null, null, "0");
                            }
                            else
                            {
                                var messageSent = MessageModel.SendMessage(chatId,
                                    "Your state is: " + userState.State, "", null, null, null, null);
                            }
                            database.SubmitChanges();
                        }
                        else // If the user does not have permission to use the bot: tell the user to message me to request permission. 
                        {
                            // todo: the update.Message.Chat.Id may be null
                            var messageSent = MessageModel.SendMessage(update.Message.Chat.Id,
                                "Hello, I'm the PWG Telegram Bot.", "", null, null, null, null);
                            var messageSent2 = MessageModel.SendMessage(update.Message.Chat.Id,
                                "You do not have permission to use me.", "", null, null, null, null);
                            var messageSent3 = MessageModel.SendMessage(update.Message.Chat.Id,
                                "Please send a message to @quade, containing " + userState.UserId + ", to request permission.", "", null, null, null, null);
                        }
                        
                    }

                }
            }



            /*if (update.Message != null)
            {
                //var bot = new Telegram.Bot.Api(ConfigurationManager.AppSettings["TelegramBotToken"]);

                if (update.Message.Text == "/start")
                {
                    //KeyboardButton button1 = new KeyboardButton("Button One");
                    //KeyboardButton button2 = new KeyboardButton("Button Two");
                    //KeyboardButton button3 = new KeyboardButton("Button Three");
                    //KeyboardButton[] buttons = { button1, button2, button3};

                    //IReplyMarkup markup = new ReplyKeyboardMarkup(buttons, true, true );
                    // chatId, text, disableWebPagePreview, disableNotification, replyToMessageId, replyMarkup, parseMode, cancellationToken
                    //var send = bot.SendTextMessageAsync(update.Message.Chat.Id, "", false, false, update.Message.Chat.Id, markup);
                    var sent = MessageModel.SendMessage(update.Message.Chat.Id, "Please choose a service to interact with.", "", null, null, null, "mainmenu");
                } else if (update.Message.Text.Length > 9)
                {
                    if (update.Message.Text.Substring(0, 9) == "/addstory")
                    {
                        string[] stringSeparators = new string[] { ",," };

                        string command = update.Message.Text.Substring(9, update.Message.Text.Length - 9);
                        string[] commandArray = command.Split(stringSeparators, StringSplitOptions.None);

                        var newStory = ProjectStory.AddStory(int.Parse(commandArray[0].Trim()), commandArray[1].Trim(), commandArray[2].Trim());
                        var sent = MessageModel.SendMessage(update.Message.Chat.Id, "Story added: " + newStory.Url, "", null, null, null, null);
                    }
                }
                else
                {
                    //bot.SendTextMessageAsync(update.Message.Chat.Id, update.Message.Text);

                    //var sent = MessageModel.SendMessage(update.Message.Chat.Id, update.Message.Text, "", null, null, null, keyboard);
                    var sent = MessageModel.SendMessage(update.Message.Chat.Id, update.Message.Text, "", null, null, null, null);

                }
            }

            if (update.CallbackQuery != null)
            {
                if (update.CallbackQuery.Data == "mainmenu_harvest")
                {
                    var sent = MessageModel.SendMessage(update.CallbackQuery.Message.Chat.Id, "Some harvest stuff goes here...", "", null, null, null, null);
                }
                else if (update.CallbackQuery.Data == "mainmenu_pivotaltracker")
                {
                    var sent = MessageModel.SendMessage(update.CallbackQuery.Message.Chat.Id, "Please choose a project.",
                        "", null, null, null, "pivotaltracker_projects");
                }
                else if(update.CallbackQuery.Data.Contains("pivotaltracker_projects_"))
                {
                    string projectId = update.CallbackQuery.Data.Substring(update.CallbackQuery.Data.IndexOf("pivotaltracker_projects_") + "pivotaltracker_projects_".Length);
                    var project = Models.Tracker.Projects.Project.GetProject(int.Parse(projectId));

                    if (update.CallbackQuery.Data == "pivotaltracker_projects_" + project.Id)
                    {
                        var sent = MessageModel.SendMessage(update.CallbackQuery.Message.Chat.Id, "Select an action for " + project.Name + ".",
                            "", null, null, null, "pivotaltracker_project_actions_" + project.Id);
                    }
                }
                else if (update.CallbackQuery.Data.Contains("pivotaltracker_project_action_addstory_"))
                {
                    string projectId = update.CallbackQuery.Data.Substring(update.CallbackQuery.Data.IndexOf("pivotaltracker_project_action_addstory_") + "pivotaltracker_project_action_addstory_".Length);
                    var project = Models.Tracker.Projects.Project.GetProject(int.Parse(projectId));

                    var sent = MessageModel.SendMessage(update.CallbackQuery.Message.Chat.Id, "Please reply with a message with the following format to add a story: ", "", null, null, null, null);
                    var sent2 = MessageModel.SendMessage(update.CallbackQuery.Message.Chat.Id, "/addstory " + project.Id + ",, story name,, story description", "", null, null, null, null);

                }
                else if (update.CallbackQuery.Data.Contains("pivotaltracker_project_action_"))
                {
                    var sent = MessageModel.SendMessage(update.CallbackQuery.Message.Chat.Id, "Sorry, that action isn't supported yet. ", "", null, null, null, null);
                }
            } */


            JsonResult jsonResult = new JsonResult();
            jsonResult.Data = "ok";
            jsonResult.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            return jsonResult;
        }

        public JsonResult EnableBot()
        {
            var request = (HttpWebRequest)WebRequest.Create("https://api.telegram.org/bot" + ConfigurationManager.AppSettings["TelegramBotToken"] + "/setWebhook?url=https://pwgwebhooktestbot.quade.co/PwgTelegramBot/Bot/Webhook");
            request.ContentType = "application/json";
            request.Accept = "application/json";
            var response = (HttpWebResponse)request.GetResponse();
            Stream stream = response.GetResponseStream();
            StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            String responseString = reader.ReadToEnd();

            var result = new JsonResult();
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            result.Data = responseString;

            return result;
        }
    }
}