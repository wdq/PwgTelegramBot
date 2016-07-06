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
    }
}