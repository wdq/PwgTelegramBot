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
            

            return model;
        }
    }
}