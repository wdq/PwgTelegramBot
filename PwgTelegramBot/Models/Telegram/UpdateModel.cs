using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PwgTelegramBot.Models.Telegram
{
    public class UpdateModel
    {
        public int UpdateId { get; set; }
        public MessageModel Message { get; set; }
        public MessageModel EditedMessage { get; set; }
        public InlineQueryModel InlineQuery { get; set; }
        public ChosenInlineResultModel ChosenInlineResult { get; set; }
        public CallbackQueryModel CallbackQuery { get; set; }

        public static UpdateModel FromJson(dynamic json)
        {
            UpdateModel model = new UpdateModel();

            model.UpdateId = json.update_id;

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
                model.EditedMessage = MessageModel.FromJson(json.edited_message);
            }
            catch (Exception exception)
            {
                // EditedMessage property is not in the json object
            }

            try
            {
                model.CallbackQuery = CallbackQueryModel.FromJson(json.callback_query);
            }
            catch (Exception exception)
            {
                // CallbackQuery property is not in the json object
            }

            return model;
        }
    }
}