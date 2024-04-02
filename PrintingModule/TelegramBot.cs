using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace PrintingModule
{
    public class TelegramBot
    {
        private readonly CancellationTokenSource cts;
        private readonly TelegramBotClient client;
        private Dictionary<long, long> Users { get; set; } = [];
        private static TelegramBot? bot;
        private static readonly Dictionary<long, MenuItem> UserState = [];
        PrinterConnection connection = new PrinterConnection();
        public enum MenuItem
        {
            Start,
            Main,
        }
        public static TelegramBot getInstance()
        {
            bot ??= new TelegramBot();
            return bot;
        }

        private TelegramBot()
        {
            var token = "";
            if (System.IO.File.Exists($"./telegram.config"))
            {
                using (var reader = new StreamReader($"./telegram.config"))
                {
                    token = reader.ReadLine() ?? "";
                }
            }
            cts = new CancellationTokenSource();
            client = new TelegramBotClient(token);
            Users = new Dictionary<long, long>();

            client.StartReceiving(new DefaultUpdateHandler(HandleUpdateAsync, HandleErrorAsync), null, cts.Token);
        }

        private Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var result = Task.CompletedTask;
            switch (update.Type)
            {
                case UpdateType.Message:
                    if (update.Message != null && update.Message.Type == MessageType.Text)
                        try
                        {
                            result = MessageHandler(update);
                        }
                        catch (Exception ex)
                        {
                            result = client.SendTextMessageAsync(update.Message.Chat.Id,
                                "Произошла какая-то ошибка!", replyMarkup: new ReplyKeyboardRemove());
                        }
                    if (update.Message != null && update.Message.Type == MessageType.Document)
                        try
                        {
                            result = DocumentHandler(update);
                        }
                        catch (Exception ex)
                        {
                            result = client.SendTextMessageAsync(update.Message.Chat.Id,
                                "Произошла какая-то ошибка!", replyMarkup: new ReplyKeyboardRemove());
                        }
                    break;
                default:
                    result = Task.CompletedTask;
                    break;
            }

            return result;
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            return Task.CompletedTask;
        }

        private Task DocumentHandler(Update update)
        {
            var msg = update.Message;

            if (msg.Document != null)
            {
                //if (UserState[msg.Chat.Id] == MenuItem.Main)
                {
                    var fileThread = client.GetFileAsync(msg.Document.FileId);
                    fileThread.Wait();
                    var res = fileThread.Result;
                    using (FileStream stream = new FileStream("temp.gcode", FileMode.OpenOrCreate))
                    {
                        var dowThread = client.DownloadFileAsync(res.FilePath, stream);
                        dowThread.Wait();
                    }
                    List<string> data = [];
                    using (StreamReader reader = new StreamReader("temp.gcode"))
                    {
                        while (!reader.EndOfStream)
                        {
                            var str = reader.ReadLine();
                            var commentIdx = str.IndexOf(';');
                            if (commentIdx == 0)
                                continue;
                            if (commentIdx != -1)
                            {
                                str.Substring(0, commentIdx);
                            }
                            if (string.IsNullOrWhiteSpace(str))
                                continue;
                            data.Add(str);
                        }
                    }
                    var task = new Thread(() =>
                    {
                        client.SendTextMessageAsync(msg.Chat.Id, connection.Print(data));
                    });
                    task.Start();
                    return client.SendTextMessageAsync(msg.Chat.Id, "Printing");
                }
            }
            return Task.CompletedTask;
        }

        private Task MessageHandler(Update update)
        {
            var msg = update.Message;

            if (msg == null || msg.From == null || msg.Text == null)
                return Task.CompletedTask;

            var result = Task.CompletedTask;

            switch (msg.Text)
            {
                case "/start":
                    result = client.SendTextMessageAsync(msg.Chat.Id,
                        "Добрый день! Введите логин пользователя для входа:");
                    UserState[msg.Chat.Id] = MenuItem.Start;
                    break;
                case "/stop":
                    Users.Remove(Users.First(x => x.Value == msg.Chat.Id).Key);
                    result = client.SendTextMessageAsync(msg.Chat.Id,
                        "Регистрация успешно удалена.");
                    break;
                case "/status":
                    result = client.SendTextMessageAsync(msg.Chat.Id,
                        $"{Math.Round(connection.pecentage, 2)}%");
                    break;
                default:
                    if (!UserState.ContainsKey(msg.Chat.Id))
                    {
                        client.SendStickerAsync(msg.Chat.Id, InputFile.FromFileId("CAACAgIAAxkBAAIBmWFf8Ia0tHtyLUI9Pg2cfe2Pz87tAAIuAwACtXHaBqoozbmcyVK2IQQ"));
                        break;
                    }
                    switch (UserState[msg.Chat.Id])
                    {
                        case MenuItem.Start:
                            if (!UpdateUserTgId(msg.Text, msg.Chat.Id))
                            {
                                result = client.SendTextMessageAsync(msg.Chat.Id,
                                    "Неверный логин");
                                break;
                            }
                            result = client.SendTextMessageAsync(msg.Chat.Id,
                                "Добро пожаловать!\n");
                            UserState[msg.Chat.Id] = MenuItem.Main;
                            break;
                        default:
                            client.SendStickerAsync(msg.Chat.Id, InputFile.FromFileId("CAACAgIAAxkBAAIBmWFf8Ia0tHtyLUI9Pg2cfe2Pz87tAAIuAwACtXHaBqoozbmcyVK2IQQ"));
                            break;
                    }
                    break;

            }

            return result;
        }


        public bool UpdateUserTgId(string login, long chatId)
        {

            return true;
        }
    }
}
