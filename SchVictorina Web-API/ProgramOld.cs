using SchVictorina.WebAPI.Utilites;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;

namespace SchVictorina.WebAPI
{
    public static class ProgramOld
    {

        //class MainUpdateHandler : IUpdateHandler
        //{
        //    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        //    {
        //        throw new NotImplementedException();
        //    }

        //    private static Task GenerateQuestionAndSend(ITelegramBotClient botClient, Update update, string engineApiName)
        //    {
        //        engineApiName = engineApiName.Split('-')[0];
        //        var engineType = BaseEngine.AllEngineTypes.First(x => x.Key.ApiName == engineApiName).Value;
        //        var engine = (BaseEngine)Activator.CreateInstance(engineType);
        //        var question = engine.GenerateQuestion() ?? new TaskInfo();
        //        var keyboard = TelegramUtilites.FromAnswerOptionsToKeyboardMarkup(question, engine);
        //        return botClient.SendTextMessageAsync(update.GetChatId() ?? new ChatId(""), question.Question ?? "", replyMarkup: keyboard);
        //    }
        //    private static Task GenerateMenuAndSend(ITelegramBotClient botClient, Update update)
        //    {
        //        return botClient.SendTextMessageAsync(update.GetChatId() ?? new ChatId(""), "Выбери тему задания:",
        //                replyMarkup: new InlineKeyboardMarkup(
        //                    BaseEngine.AllEngineTypes.Select(x => InlineKeyboardButton.WithCallbackData(x.Key.UIName, x.Key.ApiName))
        //                    )
        //                );
        //    }

        //    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        //    {

        //        if (update.Type == UpdateType.CallbackQuery)
        //        {
        //            if (BaseEngine.AllEngineTypes.Select(x => x.Key.ApiName).Contains(update.CallbackQuery?.Data)) //selected engine
        //            {
        //                await GenerateQuestionAndSend(botClient, update, update.CallbackQuery?.Data ?? "");
        //            }
        //            else if (BaseEngine.AllEngineTypes.Any(x => update.CallbackQuery.Data.StartsWith(x.Key.ApiName + "-"))) //got answer
        //            {
        //                if (update.CallbackQuery.Data.EndsWith("mainmenu"))
        //                {
        //                    await GenerateMenuAndSend(botClient, update);
        //                }
        //                else if (update.CallbackQuery.Data.EndsWith("skip"))
        //                {
        //                    await GenerateQuestionAndSend(botClient, update, update.CallbackQuery.Data);
        //                }
        //                else
        //                {
        //                    var result = TelegramUtilites.FromCallbackQueryToTrueOrFalse(update.CallbackQuery.Data);
        //                    if (result)
        //                    {
        //                        await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Правильно👍", cancellationToken: CancellationToken.None);

        //                    }
        //                    else
        //                        await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Неправильно, попробуй это:", cancellationToken: CancellationToken.None);

        //                    await GenerateQuestionAndSend(botClient, update, update.CallbackQuery.Data);
        //                }
        //            }
        //            else
        //                await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, $"Ошибка, мы не смогли распознать сообщение: \"{update.CallbackQuery.Data}\" 🤕", cancellationToken: CancellationToken.None);
        //        }
        //        else if (update.Type == UpdateType.Message)
        //        {
        //            await GenerateMenuAndSend(botClient, update);
        //        }
        //    }

        //}

        public static async Task Main()
        {

            TelegramBotClient botClient = new("5037954922:AAEOOG51TDnR6nK9Zb9EZAhn0RZWgbQ1eS8"); //5037954922:AAEOOG51TDnR6nK9Zb9EZAhn0RZWgbQ1eS8
            if (botClient.TestApiAsync().Result == false)
            {
                Console.WriteLine("Telegram bot's token is invalid!");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("Telegram bot loading is successfully");
                DefaultUpdateReceiver reciever = new(botClient);
                await reciever.ReceiveAsync(new TelegramHandlers.MainUpdateHandler());
                Console.ReadKey();
            }
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