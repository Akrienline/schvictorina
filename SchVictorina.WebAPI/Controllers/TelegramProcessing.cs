using SchVictorina.WebAPI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SchVictorina.WebAPI.Controllers
{
    public static class TelegramProcessing
    {
        internal static async Task ProcessEvent(ITelegramBotClient botClient, Update update)
        {
            try
            {
                
                await GlobalConfig.Instance?.Logging?.Requests?.Log(botClient, update, $"{update.Type}: {update.Message?.Text ?? update.CallbackQuery?.Data}");

                var user = UserConfig.Instance.GetUser(GetUserInfo(update.GetUser()));
                if (user.Statistics.LastVisitDate == new DateTime())
                    await botClient.SendTextAndImage(update, "Приветствую тебя в этом канале! Дерзай!", "gift_signup.jpg");
                else if ((DateTime.Now - user.Statistics.LastVisitDate).TotalDays > 7)
                    await botClient.SendTextAndImage(update, "С возвращением!", "gift_back.jpg");
                UserConfig.Instance.Log(user, UserConfig.EventType.Request);

                if (update.Type == UpdateType.Message)
                {
                    if (update.Message.Chat.Type == ChatType.Group || update.Message.Chat.Type == ChatType.Channel || update.Message.Chat.Type == ChatType.Supergroup)
                    {
                        if (update.Message.Text != "/theme")
                            return;
                    }
                    if (update.Message.Text.StartsWith("/"))
                    {
                        var button = ButtonConfig.GetButton(update.Message.Text.TrimStart('/'));
                        if (button is GroupButton groupButton)
                        {
                            await GenerateButtonsAndSend(botClient, update, groupButton);
                            return;
                        }
                        else if (button is EngineButton engineButton)
                        {
                            await SendQuestion(botClient, update, user, engineButton);
                            return;
                        }
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
                                    UserConfig.Instance.Log(user, UserConfig.EventType.SkipQuestion);
                                }
                                else if (callbackValues.Length == 4 && callbackValues[1] == "answer")
                                {
                                    var isRight = callbackValues[2] == callbackValues[3];

                                    UserConfig.Instance.Log(user, isRight ? UserConfig.EventType.RightAnswer : UserConfig.EventType.WrongAnswer);
                                    await botClient.SendText(update, isRight ? $"Правильно 👍. Ответ: {callbackValues[2]}" : $"Неправильно 👎. Верный ответ: {callbackValues[2]}, а не {callbackValues[3]}");
                                    await botClient.EditMessageReplyMarkupAsync(update.GetChatId(), update.GetMessageId(), new InlineKeyboardMarkup(new InlineKeyboardButton[0]));

                                    if (isRight)
                                    {
                                        if (user.Statistics.RightInSequence % 20 == 0)
                                            await botClient.SendTextAndImage(update, "Уже 20 правильных ответов подряд, держи парочку подарков.", "gift_sequence_20.jpg");
                                        else if (user.Statistics.RightInSequence % 5 == 0)
                                            await botClient.SendTextAndImage(update, "Пять правильных ответов подряд, держи подарок.", "gift_sequence_5.jpg");
                                        if (user.Statistics.RightAnswers % 100 == 0)
                                            await botClient.SendTextAndImage(update, "Сто правильных ответов, молодец.", "gift_rights_100.jpg");
                                    }
                                }

                                await SendQuestion(botClient, update, user, engineButton);
                            }
                            else if (button is FunctionButton functionButton)
                            {
                                var result = functionButton.Class?.Invoke();
                                if (result != null)
                                {
                                    await botClient.SendTextAndImage(update, result.Text, result.ImagePath);
                                }
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

        private static async Task SendQuestion(ITelegramBotClient botClient, Update update, UserConfig.User user, EngineButton engineButton)
        {
            UserConfig.Instance.Log(user, UserConfig.EventType.SendQuestion);
            var question = engineButton.Class.GenerateQuestion();
            var keyboard = new InlineKeyboardMarkup(GenerateInlineKeyboardButtons(question, engineButton.Class, engineButton));
            await botClient.SendText(update, question?.Question ?? "нет вопроса!", keyboard);
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
            var uiButtons = new List<InlineKeyboardButton[]>();
            if (groupButton.Children != null)
            {
                var uiLineButtons = new List<InlineKeyboardButton>();
                foreach (var child in groupButton.Children.Where(child => child.IsValidWithAscender))
                {
                    if (child is SplitButton)
                    {
                        if (uiLineButtons.Any())
                        {
                            uiButtons.Add(uiLineButtons.ToArray());
                            uiLineButtons.Clear();
                        }
                    }
                    else
                    {
                        uiLineButtons.Add(InlineKeyboardButton.WithCallbackData(child.Label, child.ID));
                    }
                }
                if (uiLineButtons.Any())
                    uiButtons.Add(uiLineButtons.ToArray());
            }

            if (groupButton.Parent != null)
            {
                uiButtons.Add(new[] { InlineKeyboardButton.WithCallbackData("Наверх!", groupButton.Parent.ID) });
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
