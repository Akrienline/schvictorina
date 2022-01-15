using System;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using System.Linq;
using Telegram.Bot.Args;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using System.Collections.Generic;
using System.IO;

namespace Victorina
{

    class Program
    {
        static Dictionary<long, int> CorrectAnswerCount = new Dictionary<long, int>();

        class MainUpdateHandler : IUpdateHandler
        {
            public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
            {
                if (update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
                {
                    #region questionSolving
                    if (update.CallbackQuery.Data.StartsWith("mathengine-"))
                    {
                        var request = update.CallbackQuery.Data.Substring(11).Split('.');

                        var rightAnwser = request[0];
                        var userAnwser = request[1];
                        if (rightAnwser == userAnwser)
                        {
                            if (CorrectAnswerCount.ContainsKey(update.CallbackQuery.From.Id))
                                CorrectAnswerCount[update.CallbackQuery.From.Id] += 1;
                            else
                                CorrectAnswerCount[update.CallbackQuery.From.Id] = 1;

                            await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Правильно 👍");

                            if (CorrectAnswerCount[update.CallbackQuery.From.Id] % 5 == 0)
                            {
                                await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Молодец, получена очередная пятёрка из правильных ответов!");
                                await botClient.SendPhotoAsync(update.CallbackQuery.Message.Chat.Id, new Telegram.Bot.Types.InputFiles.InputOnlineFile(new MemoryStream(System.IO.File.ReadAllBytes("gift.jpg"))));
                            }

                            BaseEngine engine = new MathEngine();
                            var question = engine.GenerateQuestion();
                            await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, question.Question, replyMarkup: new InlineKeyboardMarkup(
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData(question.AnswerOptions[0].ToString(), "mathengine-" + question.RightAnswer + "." + question.AnswerOptions[0].ToString() + "." + question.Question),
                                InlineKeyboardButton.WithCallbackData(question.AnswerOptions[1].ToString(), "mathengine-" + question.RightAnswer + "." + question.AnswerOptions[1].ToString() + "." + question.Question),
                                InlineKeyboardButton.WithCallbackData(question.AnswerOptions[2].ToString(), "mathengine-" + question.RightAnswer + "." + question.AnswerOptions[2].ToString() + "." + question.Question)
                            }));
                            //engine.GenerateQuestion().UIQuestion
                        }
                        else
                        {
                            CorrectAnswerCount[update.CallbackQuery.From.Id] = 0;

                            await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Неправильно, попрубой это:");
                            BaseEngine engine = new MathEngine();
                            var question = engine.GenerateQuestion();
                            await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, question.Question, replyMarkup: new InlineKeyboardMarkup(
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData(question.AnswerOptions[0].ToString(), "mathengine-" +question.RightAnswer + "." + question.AnswerOptions[0].ToString() + "." + question.Question),
                                InlineKeyboardButton.WithCallbackData(question.AnswerOptions[1].ToString(), "mathengine-" +question.RightAnswer + "." + question.AnswerOptions[1].ToString() + "." + question.Question),
                                InlineKeyboardButton.WithCallbackData(question.AnswerOptions[2].ToString(), "mathengine-" +question.RightAnswer + "." + question.AnswerOptions[2].ToString() + "." + question.Question)
                            }));
                        }
                        //await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, $"Мы получили данный запрос: {request}");
                    }
                    else if (update.CallbackQuery.Data.StartsWith("temperatureengine-"))
                    {

                    }
                    #endregion
                    #region newQuestion
                    else if (update.CallbackQuery.Data == "math")
                    {
                        BaseEngine engine = new MathEngine();
                        var question = engine.GenerateQuestion();
                        if (question.AnswerOptions != null)
                            await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, question.Question, replyMarkup: new InlineKeyboardMarkup(
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData(question.AnswerOptions[0].ToString(), "mathengine-" + question.RightAnswer + "." + question.AnswerOptions[0].ToString()),
                                InlineKeyboardButton.WithCallbackData(question.AnswerOptions[1].ToString(), "mathengine-" + question.RightAnswer + "." + question.AnswerOptions[1].ToString()),
                                InlineKeyboardButton.WithCallbackData(question.AnswerOptions[2].ToString(), "mathengine-" + question.RightAnswer + "." + question.AnswerOptions[2].ToString())
                            }));
                        else
                        {
                            await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, question.Question);
                        }

                    }
                    else if (update.CallbackQuery.Data == "temperature")
                    {
                        await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Данная тема задания в разработке😥");
                    }
                    else
                        await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, $"Ошибка, мы не смогли распознать сообщение: \"{update.CallbackQuery.Data}\" 🤕");
                }
                else if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
                {
                    await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Выбери тему задания:",
                        replyMarkup: new InlineKeyboardMarkup(
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Математика", "math"),
                                InlineKeyboardButton.WithCallbackData("Температура", "temperature")
                            }));
                }
                #endregion 
            }

        }

        static async Task Main(string[] args)
        {
            TelegramBotClient client = new TelegramBotClient("5037954922:AAEOOG51TDnR6nK9Zb9EZAhn0RZWgbQ1eS8");
            //var a = client.TestApiAsync().Result;
            //client.OnMessage += async (object sender, MessageEventArgs messageEventArgs) =>
            //{
            //    var message = messageEventArgs.Message;
            //    if (message == null)
            //        return;
            //    await client.SendTextMessageAsync(message.Chat.Id, message.Text + " обхрюкано");
            //};
            var reciever = new DefaultUpdateReceiver(client);
            await reciever.ReceiveAsync(new MainUpdateHandler());
            Console.ReadKey();
            //return;
            //Func<ITelegramBotClient, Telegram.Bot.Types.Update, CancellationToken>
            //var updateHandler = new DefaultUpdateHandler(Func<, Telegram.Bot.Types.Update, CancellationToken>);
            //updateHandler
            //var reciver = new DefaultUpdateReceiver(client);
            //await reciver.ReceiveAsync(updateHandler)
            //BaseEngine engine = new TemperatureEngine
            //{
            //};

            //while (true)
            //{
            //    var task = engine.GenerateQuestion();
            //    Console.WriteLine(task.Question);
            //    if (task.AnswerOptions != null && task.AnswerOptions.Any())
            //        Console.WriteLine("Варианты ответа: " + string.Join(", ", task.AnswerOptions));
            //    var userAnswerString = Console.ReadLine();
            //    if (string.IsNullOrEmpty(userAnswerString))
            //        break;
            //    var userAnswer = Convert.ChangeType(userAnswerString, task.RightAnswer.GetType());

            //    if (object.Equals(task.RightAnswer, userAnswer) || string.Equals(task.RightAnswer?.ToString()?.Trim(), userAnswer?.ToString()?.Trim(), StringComparison.InvariantCultureIgnoreCase))
            //        WriteLine("right!", ConsoleColor.Green);
            //    else
            //        WriteLine("wrong!", ConsoleColor.Red);
            //    Console.WriteLine();
            //}
        }


        private static void WriteLine(string str, ConsoleColor? color)
        {
            if (!color.HasValue)
            {
                Console.WriteLine(str);
            }
            else
            {
                var defaultColor = Console.ForegroundColor;
                Console.ForegroundColor = color.Value;
                Console.WriteLine(str);
                Console.ForegroundColor = defaultColor;
            }
        }
    }

    public class TaskInfo
    {
        public string Question { get; set; }
        public string IVQuestion { get; set; }
        public object RightAnswer { get; set; }
        public object[] AnswerOptions { get; set; }
    }


    public abstract class BaseEngine
    {
        public abstract TaskInfo GenerateQuestion();
    }

    public class MathEngine : BaseEngine
    {
        private static Random random = new Random();
        public int MaxAnswerValue = 100;

        public override TaskInfo GenerateQuestion()
        {
            var @operator = GenerateEnum<Operator>();

            var maxAnswerValue2 = @operator switch
            {
                Operator.Add => MaxAnswerValue / 2,
                Operator.Subtract => MaxAnswerValue,
                Operator.Multiply => Convert.ToInt32(Math.Sqrt(MaxAnswerValue)),
                Operator.Divide => (random.Next(10, MaxAnswerValue) / 10),
                _ => 0
            };
            var maxAnswerValue1 = @operator == Operator.Divide
                ? maxAnswerValue2 * random.Next(1, 11)
                : maxAnswerValue2;

            var value1 = @operator == Operator.Divide ? maxAnswerValue1 : random.Next(1, maxAnswerValue1);
            var value2 = @operator == Operator.Divide ? maxAnswerValue2 : random.Next(1, maxAnswerValue2);
            var answer = @operator switch
            {
                Operator.Add => value1 + value2,
                Operator.Subtract => value1 - value2,
                Operator.Multiply => value1 * value2,
                Operator.Divide => value1 / value2,
                _ => 0
            };
            return new TaskInfo
            {
                Question = @$"Сколько будет {value1}{(@operator switch
                {
                    Operator.Add => "+",
                    Operator.Subtract => "-",
                    Operator.Multiply => "*",
                    Operator.Divide => "/",
                    _ => 0
                }
                    )}{value2}",
                IVQuestion = $"mathengine-{value1}{(@operator switch { Operator.Add => "+", Operator.Subtract => "-", Operator.Multiply => "*", Operator.Divide => "/", _ => 0 })}{value2}",
                AnswerOptions = new object[]
                {
                    answer,
                    answer + InvertIfNeeded(random.Next(1, MaxAnswerValue / 10), GenerateBool()),
                    answer + InvertIfNeeded(random.Next(1, MaxAnswerValue / 10), GenerateBool()),
                }.OrderBy(x => x).ToArray(),
                RightAnswer = answer
            };
        }

        private static bool GenerateBool()
        {
            return random.Next(1, 3) == 1;
        }
        private static int InvertIfNeeded(int value, bool needsInvert)
        {
            return !needsInvert ? value : (-1 * value);
        }

        private static T GenerateEnum<T>()
        {
            var enumValues = Enum.GetValues(typeof(T)).Cast<int>().ToArray();
            var index = random.Next(0, enumValues.Length);
            return (T)(object)enumValues[index];
        }

        public enum Operator
        {
            Add,
            Subtract,
            Multiply,
            Divide
        }
    }
}