using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using SchVictorina.WebAPI.Utilities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Telegram.Bot.Types;
using System.Threading;
using System;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using System.Linq;
using Telegram.Bot.Types.ReplyMarkups;

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
            return GlobalConfig.Instance.TelegramBot.Token;
        }

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
                updateReceiver.ReceiveAsync(new TelegramHandlers.MainUpdateHandler());
            }
        }

        [HttpGet]
        public async Task Start() { }

        [HttpPost]
        public async Task Post([FromBody] Update update)
        {
            await TelegramHandlers.ProcessEvent(botClient, update);
        }


        public static IEnumerable<IEnumerable<InlineKeyboardButton>> GenerateInlineKeyboardButtons(TaskInfo question, BaseEngine baseEngine, EngineButton button)
        {
            if (question.AnswerOptions != null && question.AnswerOptions.Any())
            {
                yield return question.AnswerOptions
                                     .Select(option => InlineKeyboardButton.WithCallbackData(option?.ToString() ?? "", $"{button.ID}|answer|{question.RightAnswer}|{option}"));
            }

            yield return new[]
            {
                InlineKeyboardButton.WithCallbackData("Пропустить", $"{button.ID}|skip"),
                InlineKeyboardButton.WithCallbackData("Наверх!", $"{button.Parent.ID}")
            };
        }

        public static Task GenerateButtonsAndSend(ITelegramBotClient botClient, Update update, GroupButton groupButton)
        {
            var uiButtons = new List<InlineKeyboardButton>();
            if (groupButton.Children != null)
            {
                foreach (var child in groupButton.Children)
                    uiButtons.Add(InlineKeyboardButton.WithCallbackData(child.Label, child.ID));
            }
            if (groupButton.Parent != null)
            {
                uiButtons.Add(InlineKeyboardButton.WithCallbackData("Наверх!", groupButton.Parent.ID));
            }

            return botClient.SendTextMessageAsync(update.GetChatId(), "Выбери тему задания:",
                    replyMarkup: new InlineKeyboardMarkup(uiButtons)
                    );
        }

        public static class TelegramHandlers
        {
            public static async Task ProcessEvent(ITelegramBotClient botClient, Update update)
            {
                if (update.Type == UpdateType.Message)
                {
                    if (update.Message.Chat.Type == ChatType.Group || update.Message.Chat.Type == ChatType.Channel || update.Message.Chat.Type == ChatType.Supergroup)
                    {
                        if (update.Message.Text != "/theme")
                            return;
                    }
                    await GenerateButtonsAndSend(botClient, update, ButtonConfig.RootButton);
                }
                else if (update.Type == UpdateType.CallbackQuery)
                {
                    var callbackValues = update.CallbackQuery?.Data.Split('|');
                    if (callbackValues.Any())
                    {
                        var buttonId = callbackValues.FirstOrDefault();
                        var button = ButtonConfig.GetButton(buttonId);
                        if (button == null)
                        {
                            await GenerateButtonsAndSend(botClient, update, ButtonConfig.RootButton);
                        }
                        else
                        {
                            if (button is GroupButton groupButton)
                            {
                                await GenerateButtonsAndSend(botClient, update, groupButton);
                            }
                            else if (button is EngineButton engineButton)
                            {
                                if (callbackValues.Length == 2 && callbackValues[1] == "skip")
                                {
                                    //add statistics
                                }
                                else if (callbackValues.Length == 4 && callbackValues[1] == "answer")
                                {
                                    var message = callbackValues[2] == callbackValues[3]
                                                    ? "Правильно 👍"
                                                    : $"Неправильно 👎{Environment.NewLine}Верный ответ: {callbackValues[2]}";
                                    await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, message, cancellationToken: CancellationToken.None);
                                }

                                var question = engineButton.Engine.GenerateQuestion() ?? new TaskInfo();
                                var keyboard = new InlineKeyboardMarkup(GenerateInlineKeyboardButtons(question, engineButton.Engine, engineButton));
                                await botClient.SendTextMessageAsync(update.GetChatId(), question.Question ?? "нет вопроса!", replyMarkup: keyboard);
                            }
                        }
                    }
                }
            }

            public class MainUpdateHandler : IUpdateHandler
            {
                public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
                {
                    throw new NotImplementedException();
                }
                public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
                {
                    await TelegramHandlers.ProcessEvent(botClient, update);
                }
            }
        }

    }
}
