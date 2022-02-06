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

    public static class TelegramProcessing
    {
        internal static async Task ProcessEvent(ITelegramBotClient botClient, Update update)
        {
            try
            {
                await GlobalConfig.Instance?.Logging?.Requests?.Log(botClient, update, $"{update.Type}: {update.Message?.Text ?? update.CallbackQuery?.Data}");

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
                        if (button == null || !button.IsValidWithAscender)
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

                                    UserConfig.Instance.Log(userInfo, isRight ? UserConfig.EventType.RightAnswer : UserConfig.EventType.WrongAnswer);

                                    if (isRight)
                                    {
                                        var user = UserConfig.Instance.GetUser(userInfo);
                                        if (user.Statistics.RightInSequence % 20 == 0)
                                            await botClient.SendTextAndImage(update, "Уже 20 правильных ответов подряд, держи парочку подарков:", "gift_sequence_20.jpg");
                                        else if (user.Statistics.RightInSequence % 5 == 0)
                                            await botClient.SendTextAndImage(update, "Пять правильных ответов подряд, держи подарок:", "gift_sequence_5.jpg");
                                        if (user.Statistics.RightAnswers % 100 == 0)
                                            await botClient.SendTextAndImage(update, "Сто правльных ответов, молодец:", "gift_rights_100.jpg");
                                    }

                                    await botClient.SendText(update, isRight ? "Правильно 👍" : $"Неправильно 👎{Environment.NewLine}Верный ответ: {callbackValues[2]}");
                                }

                                UserConfig.Instance.Log(userInfo, UserConfig.EventType.SendQuestion);
                                var question = engineButton.Engine.GenerateQuestion();
                                var keyboard = new InlineKeyboardMarkup(GenerateInlineKeyboardButtons(question, engineButton.Engine, engineButton));
                                await botClient.SendText(update, question?.Question ?? "нет вопроса!", keyboard);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await GlobalConfig.Instance?.Logging?.Errors?.Log(botClient, update, ex.ToString());
            }
        }

        internal class MainUpdateHandler : IUpdateHandler
        {
            public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
            public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
            {
                await ProcessEvent(botClient, update);
            }
        }

        private static IEnumerable<IEnumerable<InlineKeyboardButton>> GenerateInlineKeyboardButtons(QuestionInfo question, BaseEngine baseEngine, EngineButton button)
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

        private static Task GenerateButtonsAndSend(ITelegramBotClient botClient, Update update, GroupButton groupButton)
        {
            var uiButtons = new List<InlineKeyboardButton>();
            if (groupButton.Children != null)
            {
                foreach (var child in groupButton.Children.Where(child => child.IsValidWithAscender))
                    uiButtons.Add(InlineKeyboardButton.WithCallbackData(child.Label, child.ID));
            }
            if (groupButton.Parent != null)
            {
                uiButtons.Add(InlineKeyboardButton.WithCallbackData("Наверх!", groupButton.Parent.ID));
            }

            return botClient.SendText(update, "Выбери тему задания:", new InlineKeyboardMarkup(uiButtons));
        }

        private static UserConfig.User.UserInfo GetUserInfo(User user)
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

        private static async Task Log(this GlobalConfig.LoggingSettings.BaseLog log, ITelegramBotClient botClient, Update update, string message)
        {
            if (log == null || !log.Enabled)
                return;

            try
            {
                LogUtilities.Log(log.Path, log.MaxSizeInKB, $"{update.GetUser()?.Username}: {message}");
            }
            catch { }

            try
            {
                if (log.SendToUser)
                    await botClient.SendText(update, message);
            }
            catch { }
        }
    }
}
