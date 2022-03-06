using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace SchVictorina.WebAPI.Utilities
{
    public static class TelegramUtilites
    {
        public static ChatId GetChatId(this Update update)
        {
            if (update.Type == UpdateType.CallbackQuery)
                return update.CallbackQuery?.Message?.Chat.Id;
            else if (update.Type == UpdateType.Message)
                return update.Message?.Chat.Id;
            else
                return new ChatId("");
        }

        public static User GetUser(this Update update)
        {
            if (update.Type == UpdateType.CallbackQuery)
                return update.CallbackQuery?.From;
            else if (update.Type == UpdateType.Message)
                return update.Message?.From;
            else
                return new User();
        }

        public static int GetMessageId(this Update update)
        {
            if (update.Type == UpdateType.Message)
                return update.Message.MessageId;
            else if (update.Type == UpdateType.CallbackQuery)
                return update.CallbackQuery.Message.MessageId;
            return 0;
        }

        public static IEnumerable<IEnumerable<InlineKeyboardButton>> SplitLongLines(this IEnumerable<IEnumerable<InlineKeyboardButton>> buttons)
        {
            return buttons.SelectMany(x => x.SplitByEqualLimit(50, y => y.Text.Length + 4));
        }

        public static async Task SendImageAsSticker(this ITelegramBotClient botClient, Update update, string filePath)
        {
            await botClient.SendStickerAsync(update.GetChatId(), new InputOnlineFile(new MemoryStream(System.IO.File.ReadAllBytes(filePath))), cancellationToken: CancellationToken.None);
        }
        public static async Task SendText(this ITelegramBotClient botClient, Update update, string message, InlineKeyboardMarkup inlineKeyboardMarkup = null)
        {
            await botClient.SendTextMessageAsync(update.GetChatId(), message, replyMarkup: inlineKeyboardMarkup, cancellationToken: CancellationToken.None);
        }
        public static async Task SendMarkdownText(this ITelegramBotClient botClient, Update update, string text, InlineKeyboardMarkup inlineKeyboardMarkup = null)
        {
            await botClient.SendTextMessageAsync(update.GetChatId(), text, ParseMode.Markdown, replyMarkup: inlineKeyboardMarkup);
        }
        public static async Task SendHTMLCode(this ITelegramBotClient botClient, Update update, string text, InlineKeyboardMarkup inlineKeyboardMarkup = null)
        {
            await botClient.SendTextMessageAsync(update.GetChatId(), text, ParseMode.Html, replyMarkup: inlineKeyboardMarkup);
        }
        public static async Task SendImage(this ITelegramBotClient botClient, Update update, string filePath)
        {
            await botClient.SendPhotoAsync(update.GetChatId(), new InputOnlineFile(new MemoryStream(System.IO.File.ReadAllBytes(filePath))), cancellationToken: CancellationToken.None);
        }
        public static async Task SendTextAndImage(this ITelegramBotClient botClient, Update update, string message, string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                await botClient.SendText(update, message);
            }
            else
            {
                if (filePath.Contains("*"))
                    filePath = Directory.GetFiles(Path.GetDirectoryName(filePath), Path.GetFileName(filePath)).OrderByRandom().First();
                await botClient.SendPhotoAsync(update.GetChatId(), new InputOnlineFile(new MemoryStream(System.IO.File.ReadAllBytes(filePath))), message, cancellationToken: CancellationToken.None);
            }
            //await SendText(botClient, update, message);
            //await SendImage(botClient, update, filePath);
        }
        public static async Task SendTextAndImageAsHTML(this ITelegramBotClient botClient, Update update, string message, string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                await botClient.SendHTMLCode(update, message);
            }
            else
            {
                if (filePath.Contains("*"))
                    filePath = Directory.GetFiles(Path.GetDirectoryName(filePath), Path.GetFileName(filePath)).OrderByRandom().First();
                await botClient.SendPhotoAsync(update.GetChatId(), new InputOnlineFile(new MemoryStream(System.IO.File.ReadAllBytes(filePath))), message, ParseMode.Html, cancellationToken: CancellationToken.None);
            }
        }
        public static async Task SendTextAndImageAsMarkdown(this ITelegramBotClient botClient, Update update, string message, string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                await botClient.SendMarkdownText(update, message);
            }
            else
            {
                if (filePath.Contains("*"))
                    filePath = Directory.GetFiles(Path.GetDirectoryName(filePath), Path.GetFileName(filePath)).OrderByRandom().First();
                await botClient.SendPhotoAsync(update.GetChatId(), new InputOnlineFile(new MemoryStream(System.IO.File.ReadAllBytes(filePath))), message, ParseMode.Markdown, cancellationToken: CancellationToken.None);
            }
        }
    }
}
