using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PwgTelegramBot.Models.Telegram
{
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