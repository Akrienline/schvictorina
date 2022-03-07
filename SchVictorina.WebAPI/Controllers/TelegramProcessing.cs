using SchVictorina.WebAPI.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
                    await botClient.SendTextAndImage(update, "Приветствую тебя в этом канале! Дерзай!\nНе забудь подписаться на новости - @schvictorina_news!", "Images/gift_signup.jpg");
                else if ((DateTime.Now - user.Statistics.LastVisitDate).TotalDays > 7)
                    await botClient.SendTextAndImage(update, "С возвращением!", "Images/gift_back.jpg");

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
                        if (update.Message.Text == "/start")
                        {
                            await GenerateButtonsAndSend(botClient, update, ButtonConfig.RootButton);
                        }
                        else if (update.Message.Text.StartsWith("/score"))
                        {
                            await Score(botClient, update);
                        }
                        else if (update.Message.Text.StartsWith("/makewaip"))
                        {
                            if (IsAdmin(update))
                                await MakeWaip(botClient, update);
                            else
                                await botClient.SendText(update, "У вас нет разрешения!");
                        }
                        else if (update.Message.Text.StartsWith("/user"))
                        {
                            await UserController(botClient, update);
                        }
                        else
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
                            else if (button is FunctionButton functionButton)
                            {
                                var result = functionButton.Class?.Invoke();
                                if (result != null)
                                    await botClient.SendTextAndImage(update, result.Text, result.ImagePath);
                                await GenerateButtonsAndSend(botClient, update, ButtonConfig.RootButton);
                                return;
                            }
                        }
                    }
                    else
                        await GenerateButtonsAndSend(botClient, update, ButtonConfig.RootButton);
                }
                else if (update.Type == UpdateType.CallbackQuery)
                {

                    if (update.CallbackQuery.Data.StartsWith("usercontrol-"))
                        await UserController(botClient, update);
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
                                else if (callbackValues.Length == 4 && callbackValues[1] == "a") //answer
                                {
                                    var isRight = (callbackValues[2] == callbackValues[3]) || (callbackValues[2] != "c" && callbackValues[3] == "c");


                                    //UserConfig.Instance.Log(user, isRight ? UserConfig.EventType.RightAnswer : UserConfig.EventType.WrongAnswer);
                                    await botClient.SendText(update, isRight ? $"Правильно 👍. Ответ: {callbackValues[2]}" : $"Неправильно 👎. Верный ответ: {callbackValues[2]}{(callbackValues[3] == "w" ? "" : ", а не " + callbackValues[3])}");

                                    try
                                    {
                                        await botClient.EditMessageReplyMarkupAsync(update.GetChatId(), update.GetMessageId(), new InlineKeyboardMarkup(new InlineKeyboardButton[0]));
                                    }
                                    catch { }
                                    
                                    if (isRight)
                                    {
                                        UserConfig.Instance.Log(user, UserConfig.EventType.RightAnswer, engineButton.RightScore);
                                        if (user.Statistics.RightInSequence % 20 == 0)
                                            await botClient.SendTextAndImage(update, "Уже 20 правильных ответов подряд, держи парочку подарков.", "Images/gift_sequence_20.jpg");
                                        else if (user.Statistics.RightInSequence % 5 == 0)
                                            await botClient.SendTextAndImage(update, "Пять правильных ответов подряд, держи подарок.", "Images/gift_sequence_5_*.jpg");
                                        if (user.Statistics.RightAnswers % 100 == 0)
                                            await botClient.SendTextAndImage(update, "Сто правильных ответов, молодец.", "Images/gift_rights_100.jpg");
                                        user.Statistics.WrongInSequence = 0;
                                    }
                                    else
                                    {
                                        var rightInSequence = user.Statistics.RightInSequence;
                                        UserConfig.Instance.Log(user, UserConfig.EventType.WrongAnswer, -engineButton.WrongScore);
                                        if (user.Statistics.WrongInSequence == 3)
                                            await botClient.SendTextAndImage(update, "Не расстраивайся, держи конфетку", "Images/gift_break.jpg");
                                        if (rightInSequence >= 3)
                                            await botClient.SendTextAndImage(update, "Не расстраивайся, держи конфетку", "Images/gift_break.jpg");
                                        user.Statistics.RightInSequence = 0;
                                    }
                                }

                                await SendQuestion(botClient, update, user, engineButton);
                            }
                            else if (button is FunctionButton functionButton)
                            {
                                var result = functionButton.Class?.Invoke();
                                if (result != null)
                                {
                                    if (result.ParseMode == null)
                                        await botClient.SendTextAndImage(update, result.Text, result.ImagePath);
                                    else if (result.ParseMode == ParseMode.Html)
                                        await botClient.SendTextAndImageAsHTML(update, result.Text, result.ImagePath);
                                    else if (result.ParseMode == ParseMode.Markdown || result.ParseMode == ParseMode.MarkdownV2)
                                        await botClient.SendTextAndImageAsMarkdown(update, result.Text, result.ImagePath);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await GlobalConfig.Instance?.Logging?.Errors?.Log(botClient, update, ex.ToString());
                await GenerateButtonsAndSend(botClient, update, ButtonConfig.RootButton); 
            }
        }
        private static async Task SendQuestion(ITelegramBotClient botClient, Update update, UserConfig.User user, EngineButton engineButton)
        {
            UserConfig.Instance.Log(user, UserConfig.EventType.SendQuestion);
            var question = engineButton.Class.GenerateQuestion();
            var keyboard = new InlineKeyboardMarkup(GenerateInlineKeyboardButtons(question, engineButton).SplitLongLines());
            
            if (question.WrongAnswers != null)
                await botClient.SendHTMLCode(update, question?.Question ?? "К сожелению не удалось найти вопрос!", keyboard);
            else
                await botClient.SendHTMLCode(update, question?.Question + "\nВариантов ответа нет." ?? "К сожелению не удалось найти вопрос!");
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
        private static IEnumerable<IEnumerable<InlineKeyboardButton>> GenerateInlineKeyboardButtons(QuestionInfo question, EngineButton button)
        {
            if (question.WrongAnswers != null && question.WrongAnswers.Any())
            {
                yield return question.WrongAnswers
                                     .Concat(new[] { question.RightAnswer })
                                     .OrderByRandom()
                                     .Select(option =>
                                     {
                                         var data = $"{button.ID}|a|{question.RightAnswer}|{option}";
                                         if (Encoding.UTF8.GetByteCount(data) > 64) // telegram limit
                                         {
                                             data = $"{button.ID}|a|{question.RightAnswer}|{(option == question.RightAnswer ? "c" : "w")}";
                                             if (Encoding.UTF8.GetByteCount(data) > 64)
                                                 data = $"{button.ID}|a|{question.RightAnswer.ToString().Substring(0, (64 - $"{button.ID}|a|".Length - "|w".Length) / 2)}|{(option == question.RightAnswer ? "c" : "w")}";
                                         }
                                         return InlineKeyboardButton.WithCallbackData(option?.ToString() ?? "", data);
                                     });
            }
            else
            {

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

            return botClient.SendText(update, "Выбери тему задания:", new InlineKeyboardMarkup(uiButtons.SplitLongLines()));
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
        public static async Task Log(this GlobalConfig.LoggingSettings.BaseLog log, ITelegramBotClient botClient, Update update, string message)
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
                    await botClient.SendText(update, "Произошла внутренняя ошибка!");
            }
            catch { }
        }
        public static UserConfig.User GetUserByUsername(string username)
        {
            var rightUsername = username.Replace("@", "");
            var user = UserConfig.Instance.Users.Where(user => user.Info.UserName == rightUsername)
                                                .FirstOrDefault();
            return user;
        }
        public static bool IsAdmin(string username)
        {
            if (string.IsNullOrEmpty(username))
                throw new ArgumentNullException("username");
            if (username == "schvictorina_debug_bot")
                return false;
            else if (GetUserByUsername(username) == null)
                throw new Exception($"User with username {username} not found");
            if (GetUserByUsername(username).Role == UserConfig.UserRole.Administrator)
                return true;
            return false;
        }
        public static bool IsAdmin(Update update)
        {
            if (update.Type == UpdateType.Message)
                return IsAdmin(update.GetUser());
            else if (update.Type == UpdateType.CallbackQuery)
                return IsAdmin(update.GetUser());
            else
                return false;
        }
        public static bool IsAdmin(this User user)
        {
            return IsAdmin(user.Username);
        }
        private static async Task MakeWaip(ITelegramBotClient botClient, Update update)
        {
            UserConfig.Instance.Users = new List<UserConfig.User>();
            await botClient.SendText(update, "Вайп успешно выполнен!");
        }

        #region User Control
        private static async Task UserController(ITelegramBotClient botClient, Update update)
        {
            if (update.Type == UpdateType.Message)
            {
                if (IsAdmin(update))
                {
                    await UserControl(botClient, update, update.Message.Text.Substring("/user".Replace("@", "").Trim().Length));
                }
                else
                {
                    await botClient.SendText(update, "У вас нет разрешения!");
                }
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                if (IsAdmin(update))
                {
                    await UserControl(botClient, update, "");
                }
                else
                {
                    await botClient.SendText(update, "У вас нет разрешения!");
                }
            }
        }
        private static async Task UserControl(ITelegramBotClient botClient, Update update, string username)
        {
            if (update.Type == UpdateType.CallbackQuery)
            {
                if (update.CallbackQuery.Data.StartsWith("usercontrol-student-"))
                {
                    var user = GetUserByUsername(update.CallbackQuery.Data.Substring("usercontrol-student-".Trim().Length));
                    user.Role = UserConfig.UserRole.Student;
                    await botClient.SendText(update, $"{user.Info.UserName} стал учеником");
                }
                if (update.CallbackQuery.Data.StartsWith("usercontrol-teacher-"))
                {
                    var user = GetUserByUsername(update.CallbackQuery.Data.Substring("usercontrol-teacher-".Trim().Length));
                    user.Role = UserConfig.UserRole.Teacher;
                    await botClient.SendText(update, $"{user.Info.UserName} стал учитилем");
                }
                if (update.CallbackQuery.Data.StartsWith("usercontrol-admin-"))
                {
                    var user = GetUserByUsername(update.CallbackQuery.Data.Substring("usercontrol-admin-".Trim().Length));
                    user.Role = UserConfig.UserRole.Administrator;
                    await botClient.SendText(update, $"{user.Info.UserName} стал администратором");
                }
                if (update.CallbackQuery.Data.StartsWith("usercontrol-hide-"))
                {
                    var user = GetUserByUsername(update.CallbackQuery.Data.Substring("usercontrol-hide-".Trim().Length));
                    user.IsHidden = true;
                    await botClient.SendText(update, $"{user.Info.UserName} был удалён из списка лидеров");
                }
                if (update.CallbackQuery.Data.StartsWith("usercontrol-show-"))
                {
                    var user = GetUserByUsername(update.CallbackQuery.Data.Substring("usercontrol-show-".Trim().Length));
                    user.IsHidden = false;
                    await botClient.SendText(update, $"{user.Info.UserName} был добавлен в список лидеров");
                }
            }
            else if (update.Type == UpdateType.Message)
            {
                username = username.Trim();
                var userInfo = GetUserByUsername(username.Trim());
                if (userInfo == null)
                {
                    await botClient.SendText(update, $"@{username.Trim()} не найден!");
                }
                else
                {
                    var preKeyboard = new List<List<InlineKeyboardButton>>();
                    var buttons = new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData("Сделать учеником", $"usercontrol-student-{username}"),
                        InlineKeyboardButton.WithCallbackData("Сделать учителем", $"usercontrol-teacher-{username}"),
                        InlineKeyboardButton.WithCallbackData("Сделать администратором", $"usercontrol-admin-{username}")
                    };
                    preKeyboard.Add(buttons.ToList());
                    buttons.Clear();
                    buttons.Add(InlineKeyboardButton.WithCallbackData("Добавить в список лидеров", $"usercontrol-show-{username}"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData("Удалить из списка лидеров", $"usercontrol-hide-{username}"));
                    preKeyboard.Add(buttons.ToList());
                    var keyboard = new InlineKeyboardMarkup(preKeyboard);
                    await botClient.SendText(update,
                        @$"Ученик {userInfo.Info.LastName} {userInfo.Info.FirstName} (@{userInfo.Info.UserName}):
Дата последнего посещения: {userInfo.Statistics.LastVisitDate:dd'.'mm'.'yyyy' 'HH':'mm':'ss}
Cкрыт ли в списке лидеров: {(userInfo.IsHidden ? "да" : "нет")}
Баллов: {userInfo.Statistics.Score}
Правильных ответов: {userInfo.Statistics.RightAnswers}
Правильных ответов подряд: {userInfo.Statistics.RightInSequence}
Неправильных ответов: {userInfo.Statistics.WrongAnswers}
Пропущеных вопросов: {userInfo.Statistics.SkipQuestions}
Всего вопросов: {userInfo.Statistics.RightAnswers + userInfo.Statistics.WrongAnswers + userInfo.Statistics.SkipQuestions}"
, keyboard);
                }
            }
        }
        #endregion
        #region Score Control
        private static async Task Score(ITelegramBotClient botClient, Update update)
        {
            if (IsAdmin(update))
            {
                var parts = update.Message.Text.Split(' ');
                if (parts.Length < 3)
                {
                    await botClient.SendText(update, "Не хватает аргументов");
                }
                else if (parts.Length > 3)
                {
                    await botClient.SendText(update, "Слишком много аргументов");
                }
                else
                {
                    var user = GetUserByUsername(parts[1]);
                    if (user == null)
                    {
                        await botClient.SendText(update, $"@{parts[1].Replace("@", "")} не найден");
                        return;
                    }
                    if (parts[2] == null)
                    {
                        await botClient.SendText(update, "Значение Score не может быть null");
                        return;
                    }
                    if (parts[2].ToDouble() < 0)
                    {
                        if (user.Statistics.Score - parts[2].ToDouble() < 0)
                        {
                            await botClient.SendText(update, "Операция не может быть выполена - баллы пользователя будут меньше нуля.");
                            return;
                        }
                    }
                    user.Statistics.Score += (double)parts[2].ToDouble();
                    await botClient.SendText(update, "Операция успешно выполнена");
                }
            }
            else
            {
                await botClient.SendText(update, "У вас нет разрешения");
            }
        }
        #endregion
    }
}
