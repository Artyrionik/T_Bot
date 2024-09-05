using System;
using System.Drawing;
using System.Net;
using System.Xml.Linq;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Polling;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Net.Mime.MediaTypeNames;
using TelegramBotFramework.Core.Objects;
using File = System.IO.File;

namespace T_Bot
{
    class Program
    {
        static List<Pattern> patterns;
        static bool QouteIsActive = false;
        const string firstMenu = "<b>Стартовое Меню</b>\n\n";
        const string secondMenu = "<b>Меню</b>\n\n";
        static string Weathermenu = $"<b>Погода на сегодня:\n</b>";

        static string NextButton = "Далее";
        static string BackButton = "Назад";
        static string WeatherButton = "Погода";
        static string FileSearch = "Поиск файла";
        static string Tutorial = "Туториал";
        static string FilePath = @"D:\TelegramBot\Archive\";

        
        static InlineKeyboardMarkup firstMenuMarkup = new(new[] { InlineKeyboardButton.WithCallbackData(NextButton), InlineKeyboardButton.WithCallbackData(FileSearch) });
        static InlineKeyboardMarkup WeatherMenuMarkup = new( new[] { InlineKeyboardButton.WithCallbackData(BackButton) });
        static InlineKeyboardMarkup secondMenuMarkup = new(
            new[] {
        new[] { InlineKeyboardButton.WithCallbackData(BackButton) },
        new[] { InlineKeyboardButton.WithUrl(Tutorial, "https://www.youtube.com/watch?v=dQw4w9WgXcQ") },
        new[]{InlineKeyboardButton.WithCallbackData(WeatherButton)}
            }
        );
        static InlineKeyboardMarkup DownloadMarkup;

        static TelegramBotClient bot;

