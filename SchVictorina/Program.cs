using System;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using System.Linq;
using SchVictorina.Engines;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using System.Collections.Generic;
using System.IO;
using SchVictorina.Utilites;
using Telegram.Bot.Types.Enums;

namespace SchVictorina
{
    public static class Program
    {

        class MainUpdateHandler : IUpdateHandler
        {
            public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            private Task GenerateQuestionAndSend(ITelegramBotClient botClient, Update update, string engineApiName)
            {
                engineApiName = engineApiName.Split('-')[0];
                var engineType = BaseEngine.AllEngineTypes.First(x => x.Key.ApiName == engineApiName).Value;
                var engine = (BaseEngine)Activator.CreateInstance(engineType);
                var question = engine.GenerateQuestion();
                var keyboard = ConvertUtilites.FromAnswerOptionsToKeyboardMarkup(question, engine);
                return botClient.SendTextMessageAsync(update.GetChatId(), question.Question, replyMarkup: keyboard);
            }
            private Task GenerateMenuAndSend(ITelegramBotClient botClient, Update update)
            {
                return botClient.SendTextMessageAsync(update.GetChatId(), "Выбери тему задания:",
                        replyMarkup: new InlineKeyboardMarkup(
                            BaseEngine.AllEngineTypes.Select(x => InlineKeyboardButton.WithCallbackData(x.Key.UIName, x.Key.ApiName))
                            )
                        );
            }

            public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
            {
                
                if (update.Type == UpdateType.CallbackQuery)
                {
                    if (BaseEngine.AllEngineTypes.Select(x => x.Key.ApiName).Contains(update.CallbackQuery.Data)) //selected engine
                    {
                        await GenerateQuestionAndSend(botClient, update, update.CallbackQuery.Data);
                    }
                    else if (BaseEngine.AllEngineTypes.Any(x => update.CallbackQuery.Data.StartsWith(x.Key.ApiName + "-"))) //got answer
                    {
                        if (update.CallbackQuery.Data.EndsWith("mainmenu"))
                        {
                            await GenerateMenuAndSend(botClient, update);
                        }
                        else if (update.CallbackQuery.Data.EndsWith("skip"))
                        {
                            await GenerateQuestionAndSend(botClient, update, update.CallbackQuery.Data);
                        }
                        else
                        {
                            var result = ConvertUtilites.FromCallbackQueryToTrueOrFalse(update.CallbackQuery.Data);
                            if (result)
                            {
                                await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Правильно👍");

                            }
                            else
                                await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Неправильно, попробуй это:");

                            await GenerateQuestionAndSend(botClient, update, update.CallbackQuery.Data);
                        }
                    }
                    else
                        await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, $"Ошибка, мы не смогли распознать сообщение: \"{update.CallbackQuery.Data}\" 🤕");
                }
                else if (update.Type == UpdateType.Message)
                {
                    await GenerateMenuAndSend(botClient, update);
                }
            }

        }

        public static async Task Main()
        {
            //var translator = new GoogleTranslateFreeApi.GoogleTranslator();
            //var r = await translator.TranslateAsync("Привет", GoogleTranslateFreeApi.Language.Russian, GoogleTranslateFreeApi.Language.English);

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


        //private static void WriteLine(string str, ConsoleColor? color)
        //{
        //    if (!color.HasValue)
        //    {
        //        Console.WriteLine(str);
        //    }
        //    else
        //    {
        //        var defaultColor = Console.ForegroundColor;
        //        Console.ForegroundColor = color.Value;
        //        Console.WriteLine(str);
        //        Console.ForegroundColor = defaultColor;
        //    }
        //}
    }
}