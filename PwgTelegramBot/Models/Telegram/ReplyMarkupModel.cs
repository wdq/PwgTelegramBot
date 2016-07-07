using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PwgTelegramBot.Models.Telegram
{
    public class InlineKeyboardButton
    {
        public string text { get; set; }
        public string url { get; set; }
        public string callback_data { get; set; }
        public string switch_inline_query { get; set; }

        public InlineKeyboardButton(string buttonText, string buttonUrl, string callbackData, string switchInlineQuery)
        {
            text = buttonText;
            url = buttonUrl;
            callback_data = callbackData;
            switch_inline_query = switchInlineQuery;
        }
    }
    public class InlineKeyboardMarkup
    {
        public InlineKeyboardButton[,] inline_keyboard { get; set; }
    }


    public class KeyboardButton
    {
        public string text { get; set; }
        public bool? request_contact { get; set; }
        public bool? request_location { get; set; }
    }
    public class ReplyKeyboardMarkup
    {
        public KeyboardButton[,] keyboard { get; set; }
        public bool? resize_keyboard { get; set; }
        public bool? one_time_keyboard { get; set; }
        public bool? selective { get; set; }
    }

    public class ReplyKeyboardHide
    {
        public bool HideKeyboard { get; set; }
        public bool? Selective { get; set; }
    }

    public class ForceReply
    {
        public bool IsForceReply { get; set; }
        public bool? Selective { get; set; }
    }
}