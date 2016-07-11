using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using PwgTelegramBot.Models.Telegram;

namespace PwgTelegramBot.Models
{
    public class WebRequestHelper
    {
        public static dynamic MakeTelegramRequest(string path, string postJson)
        {
            var request = (HttpWebRequest)WebRequest.Create("https://api.telegram.org/bot" + ConfigurationManager.AppSettings["TelegramBotToken"] + path);
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.Method = "POST";

            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                streamWriter.Write(postJson);
            }

            var response = (HttpWebResponse)request.GetResponse();
            Stream stream = response.GetResponseStream();
            StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            String responseString = reader.ReadToEnd();
            dynamic json = JsonConvert.DeserializeObject(responseString);

            return json;
        }

        private static string pivotalTrackerApiToken = ConfigurationManager.AppSettings["PivotalTrackerApiToken"];
        private static string harvestUsername = ConfigurationManager.AppSettings["HarvestUsername"];
        private static string harvestPassword = ConfigurationManager.AppSettings["HarvestPassword"];
        private static string harvestAccount = ConfigurationManager.AppSettings["HarvestAccountName"];


        public class HarvestOAuthRequest
        {
            public string refresh_token { get; set; }
            public string code { get; set; }
            public string client_id { get; set; }
            public string client_secret { get; set; }
            public string redirect_uri { get; set; }
            public string grant_type { get; set; }
        }

        public class HarvestOAuthResponse
        {
            public string TokenType { get; set; }
            public DateTime Expiration { get; set; }
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }
        }

        public class HarvestNewDailyEntry
        {
            public string notes { get; set; }
            public double hours { get; set; }
            public string project_id { get; set; }
            public string task_id { get; set; }
            public string spent_at { get; set; }
        }

        public static HarvestOAuthResponse PostHarvestOAuth(string authorizationCode, bool isRefresh)
        {
            string url = "https://" + ConfigurationManager.AppSettings["HarvestAccountName"] + ".harvestapp.com/oauth2/token";

            try
            {
                var harvestOAuthRequest = new HarvestOAuthRequest();
                harvestOAuthRequest.client_id = ConfigurationManager.AppSettings["HarvestClientID"];
                harvestOAuthRequest.client_secret = ConfigurationManager.AppSettings["HarvestClientSecret"];
                harvestOAuthRequest.redirect_uri = "https://pwgwebhooktestbot.quade.co/PwgTelegramBot/Bot/HarvestAuthRedirect";
                url += "?client_id=" + harvestOAuthRequest.client_id;
                url += "&client_secret=" + harvestOAuthRequest.client_secret;
                url += "&redirect_uri=" + harvestOAuthRequest.redirect_uri;

                if (isRefresh)
                {
                    harvestOAuthRequest.refresh_token = authorizationCode;
                    harvestOAuthRequest.grant_type = "refresh_token";
                    url += "&refresh_token=" + authorizationCode;
                    url += "&grant_type=" + harvestOAuthRequest.grant_type;
                }
                else
                {
                    harvestOAuthRequest.code = authorizationCode;
                    harvestOAuthRequest.grant_type = "authorization_code";
                    url += "&code=" + authorizationCode;
                    url += "&grant_type=" + harvestOAuthRequest.grant_type;
                }


                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Accept = "application/json";
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";

                /*using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(harvestOAuthRequest);
                }*/

                var response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                String responseString = reader.ReadToEnd();
                dynamic json = JsonConvert.DeserializeObject(responseString);

                //return json;

                HarvestOAuthResponse model = new HarvestOAuthResponse();
                model.TokenType = json.token_type;
                model.Expiration = DateTime.Now.AddSeconds((double)json.expires_in);
                model.AccessToken = json.access_token;
                model.RefreshToken = json.refresh_token;

                return model;
            }
            catch (Exception exception)
            {
                var exceptionMessage = MessageModel.SendMessage(47347293, "Exception in WebRequestHelper.cs PostHarvestOAuth(): " + exception.Message, "", null, null, null, null, null);
            }
            return new HarvestOAuthResponse();
        }


        public static dynamic PostHarvestDailyEntry(string accessToken, string notes, double hours, string projectId, string taskId, DateTime spentAt)
        {
            string url = "https://" + ConfigurationManager.AppSettings["HarvestAccountName"] + ".harvestapp.com/daily/add?access_token=" + accessToken;
            
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Accept = "application/json";
            request.Method = "POST";
            request.ContentType = "application/json";

            var entry = new HarvestNewDailyEntry();
            entry.notes = notes;
            entry.hours = hours;
            entry.project_id = projectId;
            entry.task_id = taskId;
            entry.spent_at = spentAt.ToString("yyyy-M-d");
            
            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                string postJson = JsonConvert.SerializeObject(entry, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                });
                streamWriter.Write(postJson);
            }
            var response = (HttpWebResponse)request.GetResponse();
            Stream stream = response.GetResponseStream();
            StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            String responseString = reader.ReadToEnd();
            dynamic json = JsonConvert.DeserializeObject(responseString);

            return json;
        }



        public static dynamic GetTrackerJson(string url)
        {
            var request = WebRequest.Create(url);
            request.ContentType = "application/json; charset=utf-8";
            request.Headers.Add("X-TrackerToken", pivotalTrackerApiToken);
            var response = (HttpWebResponse)request.GetResponse();
            Stream stream = response.GetResponseStream();
            StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            String responseString = reader.ReadToEnd();
            dynamic json = JsonConvert.DeserializeObject(responseString);

            return json;
        }

        public static dynamic PostTrackerJson(string url, string postJson)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Accept = "application/json";
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add("X-TrackerToken", pivotalTrackerApiToken);

            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                streamWriter.Write(postJson);
            }

            var response = (HttpWebResponse)request.GetResponse();
            Stream stream = response.GetResponseStream();
            StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            String responseString = reader.ReadToEnd();
            dynamic json = JsonConvert.DeserializeObject(responseString);

            return json;

        }

    }
}