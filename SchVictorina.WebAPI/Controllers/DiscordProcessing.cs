using Discord;
using Discord.WebSocket;
using SchVictorina.WebAPI.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SchVictorina.WebAPI.Controllers
{
    public class DiscordProcessing
    {
        public static async Task ProcessEvent(SocketMessage message)
        {
            var user = GetUserBySocket(message);
            if (!message.Author.IsBot)
            {
                if (message.Content == "/themes")
                {
                    await message.Channel.SendMessageAsync("Выбери тему заданий:", components: GenerateButtons(ButtonConfig.RootButton));
                }
                else
                {

                }
            }
            else
            {

            }
        }

        private static UserConfig.User GetUserBySocket(SocketMessage message)
        {
            var user = UserConfig.Instance.Users.FirstOrDefault(user => (ulong)user.Info.UserId == message.Author.Id);
            if (user == null)
                RegisterIfNeed(message);
            return user;
        }

        public static async Task ProcessSlashExecute(SocketSlashCommand slashCommand)
        {
            var command = slashCommand.Data.Name;
            if (command == "/themes" || ButtonConfig.GetButton(command) == ButtonConfig.RootButton)
            {
                await slashCommand.Channel.SendMessageAsync("Выбери тему заданий:", components: GenerateButtons(ButtonConfig.RootButton));
            }
            else if (ButtonConfig.GetButton(command) != null)
            {
                var button = ButtonConfig.GetButton(command);
                if (button is GroupButton groupButton)
                {
                    await slashCommand.Channel.SendMessageAsync("Выбери тему задания", components: GenerateButtons(groupButton));
                }
                else if (button is EngineButton engineButton)
                {
                    var question = engineButton.Class.GenerateQuestion();
                    await SendQuestion(slashCommand.Channel, engineButton);
                }
            }
            else
            {

            }
        }
        public static async Task ProcessCommands(DiscordSocketClient client)
        {
            foreach (var command in client.GetGlobalApplicationCommandsAsync().Result)
            {
                await command.DeleteAsync();
            }

            foreach (var button in ButtonConfig.AllButtons.Where(button => !string.IsNullOrWhiteSpace(button.Value.ID)))
            {
                var globalCommand = new SlashCommandBuilder();
                globalCommand.WithName(button.Value.ID);
                globalCommand.WithDescription(button.Value.LabelWithParents);
                try
                {
                    await client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
                }
                catch (Exception)
                {

                }
            }
        }
        public static async Task ProcessButtonExecute(SocketMessageComponent message)
        {
            var button = ButtonConfig.GetButton(message.Data.CustomId);
            if (button is GroupButton groupButton)
            {
                await message.RespondAsync("Выбери тему задания:", components: GenerateButtons(groupButton));
            }
            else if (button is EngineButton engineButton)
            {
                await SendQuestion(message.Channel, engineButton);
            }
            else if (button is FunctionButton functionButton)
            {
                var result = functionButton.Class.Invoke(null, null, message);
                if (result.ImagePath != null)
                    await message.Channel.SendFileAsync(new FileAttachment(result.ImagePath), result.Text);
                else
                    await message.Channel.SendMessageAsync(result.Text);
                await message.Channel.SendMessageAsync("Выбери тему задания:", components: GenerateButtons(ButtonConfig.RootButton));
            }
            else if (message.Data.CustomId.Split('|').Length != 5 && message.Data.CustomId.Split('|').Length != 4)
            {
                if (button == null || !button.IsValidWithAscender)
                {
                    await message.RespondAsync("Выбери тему задания:", components: GenerateButtons(ButtonConfig.RootButton));
                }
            }
            else
            {
                var parts = message.Data.CustomId.Split('|');
                if (parts.Length == 4)
                {
                    var button2 = (EngineButton)ButtonConfig.GetButton(parts[0]);
                    if (parts[2] == "id")
                    {
                        var id = parts[3];
                        var answerInfo = button2.Class.ParseAnswerId(id);
                        var isRight = answerInfo.SelectedAnswer == answerInfo.RightAnswer;
                        var msg = isRight
                            ? $"Правильно, ответ: {answerInfo.RightAnswer}"
                            : string.IsNullOrWhiteSpace(answerInfo.SelectedAnswer)
                                ? $"Неправильно, ответ: {answerInfo.RightAnswer}"
                                : $"Неправильно.";
                        await message.RespondAsync(msg);
                        await SendQuestion(message.Channel, button2);
                    }
                }
                else if (parts.Length == 5)
                {
                    var button2 = (EngineButton)ButtonConfig.GetButton(parts[0]);
                    if (parts[2] == "t")
                    {
                        var isRight = parts[3] == parts[4];
                        var msg = isRight
                            ? $"Правильно, ответ: {parts[3]}"
                            : string.IsNullOrWhiteSpace(parts[4])
                                ? $"Неправильно. Верный ответ: {parts[3]}, а не {parts[4]}"
                                : $"Неправильно. Верный ответ: {parts[3]}";
                        await message.RespondAsync(msg);
                        await SendQuestion(message.Channel, button2);
                    }
                }
                else if (parts.Length == 2)
                {
                    if (parts[1] == "skip")
                    {
                        var button2 = (EngineButton)ButtonConfig.GetButton(parts[0]);
                        await SendQuestion(message.Channel, button2);
                    }
                }
            }
        }
        public static MessageComponent GenerateButtons(GroupButton groupButton)
        {
            var builder = new ComponentBuilder();
            var currentRow = 0;
            foreach (var button in groupButton.Children.Where(button => button.IsValidWithAscender))
            {
                if (button is GroupButton)
                    builder.WithButton(button.Label, button.ID, ButtonStyle.Primary, null, null, false, currentRow);
                else if (button is EngineButton engineButton)
                    builder.WithButton(engineButton.Label, engineButton.ID, ButtonStyle.Primary, null, null, false, currentRow);
                else if (button is FunctionButton functionButton)
                    builder.WithButton(functionButton.Label, functionButton.ID, ButtonStyle.Primary, null, null, false, currentRow);
                else if (button is SplitButton)
                    currentRow++;
            }
            if (groupButton != ButtonConfig.RootButton)
                builder.WithButton("Наверх!", groupButton.Parent.ID ?? ButtonConfig.RootButton.ID, ButtonStyle.Secondary, row: currentRow + 2);
            return builder.Build();
        }
        public static async Task SendQuestion(ISocketMessageChannel channel, EngineButton engineButton)
        {
            var question = engineButton.Class.GenerateQuestion();
            if (question.QuestionImagePath != null)
            {
                await channel.SendFileAsync(new FileAttachment(question.QuestionImagePath), question.Question + $"{Environment.NewLine}Если вы ответите правильно, получите {engineButton.RightScore} баллов;{Environment.NewLine}Если же не правильно, потеряете {engineButton.WrongScore} баллов.", components: GenerateQuestionButtons(question, engineButton));
            }
            else
            {
                await channel.SendMessageAsync(question.Question + $"{Environment.NewLine}Если вы ответите правильно, получите {engineButton.RightScore} баллов;{Environment.NewLine}Если же не правильно, потеряете {engineButton.WrongScore} баллов.", components: GenerateQuestionButtons(question, engineButton));
            }
        }
        public static MessageComponent GenerateQuestionButtons(QuestionInfo question, EngineButton button)
        {
            var builder = new ComponentBuilder();
            var answerOptions = question.WrongAnswers.Concat(new[] { question.RightAnswer });
            foreach (var answerOption in answerOptions)
            {
                if (!string.IsNullOrEmpty(answerOption.ID))
                    builder.WithButton(answerOption.Text, $"{button.ID}|a|id|{answerOption.ID}", ButtonStyle.Primary);
                else
                    builder.WithButton(answerOption.Text, $"{button.ID}|a|t|{answerOption.Text}|{question.RightAnswer.Text}", ButtonStyle.Primary);
            }
            builder.WithButton("Пропустить", $"{button.ID}|skip", ButtonStyle.Secondary, row: 1);
            builder.WithButton("Наверх!", $"{button.Parent.ID}", ButtonStyle.Secondary, row: 1);
            return builder.Build();
        }
        public static void RegisterIfNeed(SocketMessage message)
        {
            var username = message.Author.Username + message.Author.Discriminator;
            var user = new UserConfig.User() { Info = new UserConfig.User.UserInfo() { FirstName = string.Empty, LastName = string.Empty, Source = UserSourceType.Discord, UserId = (long)message.Author.Id, UserName = username }, IsHidden = false, Status = UserConfig.UserStatus.AFK, Role = UserConfig.UserRole.Student, Statistics = new UserConfig.User.StatisticsInfo() { LastVisitDate = DateTime.Now, RightAnswers = 0, RightInSequence = 0, Score = 0, SkipQuestions = 0, TotalQuestions = 0, WrongAnswers = 0, WrongInSequence = 0 } };
            if (UserConfig.Instance.Users.Any(user => user.Info.UserName == username))
            {
                user.Statistics.LastVisitDate = DateTime.Now;
                user.Info.UserName = username;
            }
            else
                UserConfig.Instance.Users.Add(user);
        }
    }
}
