using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;

namespace SchVictorina.WebAPI.Utilities
{
    [XmlRoot("users")]
    public sealed class UserConfig
    {
        private static readonly string configPath = "Config/users.xml";
        private static UserConfig instance;
        private static bool hasChanges = false;
        private static Timer timer;
        private static object lockObject = new object();
        public enum UserRole
        {
            Student = 0,
            Teacher = 1,
            Administrator = 2
        }
        static UserConfig()
        {
            timer = new Timer(delegate
            {
                if (!hasChanges)
                    return;
                if (instance == null)
                    return;
                lock (lockObject)
                {
                    File.WriteAllText(configPath, instance.ToXml());
                    hasChanges = false;
                }
            }, null, new TimeSpan(-1), TimeSpan.FromSeconds(10));
        }

        public static UserConfig Instance
        {
            get
            {
                lock (lockObject)
                {
                    if (instance == null)
                    {
                        if (File.Exists(configPath))
                            instance = File.ReadAllText(configPath).FromXml<UserConfig>();
                        if (instance == null)
                            instance = new UserConfig();
                    }
                    return instance;
                }
            }
        }

        [XmlElement("user")]
        public List<User> Users { get; set; } = new List<User>();

        public User GetUser(User.UserInfo userInfo)
        {
            User user;
            lock (Users)
            {
                user = Users.FirstOrDefault(x => x.Info.UserId == userInfo.UserId);

                if (user == null)
                {
                    user = new User { Info = userInfo, Statistics = new User.StatisticsInfo() };
                    Users.Add(user);
                }
                else
                {
                    user.Info.UserName = userInfo.UserName;
                    user.Info.FirstName = userInfo.FirstName;
                    user.Info.LastName = userInfo.LastName;
                }
            }
            return user;
        }

        public void Log(User user, EventType eventType, double? score = 0)
        {
            hasChanges = true;

            user.Statistics.LastVisitDate = DateTime.Now;

            if (eventType == EventType.SendQuestion)
                user.Statistics.TotalQuestions += 1;
            else if (eventType == EventType.SkipQuestion)
                user.Statistics.SkipQuestions += 1;
            else if (eventType == EventType.RightAnswer)
                user.Statistics.RightAnswers += 1;
            else if (eventType == EventType.WrongAnswer)
                user.Statistics.WrongAnswers += 1;

            if (eventType == EventType.RightAnswer)
            {
                user.Statistics.RightInSequence += 1;
                user.Statistics.WrongInSequence = 0;
            }
            if (eventType == EventType.WrongAnswer)
            {
                user.Statistics.WrongInSequence += 1;
            }
            else if (eventType == EventType.SkipQuestion || eventType == EventType.WrongAnswer)
                user.Statistics.RightInSequence = 0;
            user.Statistics.Score += (double)score;
        }

        public enum EventType
        {
            Request,
            SendQuestion,
            SkipQuestion,
            RightAnswer,
            WrongAnswer
        }

        public sealed class User
        {
            [XmlElement("info")]
            public UserInfo Info { get; set; }

            [XmlElement("statistics")]
            public StatisticsInfo Statistics { get; set; }
            [XmlAttribute("hiden")]
            public bool IsHiden { get; set; } = false;
            [XmlAttribute("role")]
            public UserRole Role { get; set; } = UserRole.Student;
            public class UserInfo
            {
                [XmlAttribute("source")]
                public UserSourceType Source { get; set; }

                [XmlAttribute("userid")]
                public long UserId { get; set; }

                [XmlAttribute("username")]
                public string UserName { get; set; }

                [XmlAttribute("firstname")]
                public string FirstName { get; set; }

                [XmlAttribute("lastname")]
                public string LastName { get; set; }
            }

            public sealed class StatisticsInfo
            {
                [XmlAttribute("lastVisitDate")]
                public DateTime LastVisitDate { get; set; }

                [XmlAttribute("rightInSequence")]
                public int RightInSequence { get; set; }
                [XmlAttribute("wrongInSequence")]
                public int WrongInSequence { get; set; }

                [XmlAttribute("totalQuestions")]
                public int TotalQuestions { get; set; }
                [XmlAttribute("score")]
                public double Score { get; set; } = 0;

                [XmlAttribute("rightAnswers")]
                public int RightAnswers { get; set; }

                [XmlAttribute("wrongAnswers")]
                public int WrongAnswers { get; set; }

                [XmlAttribute("skipQuestions")]
                public int SkipQuestions { get; set; }
            }
        }
    }
}
