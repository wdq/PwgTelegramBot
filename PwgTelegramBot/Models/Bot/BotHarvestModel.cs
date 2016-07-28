//-----------------------------------------------------------------------
// <copyright file="BotHarvestModel.cs" company="Phoenix Web Group">
//     BotHarvestModel
// </copyright>
//-----------------------------------------------------------------------
namespace PwgTelegramBot.Models.Bot
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Harvest.Net;
    using Harvest.Net.Models;
    using Newtonsoft.Json;
    using Controllers;
    using Telegram;

    /// <summary>
    /// This model handles all of the webhooks that come in that involve Harvest, with the exception of authentication. 
    /// </summary>
    public class BotHarvestModel
    {
        /// <summary>
        /// Set up an object to use with Log4Net logging.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        private static readonly log4net.ILog _Log = log4net.LogManager.GetLogger(typeof(BotHarvestModel));

        /// <summary>
        /// The main webhook method.
        /// </summary>
        /// <param name="database">Entity framework database object</param>
        /// <param name="update">Update model from telegram</param>
        /// <param name="userId">ID of the user</param>
        /// <param name="chatId">ID of the chat</param>
        /// <param name="userState">User's state object from the database</param>
        public static void HarvestWebhook(PwgTelegramBotEntities database, UpdateModel update, int userId, int chatId, UserState userState)
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
                    HarvestRestClient harvestClient = BotController.GetHarvestClient(database, userId);
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
                    if (stateArray[1] == "1")

                    // Harvest, add a new time entry, selected a client, select a project.
                    {
                        _Log.Info(
                            "Harvest, add a new time entry, selected a client, select a project.");
                        HarvestRestClient harvestClient = BotController.GetHarvestClient(database, userId);
                        MessageModel.SendMessage(chatId,
                            "Select a project:", "", null, null, null,
                            "1 1 " + stateArray[2], harvestClient, null);
                    }
                }
                else if (stateArray.Length == 4)
                {
                    _Log.Info("State array length is 4.");
                    if (stateArray[1] == "1")

                    // Harvest, add a new time entry, selected a client, selected a project, select a task.
                    {
                        _Log.Info(
                            "Harvest, add a new time entry, selected a client, selected a project, select a task.");
                        HarvestRestClient harvestClient = BotController.GetHarvestClient(database, userId);
                        MessageModel.SendMessage(chatId,
                            "Select a task:", "", null, null, null,
                            "1 1 " + stateArray[2] + " " + stateArray[3], harvestClient,
                            null);
                    }
                }
                else if (stateArray.Length == 5)
                {
                    _Log.Info("State array length is 5.");
                    if (stateArray[1] == "1")

                    // Harvest, add a new time entry, selected a client, selected a project, selected a task, enter notes.
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
                    else if (stateArray[1] == "1" && stateArray[5] == "2")

                    // Harvest, add a new time entry, selected a client, selected a project, selected a task, entered note, entered hours, done.
                    {
                        _Log.Info(
                            "Harvest, add a new time entry, selected a client, selected a project, selected a task, entered note, entered hours, done.");
                        var harvestClient = BotController.GetHarvestClient(database, userId);
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
                                " hours, with a note of: " + previousUserEntry.EntryText,
                                "",
                                null, null, null, null, null, null);

                            //var newEntry = harvestClient.CreateDaily(DateTime.Now, project.Id, task.Id, decimal.Parse(userInputText), previousUserEntry.EntryText, null);
                            var firstOrDefault = database.HarvestAuths.FirstOrDefault(
                                x => x.UserId == userId);
                            if (firstOrDefault != null)
                            {
                                WebRequestHelper.PostHarvestDailyEntry(
                                    firstOrDefault.HarvestToken,
                                    previousUserEntry.EntryText,
                                    double.Parse(userInputText),
                                    project.Id.ToString(), task.Id.ToString(),
                                    DateTime.Today);
                            }
                        }

                        MessageModel.SendMessage(chatId,
                            "Time entry has been saved.", "", null, null, null, null,
                            null, null);
                    }
                }
            }
        }
    }
}