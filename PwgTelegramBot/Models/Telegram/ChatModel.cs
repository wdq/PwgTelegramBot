using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PwgTelegramBot.Models.Telegram
{
    public class ChatModel
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public static ChatModel FromJson(dynamic json)
        {
            ChatModel model = new ChatModel();

            model.Id = json.id;
            model.Type = json.type;

            try
            {
                model.Title = json.title;
            }
            catch (Exception exception)
            {
                // Title property does not exist in json object
            }

            try
            {
                model.Username = json.username;
            }
            catch (Exception exception)
            {
                // Username property does not exist in json object
            }

            try
            {
                model.FirstName = json.first_name;
            }
            catch (Exception exception)
            {
                // FirstName property does not exist in json object
            }

            try
            {
                model.LastName = json.last_name;
            }
            catch (Exception exception)
            {
                // LastName property does not exist in json object
            }

            return model;
        }
    }
}