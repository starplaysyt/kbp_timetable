using Newtonsoft.Json;
using System.Data;
using System.Net;
using System.Runtime.CompilerServices;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;

namespace TelegramTestBot
{
    internal class Program
    {
        public const string ADMIN_USERNAME = ""; //admin username without a '@'
        public const string BOT_TOKEN = ""; //bot token


        public static int countOfUses = 0;
        public static bool isWorkingOnIt = false;
        public static bool isListingSenders = false;
        public static ParcerCondition parcerCondition = ParcerCondition.Success;
        static ITelegramBotClient bot = new TelegramBotClient(BOT_TOKEN);
        public static string lessonsNextDay = "";
        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                var message = update.Message;
                bool isAdminPass = false;
                if (message.Text == null)
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Not a command");
                    return;
                }

                if (message.Chat.Username == null) return;
                
                StreamWriter writeLog = new StreamWriter(new FileStream("log.txt", FileMode.Append, FileAccess.Write));
                writeLog.WriteLine(message.Chat.FirstName + " " + message.Chat.LastName + " issued command or text: " + message.Text);
                writeLog.Close(); //ВНИМАНИЕ ЕБАТЬ, если не писать сообщения в логе от администратора, будет пиздец. Нужно изменить CLEARLOG чтобы избежать ошибки пустого сообщения от Телеграма

