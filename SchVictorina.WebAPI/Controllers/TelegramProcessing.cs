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
using Telegram.Bot.Types.Payments;
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
                        if (update.Message.Text.StartsWith("/score"))
                        {
                            await Score(botClient, update);
                        }

                        if (update.Message.Text.StartsWith("/hide"))
                        {
                            await HideUser(botClient, update);
                        }
                        if (update.Message.Text.StartsWith("/show"))
                        {
                            await ShowUser(botClient, update);
                        }

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
                        else if (button is FunctionButton functionButton)
                        {
                            var result = functionButton.Class?.Invoke();
                            if (result != null)
                                await botClient.SendTextAndImage(update, result.Text, result.ImagePath);
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
                                    try
                                    {
                                        await botClient.EditMessageReplyMarkupAsync(update.GetChatId(), update.GetMessageId(), new InlineKeyboardMarkup(new InlineKeyboardButton[0]));
                                    }
                                    catch { }

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
                                    await botClient.SendTextAndImage(update, result.Text, result.ImagePath);
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
        public static UserConfig.User GetUserByUsername(string username)
        {
            var rightUsername = username.Replace('@', ' ');
            var user = UserConfig.Instance.Users
                                          .Where(user => user.Info.UserName == rightUsername)
                                          .FirstOrDefault();
            return user;
        }
        public static bool IsAdmin(string username)
        {
            if (string.IsNullOrEmpty(username))
                throw new ArgumentNullException("username");
            else if (GetUserByUsername(username) == null)
                throw new ArgumentNullException("username");
            if (GetUserByUsername(username).Role == UserConfig.UserRole.Administrator)
                return true;
            return false;
        }
        private static async Task HideUser(ITelegramBotClient botClient, Update update)
        {
            if (update.Message.Text.StartsWith("/hide"))
            {
                if (IsAdmin(update.Message.From.Username))
                {
                    var nicknameToHide = update.Message.Text.Substring("/hide".Length).Trim().TrimStart('@');
                    var userToHide = GetUserByUsername(nicknameToHide);
                    if (userToHide == null)
                        await botClient.SendText(update, $"{nicknameToHide} не найден.");
                    else
                    {
                        userToHide.IsHiden = true;
                        await botClient.SendText(update, $"Ученик @{nicknameToHide} был удалён из списка лидеров.");
                    }
                }
                else
                    await botClient.SendText(update, "У тебя нет разрешения!");
            }
            else
                throw new ArgumentException();
        }
        private static async Task ShowUser(ITelegramBotClient botClient, Update update)
        {
            if (update.Message.Text.StartsWith("/show"))
            {
                if (IsAdmin(update.Message.From.Username))
                {
                    var nicknameToShow = update.Message.Text.Substring("/show".Length).Trim().TrimStart('@');
                    var userToHide = GetUserByUsername(nicknameToShow);
                    if (userToHide == null)
                        await botClient.SendText(update, $"{nicknameToShow} не найден.");
                    else
                    {
                        userToHide.IsHiden = false;
                        await botClient.SendText(update, $"Ученик @{nicknameToShow} был добавлен в список лидеров.");
                    }
                }
                else
                    await botClient.SendText(update, "У тебя нет разрешения!");
            }
        }

        #region Score Control
        private static async Task Score(ITelegramBotClient botClient, Update update)
        {
            if (string.IsNullOrWhiteSpace(update.Message.Text))
                throw new ArgumentNullException("update");
            else
                await botClient.SendText(update, "Произошла внутреняя ошибка.");
            if (update.Message.Text.StartsWith("/score right"))
                await RightScore(botClient, update);
            else if (update.Message.Text.StartsWith("/score wrong"))
                await WrongScore(botClient, update);
            else if (update.Message.Text.StartsWith("/score skip"))
                await SkipScore(botClient, update);
            else
            {
                await botClient.SendText(update, "Не хватает аргументов.");
                return;
            } 
        }
        private static async Task RightScore(ITelegramBotClient botClient, Update update)
        {
            await RightScore(botClient, update, update.Message.Text);
        }
        private static async Task RightScore(ITelegramBotClient botClient, Update update, string args)
        {
            var argsarray = args.Split(' ');
            if (argsarray.Length < 4)
            {
                await botClient.SendText(update, "Не хватает аргументов.");
                return;
            }
            if (string.IsNullOrWhiteSpace(argsarray[2]))
                await botClient.SendText(update, $"{argsarray[2]} не найден.");
            else if (string.IsNullOrWhiteSpace(argsarray[3]))
                await botClient.SendText(update, "Количество баллов не может быть пустым");
            else
            {
                var username = argsarray[2];
                var score = Convert.ToInt32(argsarray[3]);
                await RightScore(botClient, update, username, score);
            }
        }
        private static async Task RightScore(ITelegramBotClient botClient, Update update, string username, int score)
        {
            if (IsAdmin(update.Message.From.Username))
            {
                var user = GetUserByUsername(username);
                if (user == null)
                    await botClient.SendText(update, $"@{username} не найден.");
                else
                {
                    if (score < 0)
                    {
                        user.Statistics.RightAnswers += score;
                        await botClient.SendText(update, $"{score.ToString().Substring(1)} баллов было удалено у ученика {username}.");
                    }
                    else
                    {
                        user.Statistics.RightAnswers += score;
                        await botClient.SendText(update, $"{score} баллов было добавлено ученику {username}");
                    }
                }

            }
            else
                await botClient.SendText(update, "У тебя нет разрешения!");
        }
        private static async Task WrongScore(ITelegramBotClient botClient, Update update)
        {
            await WrongScore(botClient, update, update.Message.Text);
        }
        private static async Task WrongScore(ITelegramBotClient botClient, Update update, string args)
        {
            var argsarray = args.Split(' ');
            if (argsarray.Length < 4)
            {
                await botClient.SendText(update, "Не хватает аргументов.");
                return;
            }
            if (string.IsNullOrWhiteSpace(argsarray[2]))
                await botClient.SendText(update, $"{argsarray[2]} не найден.");
            else if (string.IsNullOrWhiteSpace(argsarray[3]))
                await botClient.SendText(update, "Количество баллов не может быть пустым");
            else
            {
                var username = argsarray[2];
                var score = Convert.ToInt32(argsarray[3]);
                await WrongScore(botClient, update, username, score);
            }
        }
        private static async Task WrongScore(ITelegramBotClient botClient, Update update, string username, int score)
        {
            if (IsAdmin(update.Message.From.Username))
            {
                var user = GetUserByUsername(username);
                if (user == null)
                {
                    await botClient.SendText(update, $"@{username} не найден.");
                    return;
                }
                else
                {
                    if (score < 0)
                    {
                        user.Statistics.WrongAnswers += score;
                        await botClient.SendText(update, $"{score.ToString().Substring(1)} баллов было удалено у ученика {username}.");
                    }
                    else
                    {
                        user.Statistics.WrongAnswers += score;
                        await botClient.SendText(update, $"{score} баллов было добавлено ученику {username}");
                    }
                }

            }
            else
                await botClient.SendText(update, "У тебя нет разрешения!");
        }
        private static async Task SkipScore(ITelegramBotClient botClient, Update update)
        {
            await SkipScore(botClient, update, update.Message.Text);
        }
        private static async Task SkipScore(ITelegramBotClient botClient, Update update, string args)
        {
            var argsarray = args.Split(' ');
            if (argsarray.Length < 4)
            {
                await botClient.SendText(update, "Не хватает аргументов.");
                return;
            }
            if (string.IsNullOrWhiteSpace(argsarray[2]))
                await botClient.SendText(update, $"{argsarray[2]} не найден.");
            else if (string.IsNullOrWhiteSpace(argsarray[3]))
                await botClient.SendText(update, "Количество баллов не может быть пустым");
            else
            {
                var username = argsarray[2];
                var score = Convert.ToInt32(argsarray[3]);
                await SkipScore(botClient, update, username, score);
            }
        }
        private static async Task SkipScore(ITelegramBotClient botClient, Update update, string username, int score)
        {
            if (IsAdmin(update.Message.From.Username))
            {
                var user = GetUserByUsername(username);
                if (user == null)
                {
                    await botClient.SendText(update, $"@{username} не найден.");
                    return;
                }
                else
                {
                    if (score < 0)
                    {
                        user.Statistics.SkipQuestions += score;
                        await botClient.SendText(update, $"{score.ToString().Substring(1)} баллов было удалено у ученика {username}.");
                    }
                    else
                    {
                        user.Statistics.SkipQuestions += score;
                        await botClient.SendText(update, $"{score} баллов (пропущенных) было добавлено ученику {username}");
                    }
                }

            }
            else
                await botClient.SendText(update, "У тебя нет разрешения!");
        }
        #endregion
    }
}
