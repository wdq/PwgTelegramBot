using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PwgTelegramBot.Models.Telegram
{
    public class StickerModel
    {
        public string FileId { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public PhotoSizeModel Thumb { get; set; }
        public string Emoji { get; set; }
        public int FileSize { get; set; }
    }
}