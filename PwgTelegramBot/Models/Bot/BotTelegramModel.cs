//-----------------------------------------------------------------------
// <copyright file="BotTelegramModel.cs" company="Phoenix Web Group">
//     BotTelegramModel
// </copyright>
//-----------------------------------------------------------------------
namespace PwgTelegramBot.Models.Bot
{
    using System;
    using System.Linq;
    using Telegram;

    /// <summary>
    /// This model handles operations related to the Telegram API that happen within the BotController.
    /// </summary>
    public class BotTelegramModel
    {
        /// <summary>
        /// Set up an object to use with Log4Net logging.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        private static readonly log4net.ILog _Log = log4net.LogManager.GetLogger(typeof(BotTelegramModel));

        /// <summary>
        /// If a new user is using the bot then this will add them to the database as an unapproved user.
        /// </summary>
        /// <param name="database">
        /// The Entity Framework database object.
        /// </param>
        /// <param name="update">
        /// The update model coming from Telegram.
        /// </param>
        /// <param name="userId">
        /// The ID of the new user.
        /// </param>
        /// <returns>
        /// A new UserState object.
        /// </returns>
        public static UserState HandleNewUser(PwgTelegramBotEntities database, UpdateModel update, int userId)
        {
            _Log.Info("User is a new user");
            var userState = new UserState
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

            return userState;
        }

        /// <summary>
        /// If a user sends text to the bot then this method will run.
        /// This method will send the user to the main menu if they typed in 0 or /start.
        /// It will handle commands that are entered if the message sstarts with a '/'
        /// It will overwrite their state if they send more than one char at a time (allows for skipping parts of the menu)
        /// It will append to their state if they only send one char.
        /// </summary>
        /// <param name="database">
        /// Entity Framework database object
        /// </param>
        /// <param name="userState">
        /// User state object from the database.
        /// </param>
        /// <param name="messageText">
        /// The text of the message sent to the bot.
        /// </param>
        /// <param name="userId">
        /// The ID of the user.
        /// </param>
        /// <param name="chatId">
        /// The ID of the chat.
        /// </param>
        public static void HandleUserTextEntryMode(PwgTelegramBotEntities database, UserState userState, string messageText, int userId, int chatId)
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
                    HandleUserCommandMode(database, userState, userId, chatId, messageText);
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

        /// <summary>
        /// This method handles commands that involve authenticating with harvest or pivotal. 
        /// </summary>
        /// <param name="database">
        /// Entity Framework database object.
        /// </param>
        /// <param name="messageText">
        /// Text that the user sent.
        /// </param>
        /// <param name="userId">
        /// The ID of the user.
        /// </param>
        /// <param name="chatId">
        /// The ID of the chat.
        /// </param>
        public static void HandleUserAuthenticationCommand(PwgTelegramBotEntities database, string messageText, int userId, int chatId)
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

        /// <summary>
        /// This method handles commands that the user enters that start with a '/', with the exception of the /start command, and commands used to authenticate with harvest and pivotal.
        /// Some of the commands handled here involve approving and unapproving users.
        /// </summary>
        /// <param name="database">Entity Framework database object</param>
        /// <param name="userState">User state from the database</param>
        /// <param name="userId">The ID of the user</param>
        /// <param name="chatId">The chat ID</param>
        /// <param name="messageText">The text the user sent.</param>
        public static void HandleUserCommandMode(PwgTelegramBotEntities database, UserState userState, int userId, int chatId, string messageText)
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
                                    "User " + userToApprove + " has been approved.", "",
                                    null,
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

        /// <summary>
        /// This just prints off the main menu.
        /// </summary>
        /// <param name="chatId">The chat ID to send the main menu to.</param>
        public static void SendMainMenuMessage(int chatId)
        {
            _Log.Info("User state is '0', display main menu.");
            MessageModel.SendMessage(chatId,
                "Hello, I'm the PWG Telegram Bot.", "", null, null, null, null, null, null);
            MessageModel.SendMessage(chatId,
                "Please choose a service to interact with:", "", null, null, null, "0", null,
                null);
        }

        /// <summary>
        /// This tells the user that they haven't been authorized to use the bot, and need to message me to get authorized.
        /// </summary>
        /// <param name="update">The update model from Telegram.</param>
        /// <param name="userState">The user state model from the database.</param>
        public static void SendUnauthenticatedMessage(UpdateModel update, UserState userState)
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

        /// <summary>
        /// This sends the user a message saying taht their request had an error being processed.
        /// </summary>
        /// <param name="database">Entity framework database object.</param>
        /// <param name="userState">User state object from the database.</param>
        public static void SendErrorProcessingRequestMessage(PwgTelegramBotEntities database, UserState userState)
        {
            _Log.Info(
                "Message has already been handled, there was probably an error while handling it, ignoring repeat message.");
            if (userState != null)
            {
                MessageModel.SendMessage(userState.UserId,
                    "There was an error while processing your request, returning to the main menu.", "",
                    null, null, null, null, null, null);
                userState.State = "0";
                userState.IsStateTextEntry = false;
                database.SaveChanges();
            }
        }
    }
}