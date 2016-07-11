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
using Harvest.Net;
using Harvest.Net.Models;
using Harvest.Net.Models.Interfaces;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using PwgTelegramBot.Models;
using PwgTelegramBot.Models.Telegram;
using PwgTelegramBot.Models.Tracker.Projects;
using RestSharp.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using InlineKeyboardButton = PwgTelegramBot.Models.Telegram.InlineKeyboardButton;
using IReplyMarkup = Telegram.Bot.Types.ReplyMarkups.IReplyMarkup;
using System.Data.Entity;

namespace PwgTelegramBot.Controllers
{
    public class BotController : Controller
    {
        // GET: Bot
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult BotCron()
        {
            var test = new BotDatabase();

            using (var database = new BotDatabase())
            {
                var expiredAuths = database.HarvestAuths.Where(x => x.HarvestTokenExpiration < DateTime.Now);
                foreach (var expiredAuth in expiredAuths)
                {
                    var token = WebRequestHelper.PostHarvestOAuth(expiredAuth.HarvestCode, true);
                    expiredAuth.HarvestTokenExpiration = token.Expiration;
                    expiredAuth.HarvestRefreshToken = token.RefreshToken;
                    expiredAuth.HarvestToken = token.AccessToken;
                    
                    database.SaveChanges();
                }
            }
            JsonResult jsonResult = new JsonResult();
            jsonResult.Data = "ok";
            jsonResult.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            return jsonResult;
        }

        public ActionResult HarvestAuthRedirect()
        {
            string authenticationCode = Request.QueryString["code"];

            return View((object) authenticationCode);
        }

        public ActionResult PivotalAuthRedirect()
        {
            return View();
        }