                if (message.Chat.Username == ADMIN_USERNAME)
                {
                    try
                    {
                        switch (message.Text.ToLower().Trim())
                        {
                            case "/sudo workingonit": Function_WorkingOnIt(botClient, message); return;
                            case "/sudo dolistsenders": Function_DoListSenders(botClient, message); return;
                            case "/sudo runloader": Function_RunLoader(botClient, message); return;
                            case "/sudo runparcer": Function_RunParcer(botClient, message); return;
                            case "/sudo help": Function_SudoHelp(botClient, message); return;
                            case "/sudo": Function_SudoHelp(botClient, message); return;
                            case "/sudo saveadminchat": Function_SaveAdminChat(botClient, message); return;
                            case "/sudo log show": Function_LOG_ShowLog(botClient, message); return;
                            case "/sudo log clear": Function_LOG_ClearLog(botClient, message); return;
                        }
                        if (message.Text.Contains("/accept"))
                        {
                            string fileName = message.Text.Split("/accept ")[1];
                            if (System.IO.File.Exists("uhomework/" + fileName))
                            {
                                string str = System.IO.File.ReadAllText("uhomework/" + fileName);
                                StreamWriter strwr = new StreamWriter(new FileStream("ahomework/" + fileName.Split(" ")[0], FileMode.Create, FileAccess.Write));
                                strwr.WriteLine(str);
                                strwr.Close();
                                System.IO.File.Delete("uhomework/" + fileName);
                                await botClient.SendTextMessageAsync(message.Chat, "Заявка " + str + "одобрена.");
                            }
                            else await botClient.SendTextMessageAsync(message.Chat, "Заявка не найдена");
                            return;
                        }
                    }
                    catch
                    { await botClient.SendTextMessageAsync(message.Chat, "Отменено, произошла ошибка."); }
                }
                try
                {
                    if (isWorkingOnIt == false && message.Text.ToLower().Contains("/sudo") == false)
                    {
                        switch (message.Text.ToLower())
                        {
                            case "/tt": Function_ShowTodayTimeTable(botClient, message); return;
                            case "/ttn": Function_ShowNextDayTimeTable(botClient, message); return;
                            case "/start": Function_ShowStart(botClient, message); return;
                            case "/hw": Function_ShowHomework(botClient, message); return;
                                //существование сплита в addhomework контрить Contains 
                        }
                        if (message.Text.Contains("/hwadd"))
                        {
                            Function_HomeworkHelpStandBy(botClient, message); return;
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "Неправильная команда");
                        }
                    }
                    else if (message.Text.ToLower().Contains("/sudo") == false) await botClient.SendTextMessageAsync(message.Chat, "Ведутся технические работы. Ожидайте подключения основной программы.");
                    else await botClient.SendTextMessageAsync(message.Chat, "Нет доступа к команде. Обратитесь к администратору и/или управляющему органу.");
                } 
                catch
                { await botClient.SendTextMessageAsync(message.Chat, "Отменено, произошла ошибка."); }
            }
        }

        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Некоторые действия
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }

        #region Command Area

        public async static void Function_WorkingOnIt(ITelegramBotClient botClient, Message message)
        {
            isWorkingOnIt = !isWorkingOnIt;
            await botClient.SendTextMessageAsync(message.Chat, "Working on it system is " + (isWorkingOnIt ? "ENABLED" : "DISABLED"));
        }

        public async static void Function_DoListSenders(ITelegramBotClient botClient, Message message)
        {
                isListingSenders = !isListingSenders;
                await botClient.SendTextMessageAsync(message.Chat, "Listing senders system is " + (isListingSenders ? "ENABLED" : "DISABLED"));
        }

        public async static void Function_RunLoader(ITelegramBotClient botClient, Message message)
        {
            
                string output = loaderStby();
                await botClient.SendTextMessageAsync(message.Chat, output.Equals("Success") ? "Site file successfully loaded to internal file." : output);
            
        }

        public async static void Function_RunParcer(ITelegramBotClient botClient, Message message)
        {
            string output = parcerStby(); //CHANGE BACK
            await botClient.SendTextMessageAsync(message.Chat, "Successfully parcered, output: " + output);
            string[] files = Directory.GetFiles("ahomework");
            for (int i = 0; i < files.Length; i++)
            {
                System.IO.File.Delete(files[i]);
            }
            files = Directory.GetFiles("uhomework");
            for (int i = 0; i < files.Length; i++)
            {
                System.IO.File.Delete(files[i]);
            }
            string input = System.IO.File.ReadAllText("nextday.txt");
            if (input.Trim() == "No lessons")
            {
                await botClient.SendTextMessageAsync(message.Chat, "No lessons next day");
                return;
            }
            for (int i = 0; i < input.Split(" - ").Length; i++)
            {
                if(input.Split(" - ")[i] != "")
                {
                    string inLesson = input.Split(" - ")[i].Split("&")[0].Trim();
                    if (inLesson != "")
                    {
                        Console.WriteLine(inLesson);
                        StreamWriter strwr = new StreamWriter(new FileStream("ahomework/" + inLesson, FileMode.Create, FileAccess.Write));
                        strwr.Close();
                    }
                }
            }
            for (int i = 0; i < Directory.GetFiles("ahomework").Length; i++)
            {
                lessonsNextDay += Directory.GetFiles("ahomework")[i] + " ";
            }
        }

        public async static void Function_SudoHelp(ITelegramBotClient botClient, Message message)
        {
                string output =
                    "1. workingOnIt - service workin' activate \n" +
                    "2. dolistsenders - do listing senders \n" +
                    "3. runloader - start load from site program \n" +
                    "4. runparcer - start parcing already loaded site file \n" +
                    "5. saveadminchat - save chat for logs \n" +
                    "6. log show - show log \n" +
                    "7. log clear - clear log \n" +
                    "";
            await botClient.SendTextMessageAsync(message.Chat, output);
        }

        public async static void Function_SaveAdminChat(ITelegramBotClient botClient, Message message)
        {
                StreamWriter strwr = System.IO.File.CreateText("adminchat.txt");
                strwr.Write(JsonConvert.SerializeObject(message.Chat));
                strwr.Close();
                await botClient.SendTextMessageAsync(message.Chat, "Admin chat saved.");
                await botClient.SendTextMessageAsync(message.Chat, "Verify started.");
                StreamReader strrd = System.IO.File.OpenText("adminchat.txt");
                string output = strrd.ReadToEnd();
                Chat chat = JsonConvert.DeserializeObject<Chat>(output);
                await botClient.SendTextMessageAsync(chat, "Verify completed.");
                strrd.Close();
        }

        public async static void Function_LOG_ShowLog(ITelegramBotClient botClient, Message message)
        {
                StreamReader srd = System.IO.File.OpenText("log.txt");
                string output = srd.ReadToEnd();
                srd.Close();
                try
                {
                    await botClient.SendTextMessageAsync(message.Chat, output);
                } catch { await botClient.SendTextMessageAsync(message.Chat, "Fuck you, maybe log is too long for outputing."); }
        }

        public async static void Function_LOG_ClearLog(ITelegramBotClient botClient, Message message)
        {
                FileStream fileStream = new FileStream("log.txt", FileMode.Create, FileAccess.Write);
                fileStream.Close();
                await botClient.SendTextMessageAsync(message.Chat, "Log cleared.");
        }

        //end admin commands
        public async static void Function_ShowStart(ITelegramBotClient botClient, Message message)
        {
            await botClient.SendTextMessageAsync(message.Chat, "Ку, данный бот будет показывать расписание на текущий и на следующий день для группы Т-294 КБиПа. \nДля вывода расписания на текущий день введи /tt. \nДля вывода расписания на следующий день введи /ttn.");
        }

        public async static void Function_ShowTodayTimeTable(ITelegramBotClient botClient, Message message)
        {
            if (!System.IO.File.Exists("baseday.txt")) { return; }
            StreamReader todayReader = new StreamReader(new FileStream("baseday.txt", FileMode.Open, FileAccess.Read));
            string sout = todayReader.ReadToEnd();
            todayReader.Close();
            string output = "";
            if (sout.Trim() == "No lessons") await botClient.SendTextMessageAsync(message.Chat, sout);
            else
            {
                string[] lines = sout.Split(" - ");
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim() == "") output += (i + 1).ToString() + ". \n";
                    else
                        output += (i + 1).ToString() + ". " + lines[i].Split("&")[0] + " каб. " + lines[i].Split("&")[1] + "\n";
                }
                await botClient.SendTextMessageAsync(message.Chat, output);
            }
        }

        public async static void Function_ShowHomework(ITelegramBotClient botClient, Message message)
        {
            if (System.IO.File.ReadAllText("nextday.txt").Trim() != "No lessons")
            {
                string[] files = Directory.GetFiles("ahomework");
                string genstring = "Домашнее задание на следующий день: \n";
                for (int i = 0; i < files.Length; i++)
                {
                    genstring += files[i].Split("\\")[1] + " : " + System.IO.File.ReadAllText(files[i]).Trim() + "\n";
                }
                genstring += "Напоминаю, что вы можете помочь в дополнении информации по домашнему заданию. Если у вас есть какая-нибудь информация, вбивайте /hwadd (предмет) (задание).";
                await botClient.SendTextMessageAsync(message.Chat, genstring);
            }
            else botClient.SendTextMessageAsync(message.Chat, "Завтра нет уроков.");
        }
        public async static void Function_ShowNextDayTimeTable(ITelegramBotClient botClient, Message message)
        {
            StreamReader todayReader = new StreamReader(new FileStream("nextday.txt", FileMode.Open, FileAccess.Read));
            string sout = todayReader.ReadToEnd();
            todayReader.Close();
            string output = "";
            if (sout.Trim() == "No lessons") await botClient.SendTextMessageAsync(message.Chat, sout);
            else
            {
                string[] lines = sout.Split(" - ");
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim() == "") output += (i + 1).ToString() + ". \n";
                    else
                        output += (i + 1).ToString() + ". " + lines[i].Split("&")[0] + " каб. " + lines[i].Split("&")[1] + "\n";
                }
                await botClient.SendTextMessageAsync(message.Chat, output);
            }
        }

        public async static void Function_HomeworkHelpStandBy(ITelegramBotClient botClient, Message message)
        {
            //hwadd <имя предмета> <строка>
            string inMessage = message.Text;
            if (inMessage == "/hwadd") { botClient.SendTextMessageAsync(message.Chat, "Отправляет домашнее задание боту. Синтаксис: /hwadd (предмет) (задание). Список предметов можно посмотреть введя /ttn. Добавлять задания можно только на следующий день."); return; }
            string username = message.Chat.Username;
            string command = message.Text.Split(" ")[0];
            string secondArg = message.Text.Split(" ")[1];
            if (command == "/hwadd")
            {
                if (lessonsNextDay.Contains(secondArg))
                {
                    if (System.IO.File.ReadAllText("nextday.txt").Trim() != "No lessons")
                    {
                        StreamWriter strwr = new StreamWriter(new FileStream("uhomework/" + secondArg + " " + username, FileMode.Create, FileAccess.Write));
                        strwr.WriteLine(inMessage.Replace(command + " " + secondArg + " ", ""));
                        strwr.Close();
                        await botClient.SendTextMessageAsync(message.Chat, "Отправлено задание к предмету " + secondArg + ". Ожидайте подтверждения.");
                        StreamReader strrd = System.IO.File.OpenText("adminchat.txt");
                        string output = strrd.ReadToEnd();
                        Chat chat = JsonConvert.DeserializeObject<Chat>(output);
                        strrd.Close();
                        await botClient.SendTextMessageAsync(chat, "Got homework for lesson: " + secondArg + "\n" + "Sender: " + username + "\n" + "Data: " + inMessage.Replace(command + " " + secondArg + " ", "") + "\nUsername to accept:");
                        await botClient.SendTextMessageAsync(chat, "/accept " + secondArg + " " + username);
                    }
                    else botClient.SendTextMessageAsync(message.Chat, "Завтра нет уроков.");
                }
                else botClient.SendTextMessageAsync(message.Chat, "Ошибка комманды. Проверьте синтаксис: /hwadd (предмет) (задание). Задания можно отправлять только на следующий день. Список предметов можно посмотреть вв" +
                    "едя /ttn. Если и после этого не работает значит всё пошло по пизде. Обратитесь к администратору.");
            }
            else botClient.SendTextMessageAsync(message.Chat, "Ошибка комманды. Проверьте синтаксис: /hwadd (предмет) (задание)");
        }

        #endregion // here base all commands for the bot

        public static string GenerateDefaultFile(string username, string firstname, string lastname)
        {
            return "<username>" + username + "</username>\n" +
                "<firstname>" + firstname + "</firstname>\n" +
                "<lastname>" + lastname + "</lastname>\n" +
                "<hasadminrights>false</hasadminrights>\n" +
                "<AOGpoints>0</AOGpoints>\n" +
                "<homeworkMessagesSent>0</homeworkMessagesSent>\n";
        }

        public static string GetAttributeOfUser(string attribute, string username)
        {
                string data = System.IO.File.ReadAllText("users/" + username + ".dat");
                return data.Split("<" + attribute + ">")[1].Split("</" + attribute + ">")[0];
        }

        static void Main(string[] args)
        {
            if (BOT_TOKEN == "" || BOT_TOKEN == null)
            {
                Console.WriteLine("Error: bot token is null or empty");
                return;
            }
            if (ADMIN_USERNAME == "" || ADMIN_USERNAME == null)
            {
                Console.WriteLine("Error: Admin username is null or empty");
                return;
            }
            if (!Directory.Exists("users"))
            {
                Directory.CreateDirectory("users");
                Console.WriteLine("Users directory does not exists. Creating a new one");
            }

            if (!Directory.Exists("uhomework"))
            {
                Directory.CreateDirectory("uhomework");
                Console.WriteLine("Unaccepted homework directory does not exists. Creating a new one");
            }

            if (!Directory.Exists("ahomework"))
            {
                Directory.CreateDirectory("ahomework");
                Console.WriteLine("Accepted homework directory does not exists. Creating a new one");
            }

            for (int i = 0; i < Directory.GetFiles("ahomework").Length; i++)
            {
                lessonsNextDay += Directory.GetFiles("ahomework")[i] + " ";
            }

            Console.WriteLine("Запущен бот " + bot.GetMeAsync().Result.FirstName);
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, // receive all update types
            };
            bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );
            Console.ReadLine();
        }

        static string parcerStby()
        {
            DateTime dateTime = DateTime.Now;
            DayOfWeek todayday = dateTime.DayOfWeek;
            string baseday, nextday;
            if (todayday == DayOfWeek.Saturday) 
            { 
                baseday = getInfoForDay(6); nextday = "No lessons"; 
            }
            else
            {
                if (todayday == DayOfWeek.Sunday)
                {
                    baseday = "No lessons"; nextday = getInfoForDay(1); 
                }
                else
                {
                    baseday = getInfoForDay((int)todayday); nextday = getInfoForDay((int)todayday + 1);
                }
            }
            StreamWriter writerBaseDay = new StreamWriter(new FileStream("baseday.txt", FileMode.Create, FileAccess.Write));
            writerBaseDay.WriteLine(baseday);
            StreamWriter writerNextDay = new StreamWriter(new FileStream("nextday.txt", FileMode.Create, FileAccess.Write));
            writerNextDay.WriteLine(nextday);
            writerNextDay.Close();
            writerBaseDay.Close();
            return baseday + "#" + nextday;
        }

        static string loaderStby()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    string htmlCode = client.DownloadString("https://kbp.by/rasp/timetable/view_beta_kbp/?page=stable&cat=group&id=15");
                    StreamWriter strwrt = new StreamWriter(new FileStream("sitefile.txt", FileMode.Create, FileAccess.Write));
                    strwrt.WriteLine(htmlCode);
                    strwrt.Close();
                }
                return "Success";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        static string getInfoForDay(int day)
        {
            string strBase = "";
            try
            {
                StreamReader strrd = new StreamReader(new FileStream("sitefile.txt", FileMode.Open, FileAccess.Read));
                strBase = strrd.ReadToEnd();
                strrd.Close();
            }
            catch
            {
                parcerCondition = ParcerCondition.IOLoadFail;
                return "failure";
            }

            string ret = "";
            for (int i = 1; i <= 8; i++)
            {
                int l = i, col = day;
                string[] firstParse = strBase.Split("<tr>", StringSplitOptions.None);//splitting site data for lines
                string line = firstParse[l + 1]; //LINE ID +1
                string[] lineDefeatShit = line.Split("<td class=\"number\">" + l + "</td>"); //defeating numeration in the begin of the line. AFTER NUMBER TYPE LINE ID
                string[] lineParceToBlocks = lineDefeatShit[1].Split("<td>");
                string block = lineParceToBlocks[col]; // IN MASSIVE ID TYPE COLUMN ID
                if (!block.Contains("added") && !block.Contains("empty-pair"))
                {
                    string block1Data = block; //id 1
                    string leftColumnData = block1Data.Split("<div class=\"left-column\">", StringSplitOptions.None)[1].Split("</div>", StringSplitOptions.None)[0].Split('>')[2].Split('<')[0];
                    string rightColumnData = block1Data.Split("<div class=\"place\">")[1].Split("</div>")[0].Split('>')[1].Split('<')[0];
                    ret += leftColumnData + "&" + rightColumnData + " - ";
                }
                if (block.Contains("added") && block.Contains("empty-pair"))
                {
                    string rightColumnData = block.Split("<div class=\"place\">")[1].Split("</div>")[0].Split('>')[1].Split('<')[0];
                    string blockData = block.Split("<div class=\"left-column\">", StringSplitOptions.None)[1].Split("</div>")[0];
                    string leftColumnData = blockData.Split('>')[2].Split('<')[0];
                    ret += leftColumnData + "&" + rightColumnData + " - ";
                }
                else
                {
                    if (block.Contains("added"))
                    {
                        string rightColumnData = block.Split("<div class=\"place\">")[1].Split("</div>")[0].Split('>')[1].Split('<')[0];
                        string block1Data1 = block.Split("<div class=\"left-column\">", StringSplitOptions.None)[2].Split("</div>")[0];
                        string leftColumnData1 = block1Data1.Split('>')[2].Split('<')[0];
                        ret += leftColumnData1 + "&" + rightColumnData + " - ";
                    }
                    if (block.Contains("empty-pair"))
                    {
                        ret += "" + " - ";
                    }
                }
            }
            return ret;
        }

        public enum ParcerCondition
        {
            IOLoadFail, ParceFail, Success
        }
    }
}