using SchVictorina.WebAPI.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
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
                        if (update.Message.Text == "/bitmaptest")
                        {
                            var i1 = Image.FromFile("Images/gift_back.jpg");
                            var i2 = Image.FromFile("Images/gift_sequence_20.jpg");
                            var i3 = Image.FromFile("Images/gift_sequence_20.jpg");
                            var b = new Bitmap(1920, 1080);

                            using Graphics g = Graphics.FromImage(b);
                            g.DrawImage(i1, 0, 0, 1920, 1080);
                            g.DrawImage(i2, 750, 0, 1920, 1080);
                            g.DrawImage(i3, 1500, 0, 1920, 1080);

                            b.Save("bitmaptest.png");

                            await botClient.SendTextAndImage(update, "aa", "bitmaptest.png");
                        }
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
                                var result = functionButton.Class?.Invoke(update);
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
                                else if (callbackValues.Length > 2 && callbackValues[1] == "a") //answer
                                {
                                    bool isRight;
                                    if (callbackValues.Length == 5 && callbackValues[2] == "t")
                                    {
                                        isRight = (callbackValues[3] == callbackValues[4]) || (callbackValues[3] != "c" && callbackValues[4] == "c");
                                        await botClient.SendText(update, isRight
                                            ? $"Правильно 👍. Ответ: {callbackValues[3]}. Сейчас у вас {user.Statistics.Score.To1CString()} баллов"
                                            : $"Неправильно 👎. Верный ответ: {callbackValues[3]}{(callbackValues[4] == "w" ? "" : ", а не " + callbackValues[4])}. Сейчас у вас {user.Statistics.Score.To1CString()} баллов, поскольку вы потеряли {engineButton.WrongScore.To1CString()} баллов.");
                                    }
                                    else if (callbackValues.Length >= 4 && callbackValues[2] == "id")
                                    {
                                        var answerInfo = engineButton.Class.ParseAnswerId(string.Join("|", callbackValues.Skip(3)));
                                        if (answerInfo == null)
                                            throw new ArgumentOutOfRangeException();

                                        isRight = answerInfo.RightAnswer == answerInfo.SelectedAnswer;
                                        await botClient.SendText(update, isRight
                                            ? $"Правильно 👍. Ответ: {answerInfo.RightAnswer}"
                                            : answerInfo.SelectedAnswer != null
                                                ? $"Неправильно 👎. Верный ответ: {answerInfo.RightAnswer}, а не {answerInfo.SelectedAnswer}"
                                                : $"Неправильно 👎. Верный ответ: {answerInfo.RightAnswer}");

                                        if (!isRight)
                                        {
                                            if (!string.IsNullOrEmpty(answerInfo.Description) || !string.IsNullOrEmpty(answerInfo.DescriptionImagePath))
                                                await botClient.SendTextAndImage(update, answerInfo.Description, answerInfo.DescriptionImagePath);
                                        }
                                    }
                                    else
                                    {
                                        throw new ArgumentOutOfRangeException();
                                    }

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
                                            await botClient.SendTextAndImage(update, "Три неправильных ответа подряд, соберись!", "Images/gift_too_many_wrongs.jpg");
                                        if (rightInSequence >= 3)
                                            await botClient.SendTextAndImage(update, "А ведь ты так хорошо шёл...", "Images/gift_rightbreak.jpg");
                                        user.Statistics.RightInSequence = 0;
                                    }
                                }

                                await SendQuestion(botClient, update, user, engineButton);
                            }
                            else if (button is FunctionButton functionButton)
                            {
                                var result = functionButton.Class?.Invoke(update);
                                if (result != null)
                                    await botClient.SendTextAndImage(update, result.Text, result.ImagePath);
                                await GenerateButtonsAndSend(botClient, update, ButtonConfig.RootButton);
                                return;
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

            user.Status = UserConfig.UserStatus.Solving;

            if (question.WrongAnswers != null)
            {
                await botClient.SendHtmlAndImage(update, question?.Question + $"{Environment.NewLine}Если вы ответите правильно, получите {engineButton.RightScore} {Environment.NewLine}Если же не правильно, вы потеряете {engineButton.WrongScore}" ?? "К сожалению не удалось найти вопрос!", question.QuestionImagePath, keyboard);
            }
            else
            {
                await botClient.SendHtml(update, question?.Question + "\nВариантов ответа нет." ?? "К сожалению не удалось найти вопрос!");
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
        private static IEnumerable<IEnumerable<InlineKeyboardButton>> GenerateInlineKeyboardButtons(QuestionInfo question, EngineButton button)
        {
            if (question.WrongAnswers == null || !question.WrongAnswers.Any() || question.RightAnswer == null)
            {
                yield return (IEnumerable<InlineKeyboardButton>)InlineKeyboardButton.WithCallbackData("Произошла ошибка, не удалось найти варианты ответа.");
            }
            
            if (question.WrongAnswers != null && question.WrongAnswers.Any())
            {
                yield return question.WrongAnswers
                                     .Concat(new[] { question.RightAnswer })
                                     .OrderByRandom()
                                     .Select(option =>
                                     {
                                         if (!string.IsNullOrEmpty(option.ID))
                                         {
                                             return InlineKeyboardButton.WithCallbackData(option.Text, $"{button.ID}|a|id|{option.ID}");
                                         }
                                         else
                                         {
                                             var data = $"{button.ID}|a|t|{question.RightAnswer.Text}|{option.Text}";
                                             if (Encoding.UTF8.GetByteCount(data) > 64) // telegram limit
                                             {
                                                 data = $"{button.ID}|a|t|{question.RightAnswer.Text}|{(option == question.RightAnswer ? "c" : "w")}";
                                                 if (Encoding.UTF8.GetByteCount(data) > 64)
                                                     data = $"{button.ID}|a|t|{question.RightAnswer.Text.Substring(0, (64 - $"{button.ID}|a|t|".Length - "|w".Length) / 2)}|{(option == question.RightAnswer ? "c" : "w")}";
                                             }
                                             return InlineKeyboardButton.WithCallbackData(option.Text, data);
                                         }
                                     });
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

                var user = UserConfig.Instance.GetUser(GetUserInfo(update.GetUser()));

                user.Status = UserConfig.UserStatus.Selecting;

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
        internal static UserConfig.User.UserInfo GetUserInfo(User user)
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
            catch(ApiRequestException ex) { ex.ToString(); }
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
            string[] parts;
            if (update.Type == UpdateType.Message)
            {
                parts = update.Message.Text.Split(" ");
                if (parts.Length == 1)
                {
                    await UserControl(botClient, update, update.Message.From.Username);
                }    
                else if (IsAdmin(update))
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
                parts = update.CallbackQuery.Data.Split(" ");
                if (parts.Length < 1)
                {
                    await UserControl(botClient, update, update.CallbackQuery.From.Username);
                }
                else if (IsAdmin(update))
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
                    if (IsAdmin(update))
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
                    else
                    {
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
                          );
                    }
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
