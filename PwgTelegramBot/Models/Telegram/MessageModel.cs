using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using Harvest.Net;
using Harvest.Net.Models;
using Newtonsoft.Json;

namespace PwgTelegramBot.Models.Telegram
{
    public class SendMessageModel
    {
        public int chat_id { get; set; }
        public string text { get; set; }
        public string parse_mode { get; set; }
        public bool? disable_web_page_preview { get; set; }
        public bool? disable_notification { get; set; }
        public int? reply_to_message_id { get; set; }
        public InlineKeyboardMarkup reply_markup { get; set; }

    }

    public class ReplyMarkupMaybe
    {
        public string[,] keyboard { get; set; }
        public bool one_time_keyboard { get;set; }
    }

    public class MessageModel
    {
        public int MessageId { get; set; }
        public UserModel From { get; set; }
        public DateTime Date { get; set; }
        public ChatModel Chat { get; set; }
        public UserModel ForwardFrom { get; set; }
        public ChatModel ForwardFromChat { get; set; }
        public DateTime? ForwardDate { get; set; }
        public MessageModel ReplyToMessage { get; set; }
        public DateTime? EditDate { get; set; }
        public string Text { get; set; }
        public List<MessageEntityModel> Entities { get; set; }
        public AudioModel Audio { get; set; }
        public DocumentModel Document { get; set; }
        public List<PhotoSizeModel> Photo { get; set; }
        public StickerModel Sticker { get; set; }
        public VideoModel Video { get; set; }
        public VoiceModel Voice { get; set; }
        public string Caption { get; set; }
        public ContactModel Contact { get; set; }
        public LocationModel Location { get; set; }
        public VenueModel Venue { get; set; }
        public UserModel NewChatMember { get; set; }
        public UserModel LeftChatMember { get; set; }
        public string NewChatTitle { get; set; }
        public List<PhotoSizeModel> NewChatPhoto { get; set; }
        public bool? DeleteChatPhoto { get; set; }
        public bool? GroupChatCreated { get; set; }
        public bool? SupergroupChatCreated { get; set; }
        public bool? ChannelChatCreated { get; set; }
        public int? MigrateToChatId { get; set; }
        public int? MigrateFromChatId { get; set; }
        public MessageModel PinnedMessage { get; set; }




