using SchVictorina.WebAPI.Controllers;
using SchVictorina.WebAPI.Utilities;
using Telegram.Bot.Types;

namespace SchVictorina.WebAPI.Engines
{
    public class SupportFucntion : IFunction
    {
        public FunctionButton.Result Invoke(Update update)
        {
            var user = UserConfig.Instance.GetUser(TelegramProcessing.GetUserInfo(update.GetUser()));

            user.Status = UserConfig.UserStatus.Supporting;

            return new FunctionButton.Result()
            {
                Text = "Хорошо, теперь отправь текст, описывающий пробелму или твою идею."
            };
        }
    }
}
