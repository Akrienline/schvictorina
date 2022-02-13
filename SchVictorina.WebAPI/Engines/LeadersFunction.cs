using SchVictorina.WebAPI.Utilities;
using System;
using System.Linq;

namespace SchVictorina.WebAPI.Engines
{
    public sealed class LeadersFunction : IFunction
    {
        public int MaxUsers { get; set; }

        public FunctionButton.Result Invoke()
        {
            var leaderboard = UserConfig.Instance.Users.OrderByDescending(users => users.Statistics.RightAnswers).Take(MaxUsers).ToArray();
            var resultInString = string.Empty;
            foreach (var user in leaderboard)
                resultInString += $"{user.Info.FirstName} ({user.Info.UserName}) - правильных ответов: {user.Statistics.RightAnswers}, неправильных ответов: {user.Statistics.WrongAnswers},{Environment.NewLine}пропущеных вопросов: {user.Statistics.SkipQuestions}, всего вопросов задано: {user.Statistics.TotalQuestions}";
            return new FunctionButton.Result { Text = resultInString + $"{Environment.NewLine} Это была десятка лучших!" };
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
