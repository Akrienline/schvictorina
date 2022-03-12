using SchVictorina.WebAPI.Utilities;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Telegram.Bot.Types.Enums;

namespace SchVictorina.WebAPI.Engines
{
    public sealed class LeadersFunction : IFunction
    {
        public int MaxUsers { get; set; }

        public FunctionButton.Result Invoke()
        {
            var culture = new CultureInfo("ru-RU");
            var format = string.Format("0:0.0", "");
            var leaderboard = UserConfig.Instance.Users.OrderByDescending(users => users.Statistics.Score)
                                                       .Where(user => !string.IsNullOrWhiteSpace(user.Info.FirstName))
                                                       .Where(user => !string.IsNullOrWhiteSpace(user.Info.UserName))
                                                       .Where(user => !user.IsHidden)
                                                       .Where(user => user.Statistics.Score > 0)
                                                       .Take(MaxUsers)
                                                       .Select((user, i) => new
                                                       {
                                                           Position = i + 1,
                                                           Name = $"{user.Info.FirstName} (@{user.Info.UserName})",
                                                           Score = user.Statistics.Score.ToString("N1", culture),
                                                       })
                                                       .ToArray();
            if (leaderboard.IsNullOrEmpty())
            {
                return new FunctionButton.Result()
                {
                    Text = "К сожелению пока здесь нет никого..."
                };
            }

            var result = new StringBuilder();
            result.AppendLine("Десятка лучших:");
            foreach (var leader in leaderboard)
            {
                var score = "";
                if (leader.Score.EndsWith(",0"))
                    score = leader.Score.Substring(0, leader.Score.Length - 2);
                else
                    score = leader.Score;
                result.AppendLine($"{leader.Position}. {leader.Name}: {score} баллов");
            }

            return new FunctionButton.Result
            {
                Text = result.ToString(),
                ParseMode = ParseMode.Markdown
            };
        }
    }
}

//using System;
//using System.Linq;

//namespace SchVictorina.WebAPI.Engines
//{
//    public static class UserUtilities
//    {
//        public static UserConfig.User[] GetLeaderboard(int? members = 10) => UserConfig.Instance.Users.OrderByDescending(users => users.Statistics.RightAnswers).Take((int)members).ToArray();
//        public static string ToUIString(this UserConfig.User user, bool? needsNewLines = false)
//        {
//            var userInfo = UserConfig.Instance.GetUser(user.Info).Info;
//            if (needsNewLines == true)
//                return $"{userInfo.FirstName} ({userInfo.UserName}) - правильных ответов: {user.Statistics.RightAnswers}, неправильных ответов: {user.Statistics.WrongAnswers},{Environment.NewLine}пропущеных вопросов: {user.Statistics.SkipQuestions}, всего вопросов задано: {user.Statistics.TotalQuestions}";
//            else
//                return $"{userInfo.FirstName} ({userInfo.UserName}) - правильных ответов: {user.Statistics.RightAnswers}, неправильных ответов: {user.Statistics.WrongAnswers}, пропущеных вопросов: {user.Statistics.SkipQuestions}, всего вопросов задано: {user.Statistics.TotalQuestions}";
//        }
//        public static string GetLeaderboardAsString(this UserConfig userConfig, int? members = 10, bool? needsNewLines = true)
//        {
//            var i = 1;
//            var result = string.Empty;

//            foreach (var leaderboard in GetLeaderboard(members))
//            {
//                result += i.ToString() + ". " + ToUIString(leaderboard) + Environment.NewLine;
//                i++;
//            }
//            return result;
//        }
//    }
//}
