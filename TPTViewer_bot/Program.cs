using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TPTViewer_bot
{
    class Program
    {
        static void Main(string[] args)
        {
                Run().Wait();

        }

        private static Stream GetStreamFromUrl(string url)
        {
            byte[] imageData = null;

            using (var wc = new System.Net.WebClient())
                imageData = wc.DownloadData(url);

            return new MemoryStream(imageData);
        }

        public static List<string> Info(String ID)
        {
            try
            {
                var totalpage = 0;
                HttpWebRequest wReq;
                HttpWebResponse wRes;

                List<string> List = new List<string>();
                Uri uri = new Uri("http://powdertoy.co.uk/Browse/View.json?ID=" + ID); // string 을 Uri 로 형변환
                wReq = (HttpWebRequest)WebRequest.Create(uri); // WebRequest 객체 형성 및 HttpWebRequest 로 형변환
                wReq.Method = "GET"; // 전송 방법 "GET" or "POST"
                wReq.ServicePoint.Expect100Continue = false;
                wReq.CookieContainer = new CookieContainer();
                string res = null;

                using (wRes = (HttpWebResponse)wReq.GetResponse())
                {
                    Stream respPostStream = wRes.GetResponseStream();
                    StreamReader readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("EUC-KR"), true);

                    res = readerPost.ReadToEnd();
                }

                JsonTextParser parser = new JsonTextParser();
                JsonObject obj = parser.Parse(res);

                JsonUtility.GenerateIndentedJsonText = false;




                // enumerate values in json object
                Console.WriteLine();
                int i = 0;
                String[] View = null;
                View = new string[17];
                foreach (JsonObject field in obj as JsonObjectCollection)
                {
                    i++;
                    string name = field.Name;
                    string value = string.Empty;
                    string type = field.GetValue().GetType().Name;

                    // try to get value.
                    switch (type)
                    {
                        case "String":
                            value = (string)field.GetValue();
                            break;

                        case "Double":
                            value = field.GetValue().ToString();
                            break;

                        case "Boolean":
                            value = field.GetValue().ToString();
                            break;

                    }
                    View[i] = value;

                }

                List.Add("요청하신 ID:" + View[1] + "\r\n");
                List.Add("총점수:" + View[3] + "\r\n");
                List.Add("보트업:" + View[4] + "\r\n");
                List.Add("보트다운:" + View[5] + "\r\n");
                List.Add("조회수:" + View[6] + "\r\n");
                List.Add("제목:" + View[8] + "\r\n");
                List.Add("설명:" + View[9] + "\r\n");
                TimeSpan t = TimeSpan.FromSeconds(Convert.ToInt32(View[11]));
                int hour = t.Hours + 9;
                if (hour > 24)
                {
                    hour = hour - 24;
                    if (hour >= 12)
                        hour = hour + 12;
                }

                List.Add("업로드날짜:" + hour + "시" + t.Minutes + "분" + t.Seconds + "초" + "\r\n");
                List.Add("제작자:" + View[12] + "\r\n");
                List.Add("댓글개수:" + View[13] + "\r\n");
                if (Convert.ToInt32(View[13]) / 20 == 0)
                {
                    totalpage = Convert.ToInt32(View[13]);
                    List.Add("댓글 페이지 수:" + "1" + "\r\n");
                }
                else if (Convert.ToInt32(View[13]) / 20 == Convert.ToInt32(Convert.ToInt32(View[13]) / 20))
                {
                    int one = Convert.ToInt32(View[13]) / 20 + 1;
                    totalpage = one;
                    List.Add("댓글 페이지 수:" + one + "\r\n");
                }
                else
                {
                    totalpage = Convert.ToInt32(View[13]) / 20;
                    List.Add("댓글 페이지 수:" + Convert.ToInt32(View[13]) / 20 + "\r\n");
                }
                List.Add("공개세이브:" + View[14] + "\r\n");

                return List;
            }
            catch
            {
                return new List<string> {"알수없는 ID"};
            }
        }

        public static bool CheckNumber(string letter)
        {
            bool IsCheck = true;
            Regex numRegex = new Regex(@"[0-9]");
            Boolean ismatch = numRegex.IsMatch(letter);
            if (!ismatch)
            {
                IsCheck = false;
            }

            return IsCheck;
        }

        static async Task Run()
        {
            var Bot = new Api("Api");

            var me = await Bot.GetMe();

            Console.WriteLine("Hello my name is {0}", me.Username);
            var offset = 0;
            WebClient webClient = new WebClient();
            TcpClient MinecraftServer = new TcpClient();
            while (true)
            {
                var updates = await Bot.GetUpdates(offset);
                foreach (var update in updates)
                {
                    if (update.Message.Text == "/start")
                    {
                        Console.WriteLine(update.Message.Chat.Username + ": " + update.Message.Text);
                        await Bot.SendChatAction(update.Message.Chat.Id, ChatAction.Typing);
                        await Bot.SendTextMessage(update.Message.Chat.Id, "파토 세이브 ID 를 입력하세요");
                    }
                    else
                    {
                        if (CheckNumber(update.Message.Text))
                        {
                            await Bot.SendChatAction(update.Message.Chat.Id, ChatAction.UploadPhoto);

                            try
                            {
                                webClient.DownloadFile("http://static.powdertoy.co.uk/" + update.Message.Text + ".png", "Photo" + update.Message.Chat.Id + ".png");
                            }
                            catch
                            {
                                await Bot.SendChatAction(update.Message.Chat.Id, ChatAction.Typing);
                                await Bot.SendTextMessage(update.Message.Chat.Id, "알수없는 ID");
                                goto Error;
                            }
                            using (var stream = System.IO.File.Open("Photo" + update.Message.Chat.Id + ".png", FileMode.Open))
                            {
                                var rep = await Bot.SendPhoto(update.Message.Chat.Id, new FileToSend("Photo" + update.Message.Chat.Id + ".png", stream));
                            }
                            System.IO.File.Delete("Photo" + update.Message.Text + ".png");
                            List<string> info1 = Info(update.Message.Text);

                            string dogCsv = string.Join("", info1.ToArray());
                            await Bot.SendChatAction(update.Message.Chat.Id, ChatAction.Typing);
                            await Bot.SendTextMessage(update.Message.Chat.Id, dogCsv);
                        }
                        else
                        {
                            await Bot.SendChatAction(update.Message.Chat.Id, ChatAction.Typing);
                            await Bot.SendTextMessage(update.Message.Chat.Id, "파토 세이브 ID 를 입력하세요");
                        }
                    }
                    Error:
                    offset = update.Id + 1;
                }

            }
        }
    }
}
