using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SchVictorina.WebAPI.Utilities;
using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;

namespace SchVictorina.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class TelegramBotController : ControllerBase
    {
        static TelegramBotClient botClient;
        static DefaultUpdateReceiver updateReceiver;

        public TelegramBotController(ILogger<TelegramBotController> logger)
        {
        }

        static TelegramBotController()
        {
            var settings = GlobalConfig.Instance;
            botClient = new TelegramBotClient(settings.TelegramBot.Token);
            if ((settings.TelegramBot?.Webhook?.Enabled ?? false) == false)
            {
                updateReceiver = new DefaultUpdateReceiver(botClient);
                updateReceiver.ReceiveAsync(new TelegramProcessing.MainUpdateHandler());
            }
        }

        [HttpGet]
        public async Task Start() { }

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
