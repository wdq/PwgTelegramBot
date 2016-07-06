using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using PwgTelegramBot.Models.Telegram;
using Telegram.Bot.Types.Enums;

namespace PwgTelegramBot.Controllers
{
    public class BotController : Controller
    {
        // GET: Bot
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Webhook()
        {
            StreamReader inputStream = new StreamReader(Request.InputStream);
            string inputString = inputStream.ReadToEnd();

            dynamic inputJson = JsonConvert.DeserializeObject(inputString);

            UpdateModel update = Models.Telegram.UpdateModel.FromJson(inputJson);

            var a = inputJson.message;
            var b = inputJson.message.message_id;

            if (update.Message != null)
            {
                var bot = new Telegram.Bot.Api(ConfigurationManager.AppSettings["TelegramBotToken"]);
                bot.SendTextMessageAsync(update.Message.Chat.Id, update.Message.Text);
            }


            JsonResult jsonResult = new JsonResult();
            jsonResult.Data = "ok";
            jsonResult.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            return jsonResult;
        }

        public JsonResult EnableBot()
        {
            var request = (HttpWebRequest)WebRequest.Create("https://api.telegram.org/bot" + ConfigurationManager.AppSettings["TelegramBotToken"] + "/setWebhook?url=https://pwgwebhooktestbot.quade.co/PwgTelegramBot/Bot/Webhook");
            request.ContentType = "application/json";
            request.Accept = "application/json";
            var response = (HttpWebResponse)request.GetResponse();
            Stream stream = response.GetResponseStream();
            StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            String responseString = reader.ReadToEnd();

            var result = new JsonResult();
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            result.Data = responseString;

            return result;
        }
    }
}