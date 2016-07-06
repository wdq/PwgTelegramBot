using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PwgTelegramBot.Models.Telegram
{
    public class InlineQueryModel
    {
        public string Id { get; set; }
        public UserModel From { get; set; }
        public LocationModel Location { get; set; }
        public string Query { get; set; }
        public string Offset { get; set; }
    }
}