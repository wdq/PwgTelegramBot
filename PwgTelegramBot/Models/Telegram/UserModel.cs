using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PwgTelegramBot.Models.Telegram
{
    public class UserModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }

        public static UserModel FromJson(dynamic json)
        {
            UserModel model = new UserModel();

            model.Id = json.id;
            model.FirstName = json.first_name;

            try
            {
                model.LastName = json.last_name;
            }
            catch (Exception exception)
            {
                // LastName property does not exist in json object
            }


            try
            {
                model.Username = json.username;
            }
            catch (Exception exception)
            {
                // Username property does not exist in json object
            }


            return model;
        }
    }
}