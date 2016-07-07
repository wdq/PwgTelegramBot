using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PwgTelegramBot.Models.Telegram
{
    public class CallbackQueryModel
    {
        public string Id { get; set; }
        public UserModel From { get; set; }
        public MessageModel Message { get; set; }
        public string InlineMessageId { get; set; }
        public string Data { get; set; }

        public static CallbackQueryModel FromJson(dynamic json)
        {
            CallbackQueryModel model = new CallbackQueryModel();

            model.Id = json.id;
            model.From = UserModel.FromJson(json.from);

            try
            {
                model.Message = MessageModel.FromJson(json.message);
            }
            catch (Exception exception)
            {
                // Message property is not in the json object
            }

            try
            {
                model.InlineMessageId = json.inline_message_id;
            }
            catch (Exception exception)
            {
                // InlineMessageId property is not in the json object
            }

            model.Data = json.data;

            return model;
        }
    }
}