        static void Main(string[] args)
        {
            int offset = 0;
            string token = System.IO.File.ReadAllText("token.txt");

            bot = new TelegramBotClient(token);            
            Update update = new();
            using var cts = new CancellationTokenSource();
            bot.StartReceiving(updateHandler: onUpdateReceived,errorHandler: HandleError,cancellationToken: cts.Token);
            
            Console.WriteLine("Start listening for updates. Press enter to stop");
            Console.ReadLine();
            cts.Cancel();

        }
        static string GetCurrentMonth()
        {
            int date = DateTime.Now.Month;
            switch (date)
            {
                case 1:
                    return "января";
                case 2:
                    return "февраля";
                case 3:
                    return "марта";
                case 4:
                    return "апреля";
                case 5:
                    return "мая";
                case 6:
                    return "июня";
                case 7:
                    return "июля";
                case 8:
                    return "августа";
                case 9:
                    return "сентября";
                case 10:
                    return "октября";
                case 11:
                    return "ноября";
                case 12:
                    return "декабря";
                default: return "Нет месяца";
            }
        }
        static string GetApproximateTime()
        {
            int time = DateTime.Now.Hour;
            if (time >= 10) return "12";
            else if (time >= 12) return "15";
            else if (time >= 15) return "18";
            else if (time >=18) return "21";
            else return "08";

        }
        async static Task onUpdateReceived(ITelegramBotClient bot,Update update,CancellationToken cts)
        {
            switch(update.Type)
            {
                case Telegram.Bot.Types.Enums.UpdateType.Message:
                    await onMessage(update.Message!);
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.CallbackQuery:
                    await HandleButton(update.CallbackQuery!);
                    break;
            }
        }
        async static Task onMessage(Message msg)
        {
            var msgtp = msg.Type;
            var text = msg.Text;
            var user = msg?.From;
            if (user is null) return;
            switch (msgtp)
            {
                case MessageType.Text:                  
                    Console.WriteLine($"Вы получили сообщение {text} от {user}");

                    if (text!.StartsWith("/"))
                    {
                        await HandleCommand(user.Id, text, user);
                    }
                    using (ApplicationContext db = new ApplicationContext())
                    {
                        UserEntity current_user = UserEntity.GetUserFromDb(user.Id);
                        if (current_user.IsSearching)
                        {

                             patterns = (from pat in db.Patterns
                                            where pat.Name!.Contains(text)
                                            select pat).ToList();
                            var kb = new List<InlineKeyboardButton[]>();
                            int size_of_buttons_list  = patterns.Count/2 + patterns.Count%2;
                            InlineKeyboardButton[][] buttons = new InlineKeyboardButton[size_of_buttons_list][];
                            int i = 0;
                            int j = 0;
                            for (int k = 0; k < size_of_buttons_list; k++)
                            {
                                buttons[k] = new InlineKeyboardButton[2];
                            }
                            if(patterns.Count%2 != 0)
                                buttons[^1]= new InlineKeyboardButton[1];
                            
                            foreach (var pat in patterns)
                            {
                                if (pat.Name.Length >31) 
                                pat.Name = pat.Name.Substring(0,32);
                                buttons[i][j] = InlineKeyboardButton.WithCallbackData(pat.Name);
                                Console.WriteLine($"Id:{pat.Id} Name:{pat.Name} Path:{pat.Path}");

                                if (i == size_of_buttons_list - 1 && patterns.Count % 2 != 0)
                                {
                                    kb.Add(buttons[i]);
                                    break;
                                }                                
                                if(j<1)
                                {
                                    j++;

                                }                                
                                else
                                {
                                    kb.Add(buttons[i]);
                                    i++; j = 0;

                                }                                
                            }
                            DownloadMarkup = kb.ToArray();
                            await bot.SendTextMessageAsync(user.Id, "<b>Выберите файл</b>\n\n", 0, ParseMode.Html, replyMarkup:DownloadMarkup);



                        }
                    }
                    break;
                case MessageType.Document:
                    Console.WriteLine($"Вы получили документ {msg!.Document!.FileName} от {user}");
                    Download(msg!.Document!.FileId,msg.Document.FileName!, FilePath);
                    break;
                case MessageType.Photo:                                           
                        DownloadPhoto(msg!,FilePath);
                    break;
                case MessageType.Video:
                    Console.WriteLine($"Вы получили видео {msg!.Video!.FileName} от {user}");
                    DownloadVideo(msg,FilePath);
                    break;
            }            
        }
        async static Task HandleError(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
        {
            await Console.Error.WriteLineAsync(exception.Message);
        }

        static async Task HandleCommand(long userId, string command, User user)
        {
            switch(command)
            {             
                case "/hi" :
                    await bot.SendTextMessageAsync(userId,$"Привет {user.FirstName ?? user.Username}");
                    break;
                case "/start":
                    UserEntity.SetDbEntity(userId); 
                    await bot.SendTextMessageAsync(userId, $"Привет {user.FirstName ?? user.Username}");
                    await SendMenu(userId, firstMenuMarkup,firstMenu);
                    break;
                case "/weather":
                   await WeatherThisWeek("https://pogoda7.ru/prognoz/gorod140330-Belarus-Vitsyebskaya_Voblasts-Polatsk/10days/full", user, userId);
                    break;
                case "/turnqouats":
                    Thread.Sleep(1000);
                    if (QouteIsActive)  GiveQoute("https://www.forbes.ru/forbeslife/dosug/262327-na-vse-vremena-100-vdokhnovlyayushchikh-tsitat", user, userId);
                    break;
            }
        }

        static async Task WeatherThisWeek(string Http, User user, long userId)
        {

            string trim = @"<div class=""grid current_name""><span class=""span_h2"">Сейчас  в Полоцке </span></div><div class=""clear""></div>  <div class=""current_data"">    <div class=""grid image"">      <img title=""";
            string cut = "УФ-индекс";
            HttpClient request = new HttpClient();
            var response = await request.GetAsync(Http);
            var str = await response.Content.ReadAsStringAsync();


            str = str.Substring(str.IndexOf(trim) + trim.Length);
            str = str.Remove(str.IndexOf(cut));
            Console.WriteLine(str);
            await bot.SendTextMessageAsync(userId, $"{user.FirstName} Вот погода на сегодня:\n {str}");
                
        }
        static async Task GiveQoute(string Http, User user, long userId)
        {
            while(QouteIsActive)
            {
                Random rnd = new Random();
                string trim = "data-index=\"9\" data-type=\"paragraph\" class=\"ywx5e Q0w8z\" style=\"text-align:left;";
                string cut = "</span></p> <!----> <!----><p itemprop=\"articleBody\" data-index=\"250\"";
                string trimParse = rnd.Next(70, 100).ToString() + @".&nbsp";
                string cutParse = "</b></span></p>";

                HttpClient request = new HttpClient();
                var response = await request.GetAsync(Http);
                var str = await response.Content.ReadAsStringAsync();
                /*StreamWriter streamWriter = new StreamWriter(FilePath + "File.txt");
                streamWriter.Write(str);
                streamWriter.Close();*/
                str = str.Substring(str.IndexOf(trim));
                str = str.Remove(str.IndexOf(cut));
                str = str.Substring(str.IndexOf(trimParse) + trimParse.Length);
                str = str.Remove(str.IndexOf(cutParse));
                //str = str.Replace(";<b>", "");
                if (str.Contains(".</span></p>"))
                {
                    if(str.IndexOf(".</span></p>") != 0)
                    {
                        str = str.Remove(str.IndexOf(".</span></p>"));
                    }                   
                }
                if(str.Contains(";"))
                {
                    if(str.Contains(";<b>")) { str = str.Substring(str.IndexOf(";<b>")+4); }
                    
                    else  str = str.Substring(str.IndexOf(';')+1);
                }
                await bot.SendTextMessageAsync(userId, str);
                Console.WriteLine(str);
                Thread.Sleep(10000);
            }
           

        }
        static async Task WeatherThisWeek(string Http)
        {
            string trim = @"<div class=""grid current_name""><span class=""span_h2"">Сейчас  в Полоцке </span></div><div class=""clear""></div>  <div class=""current_data"">    <div class=""grid image"">      <img title=""";
            string cut = "УФ-индекс";
            HttpClient request = new HttpClient();
            var response = await request.GetAsync(Http);
            var str = await response.Content.ReadAsStringAsync();

            str = str.Substring(str.IndexOf(trim) + trim.Length);
            str = str.Remove(str.IndexOf(cut));
            Console.WriteLine(str);
            Weathermenu = $"<b>Погода на сегодня:{str}\n</b>";

        }
        static async Task HandleButton(CallbackQuery menuQ)
        {
            string Text = string.Empty;
            InlineKeyboardMarkup markup = new(Array.Empty<InlineKeyboardButton>());
            try
            {
                UserEntity user = UserEntity.GetUserFromDb(menuQ.From.Id);
                if (user.IsSearching)
                {
                    foreach(var pat in patterns)
                    {
                        if (menuQ.Data.StartsWith(pat.Name!))
                        {
                            using FileStream fs = new FileStream(pat.Path, FileMode.Open, FileAccess.Read, FileShare.Inheritable);
                            {
                                await bot.SendDocumentAsync(menuQ.Message.Chat.Id,new InputFileStream(fs,pat.Name));
                            }
                            fs.Close();
                            
                        }
                    }
                    user.IsSearching = false;
                    UserEntity.UpdateUserDB(user);
                    Text = firstMenu;
                    markup = firstMenuMarkup;
                }
                else
                {
                    switch (menuQ.Data)
                    {
                        case "Далее":
                            Text = secondMenu;
                            markup = secondMenuMarkup;
                            break;
                        case "Назад":
                            Text = firstMenu;
                            markup = firstMenuMarkup;
                            break;
                        case "Погода":
                            await WeatherThisWeek("https://pogoda7.ru/prognoz/gorod140330-Belarus-Vitsyebskaya_Voblasts-Polatsk/10days/full");
                            //await bot.SendTextMessageAsync(menuQ.Message!.Chat.Id, "/weather");
                            Text = Weathermenu;
                            markup = WeatherMenuMarkup;
                            break;
                        case "Поиск файла":
                            Text = "Введите примерное имя файла";
                            user.IsSearching = true;
                            UserEntity.UpdateUserDB(user);
                            break;

                    }
                }
            }
            catch (Exception ex)
            {
                await bot.AnswerCallbackQueryAsync(menuQ.Id);
            }
            await bot.AnswerCallbackQueryAsync(menuQ.Id);

            await bot.EditMessageTextAsync(menuQ.Message!.Chat.Id,menuQ.Message.MessageId,Text,ParseMode.Html,replyMarkup:markup);
        }

        static async Task SendMenu(long userId,InlineKeyboardMarkup markup, string menu)
        {
            await bot.SendTextMessageAsync(userId, menu,0,ParseMode.Html, replyMarkup: markup);
        }


        static async void Download(string fileId, string fileName,string path)
        {
            try
            {
                var file = await bot.GetFileAsync(fileId);
                FileStream fs = new FileStream(path+fileName.ToLower(), FileMode.Create);
                await bot.DownloadFileAsync(file.FilePath!,fs);
                using (ApplicationContext db = new ApplicationContext())
                {
                    
                    db.Patterns.Update(new Pattern(fileName.ToLower(),path+fileName.ToLower()));
                    db.SaveChanges();
                }
                    fs.Close();
                fs.Dispose();
            }
            catch (Exception ex) 
            { 
                Console.WriteLine(ex.Message);
            }
            
        }  
        static async void DownloadPhoto(Message msg,string path)
        {
            try
            {               
                var file = await bot.GetFileAsync(msg.Photo![^1].FileId);
                Console.WriteLine($"Вы получили фото {file.FilePath} от {msg.From}");
                FileStream fs;
                await bot.DownloadFileAsync(file.FilePath!,fs = new FileStream(path + file.FilePath,FileMode.Create));
                fs.Close();
                fs.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
        static async void DownloadVideo(Message msg, string path)
        {
            try
            {
                var file = await bot.GetFileAsync(msg.Video!.FileId);
                Console.WriteLine($"Вы получили видео {file.FilePath} от {msg.From}");
                FileStream fs;
                await bot.DownloadFileAsync(file.FilePath!, fs = new FileStream(path + file.FilePath, FileMode.Create));
                fs.Close();
                fs.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }       

        }       
    }
}
