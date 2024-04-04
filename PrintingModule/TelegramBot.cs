using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Text;
using PrintingModule.EDS;

namespace PrintingModule
{
    public class TelegramBot
    {
        private readonly CancellationTokenSource cts;
        private readonly TelegramBotClient client;
        private readonly EDS.EDS? eds = null;
        private static TelegramBot? bot;
        readonly PrinterConnection connection = new();

        public static TelegramBot GetInstance()
        {
            bot ??= new TelegramBot();
            return bot;
        }

        private TelegramBot()
        {
            var token = "";
            if (System.IO.File.Exists($"./telegram.config"))
            {
                using var reader = new StreamReader($"./telegram.config");
                token = reader.ReadLine() ?? "";
            }
            cts = new CancellationTokenSource();
            client = new TelegramBotClient(token);

            if (System.IO.File.Exists($"./eds.config"))
            {
                using var reader = new StreamReader($"./eds.config");

                BigInteger p = new(reader.ReadLine(), 16);
                BigInteger a = new(reader.ReadLine(), 10);
                BigInteger b = new(reader.ReadLine(), 16);
                byte[] xG = EDS.EDS.FromHexStringToByte(reader.ReadLine()!);
                BigInteger n = new(reader.ReadLine(), 16);

                eds = new EDS.EDS(p, a, b, n, xG);
            }

            client.StartReceiving(new DefaultUpdateHandler(HandleUpdateAsync, HandleErrorAsync), null, cts.Token);
        }

        private Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var result = Task.CompletedTask;

            if (update.Message == null || update.Type != UpdateType.Message)
                return result;

            switch (update.Message.Type)
            {
                case MessageType.Text:
                    try
                    {
                        result = MessageHandler(update.Message);
                    }
                    catch
                    {
                        result = client.SendTextMessageAsync(update.Message.Chat.Id, "Произошла какая-то ошибка!",
                            cancellationToken: cancellationToken);
                    }
                    break;
                case MessageType.Document:
                    try
                    {
                        result = DocumentHandler(update.Message);
                    }
                    catch
                    {
                        result = client.SendTextMessageAsync(update.Message.Chat.Id, "Произошла какая-то ошибка!",
                            cancellationToken: cancellationToken);
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
            _ = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            return Task.CompletedTask;
        }

        private Task DocumentHandler(Message msg)
        {
            if (msg.Document != null)
            {
                var fileThread = client.GetFileAsync(msg.Document.FileId);
                fileThread.Wait();
                var res = fileThread.Result;

                using (FileStream stream = new("temp.gcode", FileMode.Create))
                {
                    var dowThread = client.DownloadFileAsync(res.FilePath!, stream);
                    dowThread.Wait();
                }
                List<string> data = [];

                var text = System.IO.File.ReadAllLines("temp.gcode");
                var sign = text[0];
                var publicKey = text[1];

                var allBytesStr = new StringBuilder();
                for (var i = 2; i < text.Length; i++)
                {
                    var str = text[i];
                    allBytesStr.AppendLine(str);
                    var commentIdx = str!.IndexOf(';');
                    if (commentIdx == 0)
                        continue;
                    if (commentIdx != -1)
                    {
                        str = str[..commentIdx];
                    }
                    if (string.IsNullOrWhiteSpace(str))
                        continue;
                    data.Add(str);
                }

                Stribog stribog = new(Stribog.Mode.m512);
                if (!eds!.VerifyDS(stribog.GetHash(allBytesStr.ToString().Select(x => (byte)x).ToArray()), sign, new CECPoint(publicKey)))
                {
                    return client.SendTextMessageAsync(msg.Chat.Id, "Wrong EDS please sign file");
                }

                var task = new Thread(() =>
                {
                    client.SendTextMessageAsync(msg.Chat.Id, connection.Print(data));
                });
                task.Start();
                return client.SendTextMessageAsync(msg.Chat.Id, "Printing");
            }
            return Task.CompletedTask;
        }

        private Task MessageHandler(Message msg)
        {
            if (msg.From == null || msg.Text == null)
                return Task.CompletedTask;

            var result = Task.CompletedTask;

            switch (msg.Text)
            {
                case "/start":
                    result = client.SendTextMessageAsync(msg.Chat.Id,
                        "Добрый день! Приложите файл с расширением gcode, содержащий электронную подпись, для отправки на печать.");
                    break;
                case "/stop":
                    result = client.SendTextMessageAsync(msg.Chat.Id,
                        "Регистрация удалена.");
                    break;
                case "/status":
                    result = client.SendTextMessageAsync(msg.Chat.Id,
                        $"{Math.Round(connection.pecentage, 2)}%");
                    break;

            }

            return result;
        }
    }
}
