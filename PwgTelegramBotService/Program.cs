using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Net;

namespace PwgTelegramBotService
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.Write("Running...");
                try
                {
                    var request = (HttpWebRequest)WebRequest.Create("http://localhost/PwgTelegramBot/Bot/BotCron");
                    request.Accept = "application/json";
                    request.Method = "POST";
                    request.ContentLength = 0;

                    var response = (HttpWebResponse) request.GetResponse();
                    Console.WriteLine("Ok");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error");
                }


                Thread.Sleep(TimeSpan.FromMinutes(5));
            }
        }
    }
}
