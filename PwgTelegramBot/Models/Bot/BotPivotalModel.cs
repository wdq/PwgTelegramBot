//-----------------------------------------------------------------------
// <copyright file="BotPivotalModel.cs" company="Phoenix Web Group">
//     BotPivotalModel
// </copyright>
//-----------------------------------------------------------------------
namespace PwgTelegramBot.Models.Bot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Telegram;
    using Tracker.Projects;

    /// <summary>
    /// This model handles all of the webhooks that come in that involve Pivotal, with the exception of authentication. 
    /// </summary>
    public class BotPivotalModel
    {
        /// <summary>
        /// Set up an object to use with Log4Net logging.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        private static readonly log4net.ILog _Log = log4net.LogManager.GetLogger(typeof(BotPivotalModel));

        /// <summary>
        /// The main webhook method.
        /// </summary>
        /// <param name="database">Entity framework database object</param>
        /// <param name="update">Update model from telegram</param>
        /// <param name="userId">ID of the user</param>
        /// <param name="chatId">ID of the chat</param>
        /// <param name="userState">User's state object from the database</param>
        public static void PivotalWebhook(PwgTelegramBotEntities database, UpdateModel update, int userId, int chatId, UserState userState)
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
                        _Log.Info(
                            "Pivotal, add story, seleted project, ask to select a story type");
                        MessageModel.SendMessage(chatId,
                            "Select a story type:", "", null, null, null,
                            "2 " + stateArray[1] + " " + stateArray[2], null,
                            pivotalAuth.ApiToken);
                    }
                    else if (stateArray[1] == "2") // Edit story
                    {
                        _Log.Info("Pivotal, edit a story, not yet supported.");
                        MessageModel.SendMessage(chatId,
                            "I'm a stupid bot, I can't edit stories yet.", "", null, null,
                            null, null, null, null);
                    }
                }
                else if (stateArray.Length == 4)
                {
                    _Log.Info("Pivotal State array length is 4");
                    if (stateArray[1] == "1") // Add a story
                    {
                        _Log.Info(
                            "Pivotal, add a story, selected project, selected story type, ask to select number of points.");
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
                        _Log.Info(
                            "Pivotal, add a story, selected project, selected story type, selected number of points, ask to select a requester");
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
                        _Log.Info(
                            "Pivotal, add a story, selected project, selected story type, selected number of points, selected a requester, ask to select an owner");

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
                        _Log.Info(
                            "Pivotal, add a story, selected project, selected story type, selected number of points, selected a requester, selected an owner, ask to enter title");
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
                            _Log.Info(
                                "Pivotal, add a story, selected project, selected story type, selected number of points, selected a requester, selected an owner, entered title, ask to enter description");
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
                            _Log.Info(
                                "Pivotal, add a story, selected project, selected story type, selected number of points, selected a requester, selected an owner, entered title, entered description, ask to enter tasks");
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
                            _Log.Info(
                                "Pivotal, add a story, selected project, selected story type, selected number of points, selected a requester, selected an owner, entered title, entered description, entered tasks, ask to select label");
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
                        _Log.Info(
                            "Pivotal, add a story, selected project, selected story type, selected number of points, selected a requester, selected an owner, entered title, entered description, entered tasks, selected label, saving story");
                        var projects =
                            Project.GetProjects(pivotalAuth.ApiToken)
                                .OrderBy(x => x.Name);
                        int projectIndex = int.Parse(stateArray[2]) - 1;
                        var project = projects.ElementAt(projectIndex);
                        int storyTypeIndex = int.Parse(stateArray[3]) - 1;
                        string[] possibleTypes = new[] { "feature", "bug", "chore", "release" };
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
                        if (titleEntry != null && descriptionEntry != null &&
                            tasksEntry != null)
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
                                "Requester: " + possibleRequester.Person.Name, "", null,
                                null,
                                null, null, null, pivotalAuth.ApiToken);
                            MessageModel.SendMessage(chatId,
                                "Owner: " +
                                (possibleOwnersIndex != -1 ? possibleOwner.Person.Name : ""),
                                "",
                                null, null, null, null, null, pivotalAuth.ApiToken);
                            MessageModel.SendMessage(chatId,
                                "Description: " + descriptionEntry.EntryText, "", null, null,
                                null, null, null, pivotalAuth.ApiToken);
                            MessageModel.SendMessage(chatId,
                                "Label: " +
                                (possibleLabelsIndex != -1 ? possibleLabel.Name : ""), "",
                                null,
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
                                newStory.owner_ids = new[] { possibleOwner.Person.Id };
                            }

                            if (possibleLabelsIndex != -1)
                            {
                                newStory.label_ids = new[] { possibleLabel.Id };
                            }

                            var tasksStringArray =
                                tasksEntry.EntryText.Split(new[] { "\r\n", "\n" },
                                    StringSplitOptions.None)
                                    .Where(x => x != "None")
                                    .ToArray();
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
                                _Log.Info("Story has been saved successfully: " +
                                          "https://www.pivotaltracker.com/story/show/" +
                                          addedStory.Id);
                                MessageModel.SendMessage(chatId,
                                    "Story has been saved successfully: " +
                                    "https://www.pivotaltracker.com/story/show/" +
                                    addedStory.Id,
                                    "", null, null, null, null, null, pivotalAuth.ApiToken);
                            }
                            else
                            {
                                _Log.Info("Failed to save story");
                                MessageModel.SendMessage(chatId,
                                    "Error saving story." + addedStory.Id, "", null, null,
                                    null,
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
    }
}