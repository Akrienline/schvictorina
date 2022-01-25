using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using SchVictorina.WebAPI.Utilites;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;

namespace SchVictorina.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class TelegramBotController : ControllerBase
    {
        static TelegramBotClient botClient;
        static DefaultUpdateReceiver updateReceiver;

        
        private readonly ILogger<TelegramBotController>? _logger;
        public TelegramBotController(ILogger<TelegramBotController> logger)
        {
            _logger = logger;
        }

        static TelegramBotController()
        {
            botClient = new("5226603528:AAFEtSHgTcOm3-5r3-EHW4ubcg0bfGdyIcI");
            updateReceiver = new(botClient);
            updateReceiver.ReceiveAsync(new TelegramHandlers.MainUpdateHandler());
        }

        [HttpPost]
        public async Task Post([FromBody] Update update)
        {
            if (update.Type == UpdateType.CallbackQuery)
            {
                if (BaseEngine.AllEngineTypes.Select(x => x.Key.ApiName).Contains(update.CallbackQuery?.Data)) //selected engine
                {
                    await TelegramUtilites.GenerateQuestionAndSend(botClient, update, update.CallbackQuery?.Data ?? "");
                }
                else if (BaseEngine.AllEngineTypes.Any(x => update.CallbackQuery.Data.StartsWith(x.Key.ApiName + "-"))) //got answer
                {
                    if (update.CallbackQuery.Data.EndsWith("mainmenu"))
                    {
                        await TelegramUtilites.GenerateMenuAndSend(botClient, update);
                    }
                    else if (update.CallbackQuery.Data.EndsWith("skip"))
                    {
                        await TelegramUtilites.GenerateQuestionAndSend(botClient, update, update.CallbackQuery.Data);
                    }
                    else
                    {
                        var result = TelegramUtilites.FromCallbackQueryToTrueOrFalse(update.CallbackQuery.Data);
                        if (result)
                        {
                            await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Правильно👍", cancellationToken: CancellationToken.None);

                        }
                        else
                            await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Неправильно, попробуй это:", cancellationToken: CancellationToken.None);

                        await TelegramUtilites.GenerateQuestionAndSend(botClient, update, update.CallbackQuery.Data);
                    }
                }
                else
                    await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, $"Ошибка, мы не смогли распознать сообщение: \"{update.CallbackQuery.Data}\" 🤕", cancellationToken: CancellationToken.None);
            }
            else if (update.Type == UpdateType.Message)
            {
                await TelegramUtilites.GenerateMenuAndSend(botClient, update);
            }
        }
    }
}