        public static MessageModel SendMessage(int chatId, string text, string parseMode, bool? disableWebPagePreview, bool? disableNotification, int? replyToMessageId, string replyMarkup, HarvestRestClient harvestClient)
        {
            SendMessageModel model = new SendMessageModel();

            string path = "/sendMessage";

            model.chat_id = chatId;
            model.text = text;

            if (!string.IsNullOrEmpty(parseMode) && !string.IsNullOrWhiteSpace(parseMode))
            {
                model.parse_mode = parseMode;
            }

            if (disableWebPagePreview.HasValue)
            {
                model.disable_web_page_preview = disableWebPagePreview.Value;
            }

            if (disableNotification.HasValue)
            {
                model.disable_notification = disableNotification.Value;
            }

            if (replyToMessageId.HasValue)
            {
                model.reply_to_message_id = replyToMessageId.Value;
            }

            if (!string.IsNullOrEmpty(replyMarkup) && !string.IsNullOrWhiteSpace(replyMarkup))
            {
                string[] replyMarkupArray = replyMarkup.Split(' ');
                if (replyMarkup == "0") // Main menu
                {
                    var keyboardMarkup = new InlineKeyboardMarkup();
                    var button1 = new InlineKeyboardButton("1. Harvest", "", "1", "");
                    var button2 = new InlineKeyboardButton("2. Pivotal Tracker", "", "2", "");
                    keyboardMarkup.inline_keyboard = new InlineKeyboardButton[,] { { button1, button2 } };
                    model.reply_markup = keyboardMarkup;
                }
                else if (replyMarkup == "1") // Harvest main menu
                {
                    var keyboardMarkup = new InlineKeyboardMarkup();
                    var button1 = new InlineKeyboardButton("1. Add a new time entry.", "", "1 1", "");
                    var button2 = new InlineKeyboardButton("2. Edit an existing time entry.", "", "1 2", "");
                    keyboardMarkup.inline_keyboard = new InlineKeyboardButton[,] { { button1}, { button2 } };
                    model.reply_markup = keyboardMarkup;
                }
                else if (replyMarkup == "1 1") // Harvest, add a new time entry, select a client
                {
                    if (harvestClient != null)
                    {
                        var keyboardMarkup = new InlineKeyboardMarkup();
                        var clients = harvestClient.ListClients().OrderBy(x => x.Name);
                        var buttonsArray = new InlineKeyboardButton[clients.Count(), 1];
                        int i = 0;
                        foreach (var client in clients)
                        {
                            var button = new InlineKeyboardButton((i + 1) + ". " + client.Name, "", "1 1 " + (i + 1), "");
                            buttonsArray[i, 0] = button;
                            i++;
                        }

                        keyboardMarkup.inline_keyboard = buttonsArray;
                        model.reply_markup = keyboardMarkup;
                    }
                }
                else if (replyMarkupArray.Length == 3)
                {
                    if (replyMarkupArray[0] == "1" && replyMarkupArray[1] == "1") // Harvest, add a new time entry, selected a client, select a project.
                    {
                        var keyboardMarkup = new InlineKeyboardMarkup();
                        int clientIndex = int.Parse(replyMarkupArray[2]) - 1;
                        var client = harvestClient.ListClients().OrderBy(x => x.Name).ElementAt(clientIndex);
                        var projects = harvestClient.ListProjects(client.Id).OrderBy(x => x.Name);
                        var buttonsArray = new InlineKeyboardButton[projects.Count(), 1];
                        int i = 0;
                        foreach (var project in projects)
                        {
                            var button = new InlineKeyboardButton((i + 1) + ". " + project.Name, "", "1 1 " + (clientIndex + 1) + " " + (i + 1), "");
                            buttonsArray[i, 0] = button;
                            i++;
                        }

                        keyboardMarkup.inline_keyboard = buttonsArray;
                        model.reply_markup = keyboardMarkup;
                    }
                }
                else if (replyMarkupArray.Length == 4)
                {
                    if (replyMarkupArray[0] == "1" && replyMarkupArray[1] == "1") // Harvest, add a new time entry, selected a client, selected a project, select a task.
                    {
                        var keyboardMarkup = new InlineKeyboardMarkup();
                        int clientIndex = int.Parse(replyMarkupArray[2]) - 1;
                        int projectIndex = int.Parse(replyMarkupArray[3]) - 1;
                        var client = harvestClient.ListClients().OrderBy(x => x.Name).ElementAt(clientIndex);
                        var project = harvestClient.ListProjects(client.Id).OrderBy(x => x.Name).ElementAt(projectIndex);
                        var taskAssignments = harvestClient.ListTaskAssignments(project.Id);
                        List<Task> tasks = new List<Task>();
                        foreach (var taskAssignment in taskAssignments)
                        {
                            var task = harvestClient.Task(taskAssignment.TaskId);
                            tasks.Add(task);
                        }
                        tasks = tasks.OrderBy(x => x.Name).ToList();
                        var buttonsArray = new InlineKeyboardButton[tasks.Count(), 1];
                        int i = 0;
                        foreach (var task in tasks)
                        {
                            string isBillible = "";
                            isBillible = task.BillableByDefault ? "Billable" : "Non-Billable";
                            if (task.Billable.HasValue)
                            {
                                isBillible = task.Billable.Value ? "Billable" : "Non-Billable";
                                
                            }
                            var button = new InlineKeyboardButton((i + 1) + ". " + task.Name + " - " + isBillible, "", "1 1 " + (clientIndex + 1) + " " + (projectIndex + 1) + " " + (i + 1), "");
                            buttonsArray[i, 0] = button;
                            i++;
                        }

                        keyboardMarkup.inline_keyboard = buttonsArray;
                        model.reply_markup = keyboardMarkup;
                    }
                                                
                                                
                }
                /*if (replyMarkup == "mainmenu")
                {
                    var keyboardMarkup = new InlineKeyboardMarkup();
                    var button1 = new InlineKeyboardButton("Harvest", "", "mainmenu_harvest", "");
                    var button2 = new InlineKeyboardButton("Pivotal Tracker", "", "mainmenu_pivotaltracker", "");
                    keyboardMarkup.inline_keyboard = new InlineKeyboardButton[1,2]{{button1, button2}};
                    model.reply_markup = keyboardMarkup;
                }
                else if (replyMarkup == "pivotaltracker_projects")
                {
                    var projects = Tracker.Projects.Project.GetProjects().OrderBy(x => x.Name);
                    var buttonsArray = new InlineKeyboardButton[projects.Count(), 1];

                    var keyboardMarkup = new InlineKeyboardMarkup();
                    int i = 0;
                    foreach (var project in projects)
                    {
                        var button = new InlineKeyboardButton(project.Name, "", "pivotaltracker_projects_" + project.Id, "");
                        buttonsArray[i,0] = button;
                        i++;
                    }

                    keyboardMarkup.inline_keyboard = buttonsArray;
                    model.reply_markup = keyboardMarkup;
                }
                else if (replyMarkup.Contains("pivotaltracker_project_actions_"))
                {
                    string projectId = replyMarkup.Substring(replyMarkup.IndexOf("pivotaltracker_project_actions_") + "pivotaltracker_project_actions_".Length);
                    var project = Tracker.Projects.Project.GetProject(int.Parse(projectId));

                    var keyboardMarkup = new InlineKeyboardMarkup();
                    var addStory = new InlineKeyboardButton("Add Story", "", "pivotaltracker_project_action_addstory_" + project.Id, "");
                    var estimateStory = new InlineKeyboardButton("Estimate Story", "", "pivotaltracker_project_action_estimatestory_" + project.Id, "");
                    var startStory = new InlineKeyboardButton("Start Story", "", "pivotaltracker_project_action_startstory_" + project.Id, "");
                    var finishStory = new InlineKeyboardButton("Finish Story", "", "pivotaltracker_project_action_finishstory_" + project.Id, "");
                    var deliverStory = new InlineKeyboardButton("Deliver Story", "", "pivotaltracker_project_action_deliverstory_" + project.Id, "");
                    var acceptStory = new InlineKeyboardButton("Accept Story", "", "pivotaltracker_project_action_acceptstory_" + project.Id, "");
                    var rejectStory = new InlineKeyboardButton("Reject Story", "", "pivotaltracker_project_action_rejectstory_" + project.Id, "");
                    var restartStory = new InlineKeyboardButton("Restart Story", "", "pivotaltracker_project_action_restartstory_" + project.Id, "");

                    keyboardMarkup.inline_keyboard = new InlineKeyboardButton[,] { 
                                                                                    { addStory, estimateStory }, 
                                                                                    { startStory, finishStory },
                                                                                    { deliverStory, acceptStory },
                                                                                    {rejectStory, restartStory}};
                    model.reply_markup = keyboardMarkup;
                } */
            }




            string postJson = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            });

            return MessageModel.FromJson(WebRequestHelper.MakeTelegramRequest(path, postJson).result);
        }


        public static MessageModel FromJson(dynamic json)
        {
            MessageModel model = new MessageModel();

            model.MessageId = json.message_id;

            try
            {
                model.From = UserModel.FromJson(json.from);
            }
            catch (Exception exception)
            {
                // From property is not in the json object
            }

            //model.Date = DateTime.ParseExact("1970-01-01", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture).AddMilliseconds(json.date);
            model.Chat = ChatModel.FromJson(json.chat);


            try
            {
                model.Text = json.text;
            }
            catch (Exception exception)
            {
                // Text property is not in the json object
            }

            return model;
        }
    }
}