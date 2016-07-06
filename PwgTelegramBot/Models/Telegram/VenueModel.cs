using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PwgTelegramBot.Models.Telegram
{
    public class VenueModel
    {
        public LocationModel Location { get; set; }
        public string Title { get; set; }
        public string Address { get; set; }
        public string FoursquareId { get; set; }
    }
}