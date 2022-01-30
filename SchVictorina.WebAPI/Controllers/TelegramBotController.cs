using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using SchVictorina.WebAPI.Utilites;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Telegram.Bot.Types;
using System.Threading;
using System;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using System.Linq;

namespace SchVictorina.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class TelegramBotController : ControllerBase
    {
        static TelegramBotClient botClient;
        static DefaultUpdateReceiver updateReceiver;
        internal string GetReleaseToken()
        {
            
        }

        public TelegramBotController(ILogger<TelegramBotController> logger)
        {
            
        }

        static TelegramBotController()
        {
#if DEBUG
            botClient = new TelegramBotClient("5126418465:AAEAiZ1T6wckDwi2JS3ARx4rynAu4VvH-IE");
            updateReceiver = new DefaultUpdateReceiver(botClient);
            updateReceiver.ReceiveAsync(new TelegramHandlers.MainUpdateHandler());
#else
            botClient = new TelegramBotClient("5226603528:AAFEtSHgTcOm3-5r3-EHW4ubcg0bfGdyIcI");
#endif
        }

        [HttpGet]
        public async Task Start() { }

        [HttpPost]
        public async Task Post([FromBody] Update update)
        {
            await TelegramHandlers.ProcessEvent(botClient, update);
        }
    }
}
