using System;
using SchVictorina.WebAPI;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types.Enums;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace SchVictorina.WebAPI.Utilites
{
    public static class TelegramUtilites
    {
        public static InlineKeyboardMarkup FromAnswerOptionsToKeyboardMarkup(TaskInfo question, BaseEngine baseEngine)
        {
            var apiName = ((EngineAttribute)baseEngine.GetType().GetCustomAttributes(typeof(EngineAttribute), true)[0]).ApiName;

            return new []
            {
                question.AnswerOptions != null && question.AnswerOptions.Any()
                    ? question.AnswerOptions.Select(option =>
                    {
                        return InlineKeyboardButton.WithCallbackData(option?.ToString() ?? "", $"{apiName}-{question.RightAnswer}.{option}");
                    }).ToArray()
                    : Array.Empty<InlineKeyboardButton>(),
                new []
                {
                    InlineKeyboardButton.WithCallbackData("Выйти", $"{apiName}-mainmenu"),
                    InlineKeyboardButton.WithCallbackData("Пропустить", $"{apiName}-skip")
                }
            }.Where(x => x != null && x.Length > 0).ToArray();
        }
        public static ChatId GetChatId(this Update update)
        {
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
                return update.CallbackQuery?.Message?.Chat.Id;
            else if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
                return update.Message?.Chat.Id;
            else
                return new ChatId("");
        }
        public static bool FromCallbackQueryToTrueOrFalse(string query)
        {
            var parsedQuery = query[(query.IndexOf('-') + 1)..].Split('.');
            var rightAnswer = parsedQuery[0];
            var userAnswer = parsedQuery[1];
            if (rightAnswer == userAnswer)
                return true;
            else
                return false;
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
        public static Task GenerateMenuAndSend(ITelegramBotClient botClient, Update update)
        {
            return botClient.SendTextMessageAsync(update.GetChatId(), "Выбери тему задания:",
                    replyMarkup: new InlineKeyboardMarkup(
                        BaseEngine.AllEngineTypes.Select(x => InlineKeyboardButton.WithCallbackData(x.Key.UIName, x.Key.ApiName))
                        )
                    );
        }
        public static Task GenerateQuestionAndSend(ITelegramBotClient botClient, Update update, string engineApiName)
        {
            engineApiName = engineApiName.Split('-')[0];
            var engineType = BaseEngine.AllEngineTypes.First(x => x.Key.ApiName == engineApiName).Value;
            var engine = (BaseEngine)Activator.CreateInstance(engineType);
            var question = engine.GenerateQuestion() ?? new TaskInfo();
            var keyboard = TelegramUtilites.FromAnswerOptionsToKeyboardMarkup(question, engine);
            return botClient.SendTextMessageAsync(update.GetChatId() ?? new ChatId(""), question.Question ?? "", replyMarkup: keyboard);
        }
    }
    public static class TelegramHandlers
    {
        public static async Task ProcessEvent(ITelegramBotClient botClient, Update update)
        {
            if (update.Type == UpdateType.Message)
            {
                await TelegramUtilites.GenerateButtonsAndSend(botClient, update, Config.RootButton);
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                var callbackValues = update.CallbackQuery?.Data.Split('|');
                if (callbackValues.Any())
                {
                    var buttonId = callbackValues.FirstOrDefault();
                    var button = Config.GetButton(buttonId);
                    if (button == null)
                    {
                        await TelegramUtilites.GenerateButtonsAndSend(botClient, update, Config.RootButton);
                    }
                    else
                    {
                        if (button is GroupButton groupButton)
                        {
                            await TelegramUtilites.GenerateButtonsAndSend(botClient, update, groupButton);
                        }
                        else if (button is EngineButton engineButton)
                        {
                            if (callbackValues.Length == 1)
                            {
                            }
                        }
                    }
                }
            }
        }
        public static async Task ProcessEvent222222222(ITelegramBotClient botClient, Update update)
        {
            if (update.Type == UpdateType.CallbackQuery)
            {
                if (BaseEngine.AllEngineTypes.Select(x => x.Key.ApiName).Contains(update.CallbackQuery?.Data)) //selected engine
                {
                    await TelegramUtilites.GenerateQuestionAndSend(botClient, update, update.CallbackQuery?.Data ?? "");
                }
                else if (BaseEngine.AllEngineTypes.Any(x => update.CallbackQuery.Data.StartsWith(x.Key.ApiName + "-"))) //got answer
                {
                    if (update.CallbackQuery.Data.EndsWith("mainmenu"))
                    {
                        await TelegramUtilites.GenerateMenuAndSend(botClient, update);
                    }
                    else if (update.CallbackQuery.Data.EndsWith("skip"))
                    {
                        await TelegramUtilites.GenerateQuestionAndSend(botClient, update, update.CallbackQuery.Data);
                    }
                    else
                    {
                        var result = TelegramUtilites.FromCallbackQueryToTrueOrFalse(update.CallbackQuery.Data);
                        if (result)
                        {
                            await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Правильно👍", cancellationToken: CancellationToken.None);

                        }
                        else
                            await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Неправильно, попробуй это:", cancellationToken: CancellationToken.None);

                        await TelegramUtilites.GenerateQuestionAndSend(botClient, update, update.CallbackQuery.Data);
                    }
                }
                else
                    await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, $"Ошибка, мы не смогли распознать сообщение: \"{update.CallbackQuery.Data}\" 🤕", cancellationToken: CancellationToken.None);
            }
            else if (update.Type == UpdateType.Message)
            {
                await TelegramUtilites.GenerateMenuAndSend(botClient, update);
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
