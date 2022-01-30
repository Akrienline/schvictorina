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
using System.Xml.Serialization;

namespace SchVictorina.WebAPI.Utilites
{
    public static class TelegramUtilites
    {
        [XmlRoot]
        public class GlobalSettings
        {
            [XmlElement]
            public TelegramBotSettings TelegramBot;
            public DiscordBotSettings DiscordBot;
        }
        public class DiscordBotSettings
        {
            [XmlElement]
            public string Token;
        }
        public class TelegramBotSettings
        {
            [XmlElement]
            public string Token;
            [XmlElement]
            public Webhook Webhook;
            [XmlIgnore]
            public IEnumerable<BotCommand> Commands;
        }
        public class Webhook
        {
            [XmlElement]
            public bool IsEnabled;
            [XmlElement]
            public string Url;
        }
        public static GlobalSettings GetGlobalSettings()
        {
            var XmlSerializer = new XmlSerializer(typeof(GlobalSettings));
            var fileStream = new FileStream(path: Path.GetFullPath("config/global_config.xml"), mode: FileMode.Open, access: FileAccess.Read);
            return (GlobalSettings)XmlSerializer.Deserialize(fileStream);
        }
        public static IEnumerable<IEnumerable<InlineKeyboardButton>> GenerateInlineKeyboardButtons(TaskInfo question, BaseEngine baseEngine, EngineButton button)
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
        //public static Task GenerateMenuAndSend(ITelegramBotClient botClient, Update update)
        //{
        //    return botClient.SendTextMessageAsync(update.GetChatId(), "Выбери тему задания:",
        //            replyMarkup: new InlineKeyboardMarkup(
        //                BaseEngine.AllEngineTypes.Select(x => InlineKeyboardButton.WithCallbackData(x.Key.UIName, x.Key.ApiName))
        //                )
        //            );
        //}
        //public static Task GenerateQuestionAndSend(ITelegramBotClient botClient, Update update, string engineApiName)
        //{
        //    engineApiName = engineApiName.Split('-')[0];
        //    var engineType = BaseEngine.AllEngineTypes.First(x => x.Key.ApiName == engineApiName).Value;
        //    var engine = (BaseEngine)Activator.CreateInstance(engineType);
        //    var question = engine.GenerateQuestion() ?? new TaskInfo();
        //    var keyboard = TelegramUtilites.FromAnswerOptionsToKeyboardMarkup(question, engine);
        //    return botClient.SendTextMessageAsync(update.GetChatId() ?? new ChatId(""), question.Question ?? "", replyMarkup: keyboard);
        //}
    }
    public static class TelegramHandlers
    {
        public static async Task ProcessEvent(ITelegramBotClient botClient, Update update)
        {
            if (update.Type == UpdateType.Message)
            {
                if (update.Message.Chat.Type == ChatType.Group || update.Message.Chat.Type == ChatType.Channel || update.Message.Chat.Type == ChatType.Supergroup)
                {
                    if (update.Message.Text != "/theme")
                        return;
                }
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
                            if (callbackValues.Length == 2 && callbackValues[1] == "skip")
                            {
                                //add statistics
                            }
                            else if (callbackValues.Length == 4 && callbackValues[1] == "answer")
                            {
                                var message = callbackValues[2] == callbackValues[3]
                                                ? "Правильно 👍"
                                                : "Неправильно 👎";
                                await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, message, cancellationToken: CancellationToken.None);
                            }

                            var question = engineButton.Engine.GenerateQuestion() ?? new TaskInfo();
                            var keyboard = new InlineKeyboardMarkup(TelegramUtilites.GenerateInlineKeyboardButtons(question, engineButton.Engine, engineButton));
                            await botClient.SendTextMessageAsync(update.GetChatId(), question.Question ?? "нет вопроса!", replyMarkup: keyboard);
                        }
                    }
                }
            }
        }
        public static async Task ProcessEvent222222222(ITelegramBotClient botClient, Update update)
        {
            //if (update.Type == UpdateType.CallbackQuery)
            //{
            //    if (BaseEngine.AllEngineTypes.Select(x => x.Key.ApiName).Contains(update.CallbackQuery?.Data)) //selected engine
            //    {
            //        await TelegramUtilites.GenerateQuestionAndSend(botClient, update, update.CallbackQuery?.Data ?? "");
            //    }
            //    else if (BaseEngine.AllEngineTypes.Any(x => update.CallbackQuery.Data.StartsWith(x.Key.ApiName + "-"))) //got answer
            //    {
            //        if (update.CallbackQuery.Data.EndsWith("mainmenu"))
            //        {
            //            await TelegramUtilites.GenerateMenuAndSend(botClient, update);
            //        }
            //        else if (update.CallbackQuery.Data.EndsWith("skip"))
            //        {
            //            await TelegramUtilites.GenerateQuestionAndSend(botClient, update, update.CallbackQuery.Data);
            //        }
            //        else
            //        {
            //            var result = TelegramUtilites.FromCallbackQueryToTrueOrFalse(update.CallbackQuery.Data);
            //            if (result)
            //            {
            //                await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Правильно👍", cancellationToken: CancellationToken.None);

            //            }
            //            else
            //                await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Неправильно, попробуй это:", cancellationToken: CancellationToken.None);

            //            await TelegramUtilites.GenerateQuestionAndSend(botClient, update, update.CallbackQuery.Data);
            //        }
            //    }
            //    else
            //        await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, $"Ошибка, мы не смогли распознать сообщение: \"{update.CallbackQuery.Data}\" 🤕", cancellationToken: CancellationToken.None);
            //}
            //else if (update.Type == UpdateType.Message)
            //{
            //    await TelegramUtilites.GenerateMenuAndSend(botClient, update);
            //}
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
