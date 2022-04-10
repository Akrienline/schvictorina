using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SchVictorina.WebAPI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;

namespace SchVictorina.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class TelegramBotController : ControllerBase
    {
        static DiscordSocketClient socketClient;
        static TelegramBotClient botClient;
        static DefaultUpdateReceiver updateReceiver;

        public TelegramBotController(ILogger<TelegramBotController> logger)
        {
        }

        static TelegramBotController()
        {
            var timer = new Timer();
            timer.Interval = 1000000000;
            timer.Elapsed += Timer_Elapsed;

            socketClient = new DiscordSocketClient();
            socketClient.LoginAsync(TokenType.Bot, GlobalConfig.Instance.DiscordBot.Token, true);
            socketClient.MessageReceived += DiscordProcessing.ProcessEvent;
            socketClient.ButtonExecuted += DiscordProcessing.ProcessButtonExecute;
            socketClient.SlashCommandExecuted += DiscordProcessing.ProcessSlashExecute;
            socketClient.Ready += ProcessCommands;
            socketClient.StartAsync();
            //TelegramProcessing.GetUserByUsername("alekami649").Role = UserConfig.UserRole.Administrator;
            var settings = GlobalConfig.Instance;
            botClient = new TelegramBotClient(settings.TelegramBot.Token);
            if ((settings.TelegramBot?.Webhook?.Enabled ?? false) == false)
            {
                updateReceiver = new DefaultUpdateReceiver(botClient);
                updateReceiver.ReceiveAsync(new TelegramProcessing.MainUpdateHandler());
            }

            ButtonConfig.ButtonListChanged += delegate
            {
                SetMyCommands();
            };
            SetMyCommands();
        }

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            TestServer();
        }

        private static async Task ProcessCommands()
        {
            await DiscordProcessing.ProcessCommands(socketClient);
        }

        private static async void SetMyCommands()
        {
            var themeCommand = new BotCommand() { Command = "/themes", Description = "Список доступных тем на сегодня" };
            var infoCommand = new BotCommand { Command = "/user", Description = "Информация о себе" };

            var buttonsCommands = ButtonConfig.AllButtons.Where(x => x.Value.ID != x.Value.AutoID).Where(x => x.Value.IsValidWithAscender).Select(x => new BotCommand
            {
                Command = x.Value.ID,
                Description = x.Value.LabelWithParents
            });

            await botClient.DeleteMyCommandsAsync();
            await botClient.SetMyCommandsAsync(buttonsCommands.Prepend(infoCommand).Prepend(themeCommand));
        }

        [HttpGet]
        public string Start() 
        {
            return "Started";
        }
        private static void TestServer()
        {
            var webClient = new WebClient();
            webClient.DownloadString("https://schvictorina2.somee.com/bot/telegrambot/start");
        }

        [HttpGet]
        public string ClearLog(string type)
        {
            var filePath = type switch
            {
                "errors" => GlobalConfig.Instance?.Logging?.Errors?.Path,
                "requests" => GlobalConfig.Instance?.Logging?.Requests?.Path,
                _ => throw new ArgumentException()
            };
            if (filePath != null && System.IO.File.Exists(filePath))
            {
                System.IO.File.WriteAllText(filePath, string.Empty);
                return "clear";
            }
            return "no file";
        }

        [HttpGet]
        public string GetLog(string type)
        {
            var filePath = type switch
            {
                "errors" => GlobalConfig.Instance?.Logging?.Errors?.Path,
                "requests" => GlobalConfig.Instance?.Logging?.Requests?.Path,
                _ => throw new ArgumentException()
            };
            if (filePath != null && System.IO.File.Exists(filePath))
            {
                return System.IO.File.ReadAllText(filePath);
            }
            return "no file";
        }


        [HttpPost]
        public async Task Post([FromBody] Update update)
        {
            await TelegramProcessing.ProcessEvent(botClient, update);
        }
    }
}