        public HarvestRestClient GetHarvestClient(BotDatabase database, int userId)
        {
            var harvestAuth = database.HarvestAuths.FirstOrDefault(x => x.UserId == userId);
            var token = new WebRequestHelper.HarvestOAuthResponse();

            bool isExpired = DateTime.Now > harvestAuth.HarvestTokenExpiration.Value;
            if (isExpired)
            {
                token = WebRequestHelper.PostHarvestOAuth(harvestAuth.HarvestCode, isExpired);
                harvestAuth.HarvestTokenExpiration = token.Expiration;
                harvestAuth.HarvestRefreshToken = token.RefreshToken;
                harvestAuth.HarvestToken = token.AccessToken;

                database.SaveChanges();
            }
            HarvestRestClient harvestClient = new HarvestRestClient(ConfigurationManager.AppSettings["HarvestAccountName"], ConfigurationManager.AppSettings["HarvestClientID"], ConfigurationManager.AppSettings["HarvestClientSecret"], harvestAuth.HarvestToken);
            return harvestClient;
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

            if (userId != 0 && isValid)
            {
                using (var database = new BotDatabase())
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
                        if (userState.Approved.Value) // If the user has permission to use the bot: display menus.
                        {
                            string messageText = "";
                            int chatId = 0;
                            if (update.Message != null) // Get the message text, and the chat id. This allows the user to either type in a response (message) or click a button (callbackquery).
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

                            if (messageText.Length > "/authenticateharvest ".Length) // Works for havest and pivotal since the commands are the same length
                            {
                                if (messageText.Substring(0, "/authenticateharvest ".Length) == "/authenticateharvest ") // Associate a harvest authentication token with a user
                                {
                                    string harvestAuthenticationCode = messageText.Substring("/authenticateharvest ".Length, (messageText.Length - "/authenticateharvest ".Length)).Trim();
                                    var harvestAuth = database.HarvestAuths.FirstOrDefault(x => x.UserId == userId); // Check to see if user already has code in database
                                    if (harvestAuth != null) // If the user exists, then just update the code
                                    {
                                        harvestAuth.HarvestCode = harvestAuthenticationCode;
                                    }
                                    else // If the user doesn't exist add a new row
                                    {
                                        harvestAuth = new HarvestAuth();
                                        harvestAuth.UserId = userId;
                                        harvestAuth.HarvestCode = harvestAuthenticationCode;
                                        database.HarvestAuths.Add(harvestAuth);
                                    }
                                    database.SaveChanges();

                                    var postAuth = WebRequestHelper.PostHarvestOAuth(harvestAuthenticationCode, false);

                                    harvestAuth.HarvestRefreshToken = postAuth.RefreshToken;
                                    harvestAuth.HarvestToken = postAuth.AccessToken;
                                    harvestAuth.HarvestTokenExpiration = postAuth.Expiration;
                                    database.SaveChanges();

                                    var messageSent = MessageModel.SendMessage(chatId,
                                        "You have successfully connected your Harvest account to your Telegram account.", "", null, null, null, null, null);
                                }
                                else if (messageText.Substring(0, "/authenticatepivotal ".Length) == "/authenticatepivotal ") // Associate a pivotal API token with a user
                                {
                                    string pivotalAPIToken = messageText.Substring("/authenticatepivotal ".Length, (messageText.Length - "/authenticatepivotal ".Length)).Trim();
                                    var pivotalAuth = database.PivotalAuths.FirstOrDefault(x => x.UserId == userId);
                                    if (pivotalAuth != null) // If the user exists, then just update the token
                                    {
                                        pivotalAuth.ApiToken = pivotalAPIToken;
                                    }
                                    else // If the user doesn't exist, add a new row
                                    {
                                        pivotalAuth = new PivotalAuth();
                                        pivotalAuth.UserId = userId;
                                        pivotalAuth.ApiToken = pivotalAPIToken;
                                        database.PivotalAuths.Add(pivotalAuth);
                                    }
                                    database.SaveChanges();

                                    var messageSent = MessageModel.SendMessage(chatId,
                                        "You have successfully connected your Pivotal Tracker account to your Telegram account.", "", null, null, null, null, null);
                                }
                            }

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
                                    else if (messageText.Substring(0, 1) == "/") // User is entering a command
                                    {
                                        userState.State = "-1"; // The -1 state is the command mode
                                        
                                        var user = database.UserStates.FirstOrDefault(x => x.UserId == userId);
                                        if (user.IsAdmin.HasValue)
                                        {
                                            if (user.IsAdmin.Value) // User is an admin
                                            {
                                                var commandParts = messageText.Split(' ');
                                                if (commandParts[0] == "/approveuser") // User wants to approve another user
                                                {
                                                    var userToApprove = commandParts[1]; // Get the Telegram ID of the user to be approved from the command
                                                    var databaseUser = database.UserStates.FirstOrDefault(x => x.UserId == int.Parse(userToApprove)); // Get the database row associated with that user
                                                    databaseUser.Approved = true; // Approve the user
                                                    if (commandParts.Length > 2)
                                                    {
                                                        string notesField = "";
                                                        for (int i = 2; i < commandParts.Length; i++)
                                                        {
                                                            notesField += commandParts[i] + " ";
                                                        }
                                                        databaseUser.Notes = notesField; // If there are enough command arguments, then set the third one to the notes field in the database
                                                    }
                                                    database.SaveChanges(); // Submit the database changes
                                                    var messageSent = MessageModel.SendMessage(chatId,
                                                        "User " + userToApprove + " has been approved.", "", null, null, null, null, null); // Notify user that it worked.
                                                    if (databaseUser.ChatId.HasValue)
                                                    {
                                                    var messageSent2 = MessageModel.SendMessage(databaseUser.ChatId.Value,
                                                        "You have been approved, plase type in /start to get started.", "", null, null, null, null, null); // Notify approved user that it worked.
                                                    }

                                                }
                                                else if (commandParts[0] == "/unapproveuser") // User wants to unapprove another user
                                                {
                                                    var userToApprove = commandParts[1]; // Get the Telegram ID of the user to be unapprove from the command
                                                    var databaseUser = database.UserStates.FirstOrDefault(x => x.UserId == int.Parse(userToApprove)); // Get the database row associated with that user
                                                    databaseUser.Approved = false; // Unapprove the user
                                                    if (commandParts.Length > 2)
                                                    {
                                                        string notesField = "";
                                                        for (int i = 2; i < commandParts.Length; i++)
                                                        {
                                                            notesField += commandParts[i] + " ";
                                                        }
                                                        databaseUser.Notes = notesField; // If there are enough command arguments, then set the third one to the notes field in the database
                                                    }
                                                    database.SaveChanges(); // Submit the database changes
                                                    var messageSent = MessageModel.SendMessage(chatId,
                                                        "User " + userToApprove + " has been unapproved.", "", null, null, null, null, null); // Notify user that it worked.
                                                }
                                            }
                                        }
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
                            database.SaveChanges();

                            if (userState.State == "0") // Main menu
                            {
                                var messageSent = MessageModel.SendMessage(chatId,
                                    "Hello, I'm the PWG Telegram Bot.", "", null, null, null, null, null);
                                var messageSent2 = MessageModel.SendMessage(chatId,
                                    "Please choose a service to interact with:", "", null, null, null, "0", null);
                            }
                            else
                            {
                                if (userState.State.Substring(0, 1) == "1") // Harvest
                                {
                                    var harvestAuth = database.HarvestAuths.FirstOrDefault(x => x.UserId == userId);
                                    if (harvestAuth == null) // User isn't authenticated with Harvest, send them a link to authenticate
                                    {
                                        var messageSent = MessageModel.SendMessage(chatId,
                                            "You haven't linked your Harvest account with your Telegram account.", "", null, null, null, null, null);
                                        var messageSent2 = MessageModel.SendMessage(chatId,
                                            "Please follow this link to connect your accounts: " + "https://" + ConfigurationManager.AppSettings["HarvestAccountName"] + ".harvestapp.com/oauth2/authorize?client_id=" + ConfigurationManager.AppSettings["HarvestClientID"] + "&redirect_uri=" + "https://pwgwebhooktestbot.quade.co/PwgTelegramBot/Bot/HarvestAuthRedirect" + "&state=optional-csrf-token&response_type=code", "", null, null, null, null, null);
                                    }
                                    else // User is authenticated
                                    {
                                        string[] stateArray = userState.State.Split(' ');
                                        if (stateArray.Length == 1)
                                        {
                                            if (stateArray[0] == "1") // Harvest
                                            {
                                                var messageSent = MessageModel.SendMessage(chatId,
                                                    "Select a Harvest action:", "", null, null, null, "1", null);
                                            }
                                        }
                                        else if (stateArray.Length == 2)
                                        {
                                            if (stateArray[0] == "1") // Harvest
                                            {
                                                HarvestRestClient harvestClient = GetHarvestClient(database, userId);
                                                if (stateArray[1] == "1") // Harvest, Add a new time entry
                                                {
                                                    var messageSent = MessageModel.SendMessage(chatId,
                                                        "Select a client:", "", null, null, null, "1 1", harvestClient);

                                                }
                                                else if (stateArray[1] == "2") // Harvest, Edit an existing time entry
                                                {
                                                    var messageSent = MessageModel.SendMessage(chatId,
                                                        "Editing time entries isn't supported yet.", "", null, null, null, null, null);
                                                }
                                            }
                                            else if (stateArray[0] == "2") // Pivotal
                                            {
                                                
                                            }
                                        }
                                        else if (stateArray.Length == 3)
                                        {
                                            if (stateArray[0] == "1" && stateArray[1] == "1") // Harvest, add a new time entry, selected a client, select a project.
                                            {
                                                HarvestRestClient harvestClient = GetHarvestClient(database, userId);
                                                var messageSent = MessageModel.SendMessage(chatId,
                                                    "Select a project:", "", null, null, null, "1 1 " + stateArray[2], harvestClient);
                                            }
                                        }
                                        else if (stateArray.Length == 4)
                                        {
                                            if (stateArray[0] == "1" && stateArray[1] == "1") // Harvest, add a new time entry, selected a client, selected a project, select a task.
                                            {
                                                HarvestRestClient harvestClient = GetHarvestClient(database, userId);
                                                var messageSent = MessageModel.SendMessage(chatId,
                                                    "Select a task:", "", null, null, null, "1 1 " + stateArray[2] + " " + stateArray[3], harvestClient);
                                            }
                                        }
                                        else if (stateArray.Length == 5)
                                        {
                                            if (stateArray[0] == "1" && stateArray[1] == "1") // Harvest, add a new time entry, selected a client, selected a project, selected a task, enter notes.
                                            {
                                                userState.State += " 1"; // Entering the first text field
                                                userState.IsStateTextEntry = true;
                                                database.SaveChanges();
                                                var messageSent = MessageModel.SendMessage(chatId,
                                                    "Type in the note for the time entry and press enter.", "", null, null, null, null, null);
                                            }
                                        }
                                        else if (stateArray.Length == 6)
                                        {
                                            if (stateArray[0] == "1" && stateArray[1] == "1" && stateArray[5] == "1") // Harvest, add a new time entry, selected a client, selected a project, selected a task, entered note, enter hours.
                                            {
                                                string userInputText = update.Message.Text;

                                                var userEntries = database.UserTextEntries.Where(x => x.UserId == userId); // Get rid of previous user text entries that may exist from past sessions.
                                                database.UserTextEntries.RemoveRange(userEntries);
                                                database.SaveChanges();

                                                var userEntry = new UserTextEntry(); // Add user text input to the database
                                                userEntry.Id = Guid.NewGuid();
                                                userEntry.UserId = userId;
                                                userEntry.EntryIndex = 1;
                                                userEntry.EntryText = userInputText;
                                                database.UserTextEntries.Add(userEntry);
                                                database.SaveChanges();

                                                userState.State = userState.State.Remove(userState.State.Length - 1, 1) + "2"; // Entering the second text field
                                                userState.IsStateTextEntry = true;
                                                database.SaveChanges();
                                                var messageSent = MessageModel.SendMessage(chatId,
                                                    "Type in the hours for the time entry and press enter.", "", null, null, null, null, null);
                                            }
                                            else if (stateArray[0] == "1" && stateArray[1] == "1" && stateArray[5] == "2") // Harvest, add a new time entry, selected a client, selected a project, selected a task, entered note, entered hours, done.
                                            {
                                                var harvestClient = GetHarvestClient(database, userId);
                                                int clientIndex = int.Parse(stateArray[2]) - 1;
                                                int projectIndex = int.Parse(stateArray[3]) - 1;
                                                int taskIndex = int.Parse(stateArray[4]) - 1;
                                                var client = harvestClient.ListClients().OrderBy(x => x.Name).ElementAt(clientIndex);
                                                var project = harvestClient.ListProjects(client.Id).OrderBy(x => x.Name).ElementAt(projectIndex);
                                                var taskAssignments = harvestClient.ListTaskAssignments(project.Id);
                                                List<Task> tasks = new List<Task>();
                                                foreach (var taskAssignment in taskAssignments)
                                                {
                                                    var newTask = harvestClient.Task(taskAssignment.TaskId);
                                                    tasks.Add(newTask);
                                                }
                                                tasks = tasks.OrderBy(x => x.Name).ToList();
                                                var task = tasks.ElementAt(taskIndex);

                                                string userInputText = update.Message.Text;

                                                var previousUserEntry = database.UserTextEntries.FirstOrDefault(x => x.UserId == userId && x.EntryIndex == 1);

                                                userState.State = userState.State.Remove(userState.State.Length - 1, 1) + "2"; // Entering the second text field
                                                userState.IsStateTextEntry = false;
                                                database.SaveChanges();

                                                var messageSent = MessageModel.SendMessage(chatId,
                                                    "Saving time entry for " + userInputText + " hours, with a note of: " + previousUserEntry.EntryText, "", null, null, null, null, null);

                                                //var newEntry = harvestClient.CreateDaily(DateTime.Now, project.Id, task.Id, decimal.Parse(userInputText), previousUserEntry.EntryText, null);
                                                var newEntry = WebRequestHelper.PostHarvestDailyEntry(database.HarvestAuths.FirstOrDefault(x => x.UserId == userId).HarvestToken, previousUserEntry.EntryText, double.Parse(userInputText), project.Id.ToString(), task.Id.ToString(), DateTime.Today);
                                                var messageSent2 = MessageModel.SendMessage(chatId,
                                                    "Time entry has been saved.", "", null, null, null, null, null);
                                            }
                                        }
                                    }
                                }
                                else if (userState.State.Substring(0, 1) == "2") // Pivotal
                                {
                                    var pivotalAuth = database.PivotalAuths.FirstOrDefault(x => x.UserId == userId);
                                    if (pivotalAuth == null) // User isn't authenticated with Pivotal, ask them to authenticate
                                    {
                                        var messageSent = MessageModel.SendMessage(chatId, "You haven't linked your Pivotal Tracker account with your Telegram account.", "", null, null, null, null, null);
                                        var messageSent2 = MessageModel.SendMessage(chatId, "While signed into your Pivotal account follow this link: https://www.pivotaltracker.com/profile", "", null, null, null, null, null);
                                        var messageSent3 = MessageModel.SendMessage(chatId, "Find your API token (or create new token) and then reply with a message like the following:", "", null, null, null, null, null);
                                        var messageSent4 = MessageModel.SendMessage(chatId, "/authenticatepivotal API_TOKEN", "", null, null, null, null, null);
                                    }
                                    else
                                    {
                                        string[] stateArray = userState.State.Split(' ');
                                        if (stateArray.Length == 1)
                                        {
                                            if (stateArray[0] == "2") // Pivotal
                                            {
                                                var messageSent = MessageModel.SendMessage(chatId,
                                                        "Stuff.", "", null, null, null, null, null);
                                            }
                                        }
                                    }
                                }

                                //var messageSent = MessageModel.SendMessage(chatId,
                                //    "Your state is: " + userState.State, "", null, null, null, null);
                            }
                            database.SaveChanges();
                        }
                        else // If the user does not have permission to use the bot: tell the user to message me to request permission. 
                        {
                            // todo: the update.Message.Chat.Id may be null
                            var messageSent = MessageModel.SendMessage(update.Message.Chat.Id,
                                "Hello, I'm the PWG Telegram Bot.", "", null, null, null, null, null);
                            var messageSent2 = MessageModel.SendMessage(update.Message.Chat.Id,
                                "You do not have permission to use me.", "", null, null, null, null, null);
                            var messageSent3 = MessageModel.SendMessage(update.Message.Chat.Id,
                                "Please send a message to @quade, containing " + userState.UserId + ", to request permission.", "", null, null, null, null, null);
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