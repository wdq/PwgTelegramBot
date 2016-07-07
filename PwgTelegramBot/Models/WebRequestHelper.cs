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