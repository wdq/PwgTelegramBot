using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;
using Harvest.Net;
using Harvest.Net.Models;
using Newtonsoft.Json;
using PwgTelegramBot.Models;
using PwgTelegramBot.Models.Telegram;
using PwgTelegramBot.Models.Tracker.Projects;

namespace PwgTelegramBot.Controllers
{
    public class BotController : Controller
    {
        // ReSharper disable once InconsistentNaming
        private static readonly log4net.ILog _Log = log4net.LogManager.GetLogger(typeof(BotController));

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

        public ActionResult HarvestAuthRedirect()
        {
            string authenticationCode = Request.QueryString["code"];
            _Log.Info("HarvestAuthRedirect");
            return View((object) authenticationCode);
        }

        public ActionResult PivotalAuthRedirect()
        {
            _Log.Info("PivotalAuthRedirect");
            return View();
        }

        public HarvestRestClient GetHarvestClient(PwgTelegramBotEntities database, int userId)
        {
            _Log.Info("GetHarvestClient for UserId: " + userId);
            var harvestAuth = database.HarvestAuths.FirstOrDefault(x => x.UserId == userId);

            if (harvestAuth != null)
            {
                bool isExpired = harvestAuth.HarvestTokenExpiration != null && DateTime.Now > harvestAuth.HarvestTokenExpiration.Value;
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
                HarvestRestClient harvestClient = new HarvestRestClient(ConfigurationManager.AppSettings["HarvestAccountName"], ConfigurationManager.AppSettings["HarvestClientID"], ConfigurationManager.AppSettings["HarvestClientSecret"], harvestAuth.HarvestToken);
                return harvestClient;
            }
            _Log.Info("Could not get Harvest client for user: " + userId);
            return null;
        }


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
                            // User doesn't have a state in the database, add the user to the database, default to a user without permissions.
                        {
                            _Log.Info("User is a new user");
                            userState = new UserState
                            {
                                UserId = userId,
                                State = "0",
                                Approved = false,
                                IsAdmin = false,
                                IsStateTextEntry = false,
                                Notes = "New unapproved user " + DateTime.Now + "."
                            };
                            // Main menu
                            if (update.Message != null)
                            {
                                userState.ChatId = update.Message.Chat.Id;
                            }
                            else if (update.CallbackQuery != null)
                            {
                                userState.ChatId = update.CallbackQuery.Message.Chat.Id;
                            }
                            database.UserStates.Add(userState);
                            database.SaveChanges();
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
                                    // Works for havest and pivotal since the commands are the same length
                                {
                                    if (messageText.Substring(0, "/authenticateharvest ".Length) ==
                                        "/authenticateharvest ") // Associate a harvest authentication token with a user
                                    {
                                        _Log.Info("User is authenticating with Harvest.");
                                        string harvestAuthenticationCode =
                                            messageText.Substring("/authenticateharvest ".Length,
                                                (messageText.Length - "/authenticateharvest ".Length)).Trim();
                                        var harvestAuth = database.HarvestAuths.FirstOrDefault(x => x.UserId == userId);
                                            // Check to see if user already has code in database
                                        if (harvestAuth != null) // If the user exists, then just update the code
                                        {
                                            harvestAuth.HarvestCode = harvestAuthenticationCode;
                                        }
                                        else // If the user doesn't exist add a new row
                                        {
                                            harvestAuth = new HarvestAuth
                                            {
                                                UserId = userId,
                                                HarvestCode = harvestAuthenticationCode
                                            };
                                            database.HarvestAuths.Add(harvestAuth);
                                        }
                                        database.SaveChanges();

                                        _Log.Info(
                                            "Get OAuth tokens wusing Harvest authentication code (PostHarvestOAuth).");
                                        var postAuth = WebRequestHelper.PostHarvestOAuth(harvestAuthenticationCode,
                                            false);

                                        harvestAuth.HarvestRefreshToken = postAuth.RefreshToken;
                                        harvestAuth.HarvestToken = postAuth.AccessToken;
                                        harvestAuth.HarvestTokenExpiration = postAuth.Expiration;
                                        database.SaveChanges();

                                        MessageModel.SendMessage(chatId,
                                            "You have successfully connected your Harvest account to your Telegram account.",
                                            "", null, null, null, null, null, null);
                                    }
                                    else if (messageText.Substring(0, "/authenticatepivotal ".Length) ==
                                             "/authenticatepivotal ") // Associate a pivotal API token with a user
                                    {
                                        _Log.Info("User is authenticating with Pivotal.");
                                        string pivotalApiToken =
                                            messageText.Substring("/authenticatepivotal ".Length,
                                                (messageText.Length - "/authenticatepivotal ".Length)).Trim();
                                        var pivotalAuth =
                                            database.PivotalAuths.FirstOrDefault(x => x.UserId == userId);
                                        if (pivotalAuth != null) // If the user exists, then just update the token
                                        {
                                            pivotalAuth.ApiToken = pivotalApiToken;
                                        }
                                        else // If the user doesn't exist, add a new row
                                        {
                                            pivotalAuth = new PivotalAuth
                                            {
                                                UserId = userId,
                                                ApiToken = pivotalApiToken
                                            };
                                            database.PivotalAuths.Add(pivotalAuth);
                                        }
                                        database.SaveChanges();

                                        MessageModel.SendMessage(chatId,
                                            "You have successfully connected your Pivotal Tracker account to your Telegram account.",
                                            "", null, null, null, null, null, null);
                                    }
                                }

                                if (userState.IsStateTextEntry.HasValue)
                                {
                                    // ReSharper disable once PossibleInvalidOperationException
                                    if (userState.IsStateTextEntry.Value)
                                        // User is entering text, so don't try to parse out the commands
                                    {
                                        _Log.Info("User is in text entry mode, don't try to parse out commands.");

                                    }
                                    else // User is entering a command, either single string, or space separated strings
                                    {
                                        _Log.Info("User is not in text entry mode, attempt to parse command.");
                                        string[] commands = messageText.Split(' ');
                                        if (messageText == "0" || messageText == "/start")
                                            // User entered 0, or start, go back to the main menu
                                        {
                                            _Log.Info("Return user state to main menu (0).");
                                            userState.State = "0";

                                        }
                                        else if (messageText.Substring(0, 1) == "/") // User is entering a command
                                        {
                                            _Log.Info(
                                                "User is entering a command starting with a '/', set state to command mode (-1).");
                                            userState.State = "-1"; // The -1 state is the command mode

                                            var user = database.UserStates.FirstOrDefault(x => x.UserId == userId);
                                            if (user != null)
                                            {
                                                if (user.IsAdmin.HasValue)
                                                {
                                                    // ReSharper disable once PossibleInvalidOperationException
                                                    if (user.IsAdmin.Value) // User is an admin
                                                    {
                                                        _Log.Info("User is an admin.");
                                                        var commandParts = messageText.Split(' ');
                                                        if (commandParts[0] == "/approveuser")
                                                        // User wants to approve another user
                                                        {
                                                            _Log.Info("User is approving another user.");
                                                            var userToApprove = commandParts[1];
                                                            // Get the Telegram ID of the user to be approved from the command
                                                            _Log.Info("User being approved is: " + userToApprove);
                                                            var databaseUser =
                                                                database.UserStates.FirstOrDefault(
                                                                    x => x.UserId == int.Parse(userToApprove));
                                                            // Get the database row associated with that user
                                                            if (databaseUser != null)
                                                            {
                                                                databaseUser.Approved = true; // Approve the user
                                                                if (commandParts.Length > 2)
                                                                {
                                                                    string notesField = "";
                                                                    for (int i = 2; i < commandParts.Length; i++)
                                                                    {
                                                                        notesField += commandParts[i] + " ";
                                                                    }
                                                                    databaseUser.Notes = notesField;
                                                                    // If there are enough command arguments, then set the third one to the notes field in the database
                                                                }
                                                                database.SaveChanges(); // Submit the database changes
                                                                MessageModel.SendMessage(chatId,
                                                                    "User " + userToApprove + " has been approved.", "", null,
                                                                    null, null, null, null, null);
                                                                // Notify user that it worked.
                                                                if (databaseUser.ChatId.HasValue)
                                                                {
                                                                    // ReSharper disable once PossibleInvalidOperationException
                                                                    MessageModel.SendMessage(databaseUser.ChatId.Value,
                                                                        "You have been approved, plase type in /start to get started.",
                                                                        "", null, null, null, null, null, null);
                                                                    // Notify approved user that it worked.
                                                                }
                                                            }
                                                        }
                                                        else if (commandParts[0] == "/unapproveuser")
                                                        // User wants to unapprove another user
                                                        {
                                                            _Log.Info("User is unapproving another user.");
                                                            var userToApprove = commandParts[1];
                                                            // Get the Telegram ID of the user to be unapprove from the command
                                                            _Log.Info("User being unapproved is: " + userToApprove);
                                                            var databaseUser =
                                                                database.UserStates.FirstOrDefault(
                                                                    x => x.UserId == int.Parse(userToApprove));
                                                            // Get the database row associated with that user
                                                            if (databaseUser != null)
                                                            {
                                                                databaseUser.Approved = false; // Unapprove the user
                                                                if (commandParts.Length > 2)
                                                                {
                                                                    string notesField = "";
                                                                    for (int i = 2; i < commandParts.Length; i++)
                                                                    {
                                                                        notesField += commandParts[i] + " ";
                                                                    }
                                                                    databaseUser.Notes = notesField;
                                                                    // If there are enough command arguments, then set the third one to the notes field in the database
                                                                }
                                                            }
                                                            database.SaveChanges(); // Submit the database changes
                                                            MessageModel.SendMessage(chatId,
                                                                "User " + userToApprove + " has been unapproved.", "",
                                                                null, null, null, null, null, null);
                                                            // Notify user that it worked.
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else if (commands.Length > 1)
                                            // More than one command entered, overwrite previous state
                                        {
                                            _Log.Info(
                                                "User has entered more than one command at once, replace their state wtih the text they entered: " +
                                                messageText);
                                            userState.State = messageText;
                                        }
                                        else // Single command entered, append to state
                                        {
                                            if (userState.State == "0")
                                            {
                                                _Log.Info(
                                                    "User has entered one command, and that command is '0', return user to the main menu.");
                                                userState.State = messageText;
                                                    // If the user is coming from the main menu, get rid of the 0 from the start
                                            }
                                            else
                                            {
                                                userState.State += " " + messageText;
                                                _Log.Info(
                                                    "User has entered one command, append that command to their state, state=" +
                                                    userState.State);
                                            }
                                        }
                                    }
                                }
                                database.SaveChanges();

                                if (userState.State == "0") // Main menu
                                {
                                    _Log.Info("User state is '0', display main menu.");
                                    MessageModel.SendMessage(chatId,
                                        "Hello, I'm the PWG Telegram Bot.", "", null, null, null, null, null, null);
                                    MessageModel.SendMessage(chatId,
                                        "Please choose a service to interact with:", "", null, null, null, "0", null,
                                        null);
                                }
                                else
                                {
                                    if (userState.State.Substring(0, 1) == "1") // Harvest
                                    {
                                        _Log.Info("User's first command is '1', Harvest mode.");
                                        var harvestAuth = database.HarvestAuths.FirstOrDefault(x => x.UserId == userId);
                                        if (harvestAuth == null)
                                            // User isn't authenticated with Harvest, send them a link to authenticate
                                        {
                                            _Log.Info("User isn't authenticated with Harvest, ask them to authenticate.");
                                            MessageModel.SendMessage(chatId,
                                                "You haven't linked your Harvest account with your Telegram account.",
                                                "", null, null, null, null, null, null);
                                            MessageModel.SendMessage(chatId,
                                                "Please follow this link to connect your accounts: " + "https://" +
                                                ConfigurationManager.AppSettings["HarvestAccountName"] +
                                                ".harvestapp.com/oauth2/authorize?client_id=" +
                                                ConfigurationManager.AppSettings["HarvestClientID"] + "&redirect_uri=" +
                                                "https://pwgwebhooktestbot.quade.co/PwgTelegramBot/Bot/HarvestAuthRedirect" +
                                                "&state=optional-csrf-token&response_type=code", "", null, null, null,
                                                null, null, null);
                                        }
                                        else // User is authenticated
                                        {
                                            _Log.Info("User is authenticated with Harvest, continuing.");
                                            string[] stateArray = userState.State.Split(' ');
                                            _Log.Info("State array: " + JsonConvert.SerializeObject(stateArray));
                                            if (stateArray.Length == 1)
                                            {
                                                _Log.Info("State array length is 1.");
                                                if (stateArray[0] == "1") // Harvest
                                                {
                                                    _Log.Info("Display harvest main menu.");
                                                    MessageModel.SendMessage(chatId,
                                                        "Select a Harvest action:", "", null, null, null, "1", null,
                                                        null);
                                                }
                                            }
                                            else if (stateArray.Length == 2)
                                            {
                                                _Log.Info("State array length is 2.");
                                                HarvestRestClient harvestClient = GetHarvestClient(database, userId);
                                                if (stateArray[1] == "1") // Harvest, Add a new time entry
                                                {
                                                    _Log.Info("Adding a new time entry, ask to select a client.");
                                                   MessageModel.SendMessage(chatId,
                                                        "Select a client:", "", null, null, null, "1 1",
                                                        harvestClient, null);

                                                }
                                                else if (stateArray[1] == "2")
                                                    // Harvest, Edit an existing time entry
                                                {
                                                    _Log.Info("Editing a time entry, say it is unsupported.");
                                                    MessageModel.SendMessage(chatId,
                                                        "Editing time entries isn't supported yet.", "", null,
                                                        null, null, null, null, null);
                                                }

                                            }
                                            else if (stateArray.Length == 3)
                                            {
                                                _Log.Info("State array length is 3.");
                                                if (stateArray[1] == "1") // Harvest, add a new time entry, selected a client, select a project.
                                                {
                                                    _Log.Info(
                                                        "Harvest, add a new time entry, selected a client, select a project.");
                                                    HarvestRestClient harvestClient = GetHarvestClient(database, userId);
                                                    MessageModel.SendMessage(chatId,
                                                        "Select a project:", "", null, null, null,
                                                        "1 1 " + stateArray[2], harvestClient, null);
                                                }
                                            }
                                            else if (stateArray.Length == 4)
                                            {
                                                _Log.Info("State array length is 4.");
                                                if (stateArray[1] == "1") // Harvest, add a new time entry, selected a client, selected a project, select a task.
                                                {
                                                    _Log.Info(
                                                        "Harvest, add a new time entry, selected a client, selected a project, select a task.");
                                                    HarvestRestClient harvestClient = GetHarvestClient(database, userId);
                                                    MessageModel.SendMessage(chatId,
                                                        "Select a task:", "", null, null, null,
                                                        "1 1 " + stateArray[2] + " " + stateArray[3], harvestClient,
                                                        null);
                                                }
                                            }
                                            else if (stateArray.Length == 5)
                                            {
                                                _Log.Info("State array length is 5.");
                                                if (stateArray[1] == "1") // Harvest, add a new time entry, selected a client, selected a project, selected a task, enter notes.
                                                {
                                                    _Log.Info(
                                                        "Harvest, add a new time entry, selected a client, selected a project, selected a task, enter notes.");
                                                    userState.State += " 1"; // Entering the first text field
                                                    userState.IsStateTextEntry = true;
                                                    database.SaveChanges();
                                                    MessageModel.SendMessage(chatId,
                                                        "Type in the note for the time entry and press enter.", "", null,
                                                        null, null, null, null, null);
                                                }
                                            }
                                            else if (stateArray.Length == 6)
                                            {
                                                _Log.Info("State array length is 6.");
                                                if (stateArray[1] == "1" && stateArray[5] == "1")
                                                    // Harvest, add a new time entry, selected a client, selected a project, selected a task, entered note, enter hours.
                                                {
                                                    _Log.Info(
                                                        "Harvest, add a new time entry, selected a client, selected a project, selected a task, entered note, enter hours.");
                                                    string userInputText = update.Message.Text;

                                                    var userEntries =
                                                        database.UserTextEntries.Where(x => x.UserId == userId);
                                                        // Get rid of previous user text entries that may exist from past sessions.
                                                    database.UserTextEntries.RemoveRange(userEntries);
                                                    database.SaveChanges();

                                                    var userEntry = new UserTextEntry
                                                    {
                                                        Id = Guid.NewGuid(),
                                                        UserId = userId,
                                                        EntryIndex = 1,
                                                        EntryText = userInputText
                                                    };
                                                    // Add user text input to the database
                                                    database.UserTextEntries.Add(userEntry);
                                                    database.SaveChanges();

                                                    userState.State =
                                                        userState.State.Remove(userState.State.Length - 1, 1) + "2";
                                                        // Entering the second text field
                                                    userState.IsStateTextEntry = true;
                                                    database.SaveChanges();
                                                    MessageModel.SendMessage(chatId,
                                                        "Type in the hours for the time entry and press enter.", "",
                                                        null, null, null, null, null, null);
                                                }
                                                else if (stateArray[1] == "1" && stateArray[5] == "2") // Harvest, add a new time entry, selected a client, selected a project, selected a task, entered note, entered hours, done.
                                                {
                                                    _Log.Info(
                                                        "Harvest, add a new time entry, selected a client, selected a project, selected a task, entered note, entered hours, done.");
                                                    var harvestClient = GetHarvestClient(database, userId);
                                                    int clientIndex = int.Parse(stateArray[2]) - 1;
                                                    int projectIndex = int.Parse(stateArray[3]) - 1;
                                                    int taskIndex = int.Parse(stateArray[4]) - 1;
                                                    var client =
                                                        harvestClient.ListClients()
                                                            .OrderBy(x => x.Name)
                                                            .ElementAt(clientIndex);
                                                    var project =
                                                        harvestClient.ListProjects(client.Id)
                                                            .OrderBy(x => x.Name)
                                                            .ElementAt(projectIndex);
                                                    var taskAssignments =
                                                        harvestClient.ListTaskAssignments(project.Id);
                                                    List<Task> tasks = new List<Task>();
                                                    foreach (var taskAssignment in taskAssignments)
                                                    {
                                                        var newTask = harvestClient.Task(taskAssignment.TaskId);
                                                        tasks.Add(newTask);
                                                    }
                                                    tasks = tasks.OrderBy(x => x.Name).ToList();
                                                    var task = tasks.ElementAt(taskIndex);

                                                    string userInputText = update.Message.Text;

                                                    var previousUserEntry =
                                                        database.UserTextEntries.FirstOrDefault(
                                                            x => x.UserId == userId && x.EntryIndex == 1);

                                                    userState.State =
                                                        userState.State.Remove(userState.State.Length - 1, 1) + "2";
                                                        // Entering the second text field
                                                    userState.IsStateTextEntry = false;
                                                    database.SaveChanges();

                                                    if (previousUserEntry != null)
                                                    {
                                                        MessageModel.SendMessage(chatId,
                                                            "Saving time entry for " + userInputText +
                                                            " hours, with a note of: " + previousUserEntry.EntryText, "",
                                                            null, null, null, null, null, null);

                                                        //var newEntry = harvestClient.CreateDaily(DateTime.Now, project.Id, task.Id, decimal.Parse(userInputText), previousUserEntry.EntryText, null);
                                                        var firstOrDefault = database.HarvestAuths.FirstOrDefault(
                                                            x => x.UserId == userId);
                                                        if (firstOrDefault != null)
                                                            WebRequestHelper.PostHarvestDailyEntry(
                                                                firstOrDefault.HarvestToken,
                                                                previousUserEntry.EntryText, double.Parse(userInputText),
                                                                project.Id.ToString(), task.Id.ToString(),
                                                                DateTime.Today);
                                                    }
                                                    MessageModel.SendMessage(chatId,
                                                        "Time entry has been saved.", "", null, null, null, null,
                                                        null, null);
                                                }
                                            }
                                        }
                                    }
                                    else if (userState.State.Substring(0, 1) == "2") // Pivotal
                                    {
                                        _Log.Info("Switching to Pivotal mode.");
                                        var pivotalAuth = database.PivotalAuths.FirstOrDefault(x => x.UserId == userId);
                                        if (pivotalAuth == null)
                                            // User isn't authenticated with Pivotal, ask them to authenticate
                                        {
                                            _Log.Info("User is not authenticated with Pivotal.");
                                            MessageModel.SendMessage(chatId,
                                                "You haven't linked your Pivotal Tracker account with your Telegram account.",
                                                "", null, null, null, null, null, null);
                                            MessageModel.SendMessage(chatId,
                                                "While signed into your Pivotal account follow this link: https://www.pivotaltracker.com/profile",
                                                "", null, null, null, null, null, null);
                                            MessageModel.SendMessage(chatId,
                                                "Find your API token (or create new token) and then reply with a message like the following:",
                                                "", null, null, null, null, null, null);
                                            MessageModel.SendMessage(chatId,
                                                "/authenticatepivotal API_TOKEN", "", null, null, null, null, null, null);
                                        }
                                        else // User is authenticated
                                        {
                                            _Log.Info("User is authenticated with Pivotal.");
                                            string[] stateArray = userState.State.Split(' ');
                                            _Log.Info("State array: " + JsonConvert.SerializeObject(stateArray));
                                            if (stateArray.Length == 1)
                                            {
                                                _Log.Info("Pivotal State array length is 1");
                                                _Log.Info("Pivotal main menu, ask to select a tracker action.");
                                                MessageModel.SendMessage(chatId,
                                                    "Select a Pivotal Tracker action:", "", null, null, null, "2", null,
                                                    pivotalAuth.ApiToken);
                                            }
                                            else if (stateArray.Length == 2)
                                            {
                                                _Log.Info("Pivotal State array length is 2");
                                                if (stateArray[1] == "1" || stateArray[1] == "2") // Add or edit story
                                                {
                                                    _Log.Info("Pivotal, add or edit story, ask to select a project");
                                                    MessageModel.SendMessage(chatId,
                                                        "Select a project:", "", null, null, null, "2 " + stateArray[1],
                                                        null, pivotalAuth.ApiToken);
                                                }
                                            }
                                            else if (stateArray.Length == 3)
                                            {
                                                _Log.Info("Pivotal State array length is 3");
                                                if (stateArray[1] == "1") // Add story
                                                {
                                                    _Log.Info("Pivotal, add story, seleted project, ask to select a story type");
                                                    MessageModel.SendMessage(chatId,
                                                        "Select a story type:", "", null, null, null,
                                                        "2 " + stateArray[1] + " " + stateArray[2], null,
                                                        pivotalAuth.ApiToken);
                                                }
                                                else if (stateArray[1] == "2") // Edit story
                                                {
                                                    _Log.Info("Pivotal, edit a story, not yet supported.");
                                                    MessageModel.SendMessage(chatId,
                                                        "I'm a stupid bot, I can't edit stories yet.", "", null, null, null, null, null, null);
                                                }
                                            }
                                            else if (stateArray.Length == 4)
                                            {
                                                _Log.Info("Pivotal State array length is 4");
                                                if (stateArray[1] == "1") // Add a story
                                                {
                                                    _Log.Info("Pivotal, add a story, selected project, selected story type, ask to select number of points.");
                                                    MessageModel.SendMessage(chatId,
                                                        "Select the number of points:", "", null, null, null,
                                                        "2 " + stateArray[1] + " " + stateArray[2] + " " + stateArray[3],
                                                        null, pivotalAuth.ApiToken);

                                                }
                                            }
                                            else if (stateArray.Length == 5)
                                            {
                                                _Log.Info("Pivotal State array length is 5");
                                                if (stateArray[1] == "1") // Add a story
                                                {
                                                    _Log.Info("Pivotal, add a story, selected project, selected story type, selected number of points, ask to select a requester");
                                                    MessageModel.SendMessage(chatId,
                                                        "Select a requester:", "", null, null, null,
                                                        "2 " + stateArray[1] + " " + stateArray[2] + " " + stateArray[3] +
                                                        " " + stateArray[4], null, pivotalAuth.ApiToken);
                                                }
                                            }
                                            else if (stateArray.Length == 6)
                                            {
                                                _Log.Info("Pivotal State array length is 6");
                                                if (stateArray[1] == "1") // Add a story
                                                {
                                                    _Log.Info("Pivotal, add a story, selected project, selected story type, selected number of points, selected a requester, ask to select an owner");
                                                    // todo: I'm pretty sure there is some sort of method that will print out each array element with a separator of your choice that can simplify these,  string.Join(" ", stateArray)
                                                    MessageModel.SendMessage(chatId,
                                                        "Select an owner:", "", null, null, null,
                                                        "2 " + stateArray[1] + " " + stateArray[2] + " " + stateArray[3] +
                                                        " " + stateArray[4] + " " + stateArray[5], null,
                                                        pivotalAuth.ApiToken);
                                                }
                                            }
                                            else if (stateArray.Length == 7)
                                            {
                                                _Log.Info("Pivotal State array length is 7");
                                                if (stateArray[1] == "1") // Add a story
                                                {
                                                    _Log.Info("Pivotal, add a story, selected project, selected story type, selected number of points, selected a requester, selected an owner, ask to enter title");
                                                    userState.State += " 1"; // Entering the first text field, title
                                                    userState.IsStateTextEntry = true;
                                                    database.SaveChanges();

                                                    MessageModel.SendMessage(chatId,
                                                        "Type in a story title and then press enter.", "", null, null,
                                                        null, null, null, pivotalAuth.ApiToken);
                                                }
                                            }
                                            else if (stateArray.Length == 8)
                                            {
                                                _Log.Info("Pivotal State array length is 8");
                                                if (stateArray[1] == "1") // Add a story
                                                {
                                                    if (stateArray[7] == "1") // Second text field, description
                                                    {
                                                        _Log.Info("Pivotal, add a story, selected project, selected story type, selected number of points, selected a requester, selected an owner, entered title, ask to enter description");
                                                        userState.State =
                                                            userState.State.Remove(userState.State.Length - 1, 1) + "2";
                                                        userState.IsStateTextEntry = true;
                                                        database.SaveChanges();

                                                        string userInputText = update.Message.Text;

                                                        var userEntries =
                                                            database.UserTextEntries.Where(x => x.UserId == userId);
                                                            // Get rid of previous user text entries that may exist from past sessions.
                                                        database.UserTextEntries.RemoveRange(userEntries);
                                                        database.SaveChanges();

                                                        var userEntry = new UserTextEntry
                                                        {
                                                            Id = Guid.NewGuid(),
                                                            UserId = userId,
                                                            EntryIndex = 1,
                                                            EntryText = userInputText
                                                        };
                                                        // Add user text input to the database
                                                        // title
                                                        database.UserTextEntries.Add(userEntry);
                                                        database.SaveChanges();

                                                        MessageModel.SendMessage(chatId,
                                                            "Type in a story description and then press enter.", "",
                                                            null, null, null, null, null, pivotalAuth.ApiToken);
                                                    }
                                                    else if (stateArray[7] == "2")
                                                    {
                                                        _Log.Info("Pivotal, add a story, selected project, selected story type, selected number of points, selected a requester, selected an owner, entered title, entered description, ask to enter tasks");
                                                        userState.State =
                                                            userState.State.Remove(userState.State.Length - 1, 1) + "3";
                                                        userState.IsStateTextEntry = true;
                                                        database.SaveChanges();

                                                        string userInputText = update.Message.Text;

                                                        var userEntry = new UserTextEntry
                                                        {
                                                            Id = Guid.NewGuid(),
                                                            UserId = userId,
                                                            EntryIndex = 2,
                                                            EntryText = userInputText
                                                        };
                                                        // Add user text input to the database
                                                        // description
                                                        database.UserTextEntries.Add(userEntry);
                                                        database.SaveChanges();

                                                        MessageModel.SendMessage(chatId,
                                                            "Type in the project tasks, one line per task, and press enter. If you don't want to add any tasks, type in \"None\".",
                                                            "", null, null, null, null, null, pivotalAuth.ApiToken);
                                                    }
                                                    else if (stateArray[7] == "3")
                                                    {
                                                        _Log.Info("Pivotal, add a story, selected project, selected story type, selected number of points, selected a requester, selected an owner, entered title, entered description, entered tasks, ask to select label");
                                                        userState.State =
                                                            userState.State.Remove(userState.State.Length - 1, 1) + "4";
                                                        userState.IsStateTextEntry = false;
                                                        database.SaveChanges();

                                                        string userInputText = update.Message.Text;

                                                        var userEntry = new UserTextEntry
                                                        {
                                                            Id = Guid.NewGuid(),
                                                            UserId = userId,
                                                            EntryIndex = 3,
                                                            EntryText = userInputText
                                                        };
                                                        // Add user text input to the database
                                                        // tasks
                                                        database.UserTextEntries.Add(userEntry);
                                                        database.SaveChanges();

                                                        MessageModel.SendMessage(chatId,
                                                            "Select a label:", "", null, null, null,
                                                            string.Join(" ", stateArray), null, pivotalAuth.ApiToken);
                                                    }
                                                }

                                            }
                                            else if (stateArray.Length == 9)
                                            {
                                                _Log.Info("Pivotal State array length is 9");
                                                if (stateArray[1] == "1") // Add a story
                                                {
                                                    _Log.Info("Pivotal, add a story, selected project, selected story type, selected number of points, selected a requester, selected an owner, entered title, entered description, entered tasks, selected label, saving story");
                                                    var projects =
                                                        Models.Tracker.Projects.Project.GetProjects(pivotalAuth.ApiToken)
                                                            .OrderBy(x => x.Name);
                                                    int projectIndex = int.Parse(stateArray[2]) - 1;
                                                    var project = projects.ElementAt(projectIndex);
                                                    int storyTypeIndex = int.Parse(stateArray[3]) - 1;
                                                    string[] possibleTypes = new[]
                                                    {"feature", "bug", "chore", "release"};
                                                    string storyType = possibleTypes[storyTypeIndex];
                                                    List<string> possiblePoints = project.PointScale.Split(',').ToList();
                                                    int possiblePointIndex = int.Parse(stateArray[4]) - 1;
                                                    possiblePoints.Insert(0, "Unestimated");
                                                    string possiblePoint = possiblePoints.ElementAt(possiblePointIndex);
                                                    var possibleRequesters =
                                                        ProjectMembership.GetMemberships(
                                                            project.Id, pivotalAuth.ApiToken)
                                                            .OrderBy(x => x.Person.Name);
                                                    int possibleRequestersIndex = int.Parse(stateArray[5]) - 1;
                                                    var possibleRequester =
                                                        possibleRequesters.ElementAt(possibleRequestersIndex);
                                                    var possibleOwners =
                                                        ProjectMembership.GetMemberships(
                                                            project.Id, pivotalAuth.ApiToken)
                                                            .OrderBy(x => x.Person.Name)
                                                            .ToList();
                                                    int possibleOwnersIndex = int.Parse(stateArray[6]) - 2;
                                                        // Not -1 since the first item is No owners, but this array doesn't match that
                                                    var possibleOwner = new ProjectMembership();
                                                    if (possibleOwnersIndex != -1)
                                                    {
                                                        possibleOwner = possibleOwners.ElementAt(possibleOwnersIndex);
                                                    }
                                                    var possibleLabels =
                                                        ProjectLabel.GetLabels(project.Id,
                                                            pivotalAuth.ApiToken);
                                                    var possibleLabelsIndex = int.Parse(stateArray[8]) - 2;
                                                        // Not -1, see possibleOwnersIndex
                                                    var possibleLabel = new ProjectLabel();
                                                    if (possibleLabelsIndex != -1)
                                                    {
                                                        possibleLabel = possibleLabels.ElementAt(possibleLabelsIndex);
                                                    }

                                                    var previousUserEntries =
                                                        database.UserTextEntries.Where(x => x.UserId == userId).ToList();
                                                    var titleEntry =
                                                        previousUserEntries.FirstOrDefault(x => x.EntryIndex == 1);
                                                    var descriptionEntry =
                                                        previousUserEntries.FirstOrDefault(x => x.EntryIndex == 2);
                                                    var tasksEntry =
                                                        previousUserEntries.FirstOrDefault(x => x.EntryIndex == 3);

                                                    MessageModel.SendMessage(chatId,
                                                        "Saving new story with these properties:", "", null, null, null,
                                                        null, null, pivotalAuth.ApiToken);
                                                    if (titleEntry != null && descriptionEntry != null && tasksEntry != null)
                                                    {
                                                        MessageModel.SendMessage(chatId,
                                                            "Title: " + titleEntry.EntryText, "", null, null, null, null,
                                                            null, pivotalAuth.ApiToken);
                                                        MessageModel.SendMessage(chatId,
                                                            "Type: " + storyType, "", null, null, null, null, null,
                                                            pivotalAuth.ApiToken);
                                                        MessageModel.SendMessage(chatId,
                                                            "Points: " + possiblePoint, "", null, null, null, null, null,
                                                            pivotalAuth.ApiToken);
                                                        MessageModel.SendMessage(chatId,
                                                            "Requester: " + possibleRequester.Person.Name, "", null, null,
                                                            null, null, null, pivotalAuth.ApiToken);
                                                        MessageModel.SendMessage(chatId,
                                                            "Owner: " +
                                                            (possibleOwnersIndex != -1 ? possibleOwner.Person.Name : ""), "",
                                                            null, null, null, null, null, pivotalAuth.ApiToken);
                                                        MessageModel.SendMessage(chatId,
                                                            "Description: " + descriptionEntry.EntryText, "", null, null,
                                                            null, null, null, pivotalAuth.ApiToken);
                                                        MessageModel.SendMessage(chatId,
                                                            "Label: " +
                                                            (possibleLabelsIndex != -1 ? possibleLabel.Name : ""), "", null,
                                                            null, null, null, null, pivotalAuth.ApiToken);
                                                        MessageModel.SendMessage(chatId,
                                                            "Tasks: " + tasksEntry.EntryText, "", null, null, null, null,
                                                            null, pivotalAuth.ApiToken);

                                                        AddStoryModel newStory = new AddStoryModel
                                                        {
                                                            name = titleEntry.EntryText,
                                                            description = descriptionEntry.EntryText,
                                                            story_type = storyType,
                                                            current_state = "unscheduled"
                                                        };
                                                        // todo: maybe ask user
                                                        if (possiblePoint != "Unestimated")
                                                        {
                                                            newStory.estimate = float.Parse(possiblePoint);
                                                        }
                                                        newStory.requested_by_id = possibleRequester.Person.Id;
                                                        if (possibleOwnersIndex != -1)
                                                        {
                                                            newStory.owner_ids = new[] {possibleOwner.Person.Id};
                                                        }
                                                        if (possibleLabelsIndex != -1)
                                                        {
                                                            newStory.label_ids = new[] {possibleLabel.Id};
                                                        }


                                                        var tasksStringArray =
                                                            tasksEntry.EntryText.Split(new[] {"\r\n", "\n"},
                                                                StringSplitOptions.None).Where(x => x != "None").ToArray();
                                                        var tasksArrayTemp = new AddTaskModel[tasksStringArray.Length];
                                                        for (int i = 0; i < tasksStringArray.Length; i++)
                                                        {
                                                            AddTaskModel task = new AddTaskModel
                                                            {
                                                                description = tasksStringArray[i],
                                                                position = i + 1
                                                            };
                                                            tasksArrayTemp[i] = task;
                                                        }
                                                        newStory.tasks = tasksArrayTemp;
                                                        var addedStory =
                                                            ProjectStory.AddStory(project.Id,
                                                                newStory, pivotalAuth.ApiToken);

                                                        if (addedStory != null)
                                                        {
                                                            _Log.Info("Story has been saved successfully: " + "https://www.pivotaltracker.com/story/show/" + addedStory.Id);
                                                            MessageModel.SendMessage(chatId,
                                                                "Story has been saved successfully: " +
                                                                "https://www.pivotaltracker.com/story/show/" + addedStory.Id,
                                                                "", null, null, null, null, null, pivotalAuth.ApiToken);
                                                        }
                                                        else
                                                        {
                                                            _Log.Info("Failed to save story");
                                                            MessageModel.SendMessage(chatId,
                                                                "Error saving story." + addedStory.Id, "", null, null, null,
                                                                null, null, pivotalAuth.ApiToken);
                                                        }
                                                    }
                                                }
                                            }
                                            else if (stateArray.Length == 10)
                                            {
                                                _Log.Info("Pivotal State array length is 10");

                                            }
                                            else if (stateArray.Length == 11)
                                            {
                                                _Log.Info("Pivotal State array length is 11");

                                            }
                                            else if (stateArray.Length == 12)
                                            {
                                                _Log.Info("Pivotal State array length is 12");

                                            }
                                        }
                                    }

                                    //var messageSent = MessageModel.SendMessage(chatId,
                                    //    "Your state is: " + userState.State, "", null, null, null, null);
                                }
                                database.SaveChanges();
                            }
                            else
                            // If the user does not have permission to use the bot: tell the user to message me to request permission. 
                            {
                                _Log.Info("User isn't authorized.");
                                // todo: the update.Message.Chat.Id may be null
                                MessageModel.SendMessage(update.Message.Chat.Id,
                                    "Hello, I'm the PWG Telegram Bot.", "", null, null, null, null, null, null);
                                MessageModel.SendMessage(update.Message.Chat.Id,
                                    "You do not have permission to use me.", "", null, null, null, null, null, null);
                                MessageModel.SendMessage(update.Message.Chat.Id,
                                    "Please send a message to @quade, containing " + userState.UserId +
                                    ", to request permission.", "", null, null, null, null, null, null);
                            }
                        }
                    }
                    else
                    {
                        _Log.Info("Message has already been handled, there was probably an error while handling it, ignoring repeat message.");
                        if (userState != null)
                        {
                            MessageModel.SendMessage(userState.UserId, "There was an error while processing your request, returning to the main menu.", "", null, null, null, null, null, null);
                            userState.State = "0";
                            userState.IsStateTextEntry = false;
                            database.SaveChanges();
                        }
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

        public JsonResult EnableBot()
        {
            var result = new JsonResult {JsonRequestBehavior = JsonRequestBehavior.AllowGet};

            var request = (HttpWebRequest)WebRequest.Create("https://api.telegram.org/bot" + ConfigurationManager.AppSettings["TelegramBotToken"] + "/setWebhook?url=https://pwgwebhooktestbot.quade.co/PwgTelegramBot/Bot/Webhook");
            request.ContentType = "application/json";
            request.Accept = "application/json";
            var response = (HttpWebResponse)request.GetResponse();
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