using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PwgTelegramBot.Models.Telegram
{
    public class ChosenInlineResultModel
    {
        public string ResultId { get; set; }
        public UserModel From { get; set; }
        public LocationModel Location { get; set; }
        public string InlineMessageId { get; set; }
        public string Query { get; set; }
    }
}