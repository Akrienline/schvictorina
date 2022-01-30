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
using Telegram.Bot.Types.InputFiles;
using System.IO;

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

        public static UserConfig.User.UserInfo GetUserInfo(User user)
        {
            return new UserConfig.User.UserInfo
            {
                Source = UserSourceType.Telegram,
                UserId = user.Id,
                UserName = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName
            };
        }

        public static class TelegramHandlers
        {
            public static async Task ProcessEvent(ITelegramBotClient botClient, Update update)
            {
                var userInfo = GetUserInfo(update.GetUser());

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
                                    UserConfig.Instance.Log(userInfo, UserConfig.EventType.SkipQuestion);
                                }
                                else if (callbackValues.Length == 4 && callbackValues[1] == "answer")
                                {
                                    var isRight = callbackValues[2] == callbackValues[3];

                                    if (isRight)
                                        UserConfig.Instance.Log(userInfo, UserConfig.EventType.RightAnswer);
                                    else
                                        UserConfig.Instance.Log(userInfo, UserConfig.EventType.WrongAnswer);

                                    if (isRight)
                                    {
                                        var user = UserConfig.Instance.GetUser(userInfo);
                                        if (user.Statistics.RightInSequence % 20 == 0)
                                        {
                                            await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Уже 20 правильных ответов подряд, держи парочку подарков:", cancellationToken: CancellationToken.None);
                                            await botClient.SendPhotoAsync(TelegramUtilites.GetChatId(update), new InputOnlineFile(new MemoryStream(System.IO.File.ReadAllBytes("gift_sequence_20.jpg"))));
                                        }
                                        else if (user.Statistics.RightInSequence % 5 == 0)
                                        {
                                            await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Пять правильных ответов подряд, держи подарок:", cancellationToken: CancellationToken.None);
                                            await botClient.SendPhotoAsync(TelegramUtilites.GetChatId(update), new InputOnlineFile(new MemoryStream(System.IO.File.ReadAllBytes("gift_sequence_5.jpg"))));
                                        }

                                        if (user.Statistics.RightAnswers % 100 == 0)
                                        {
                                            await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Сто правльных ответов, молодец:", cancellationToken: CancellationToken.None);
                                            await botClient.SendPhotoAsync(TelegramUtilites.GetChatId(update), new InputOnlineFile(new MemoryStream(System.IO.File.ReadAllBytes("gift_rights_100.jpg"))));
                                        }
                                    }
                                    
                                    var message = isRight
                                                    ? "Правильно 👍"
                                                    : $"Неправильно 👎{Environment.NewLine}Верный ответ: {callbackValues[2]}";
                                    await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, message, cancellationToken: CancellationToken.None);
                                }

                                UserConfig.Instance.Log(userInfo, UserConfig.EventType.SendQuestion);
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
