using SchVictorina.WebAPI.Utilites;
using System;

namespace SchVictorina.WebAPI.Engines
{
    public class EnglishEngine: BaseEngine
    {
        public object no;
        public bool RandomTrueOrFalse()
        {
            var random = new Random();
            var randomInt = random.Next(1, 3);
            no = null;
            if (randomInt == 1)
                return true;
            else
                return false;
        }
        public string RandomTrueOrFalseAndToString()
        {
            bool randomBool = RandomTrueOrFalse();
            return BoolToString(randomBool);
        }
        public string BoolToString(bool boolean)
        {
            if (boolean == true)
                return "Yes!";
            else
                return "No.";
        }
        public string BreakSentenceIfNeeded(string sentence, bool boolean)
        {
            if (boolean == true)
            {
                var sentence2 = sentence.Replace('e', 'i');
                var sentence3 = sentence2.Replace('o', 'a');
                return sentence3;
            }
            else
                return sentence;
        }
        public override TaskInfo GenerateQuestion()
        {
            var trueOrFalse = RandomTrueOrFalse();
            string[] sentences = {"I have to go to the university.",
                                  "There is the house where my family lives.",
                                  "They didn’t go to school last year.",
                                  "Go and buy some bread, please.",
                                  "Let the children play with our dog.",
                                  "What a lovely day it is." };
            var randomSentence = ArrayUtilites.RandomMemberFromArray(sentences).ToString();
            var breakedSentence = BreakSentenceIfNeeded(randomSentence, trueOrFalse);
            no = "";
            return new TaskInfo
            {
                Question = $"Есть ли здесь ошибка: {breakedSentence}",
                AnswerOptions = new object[] { "Yes!", "No."},
                RightAnswer = BoolToString(trueOrFalse)
            };
        }
        
    }
}
