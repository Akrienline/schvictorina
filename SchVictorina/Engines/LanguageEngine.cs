using GoogleTranslateFreeApi;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Victorina;

namespace SchVictorina.Engines
{
    public class LanguageEngine : BaseEngine
    {
        static Random random = new Random();
        public static string RandomText()
        {
            #region Texts
            var text1 = "Мало кто знает, что английская народная сказка Три поросенка на самом деле сатирическая история с политическим подтекстом. Образы забавных героев олицетворяли глав трех государств, которые дружили между собой. А в роли злобного и беспощадного волка выступил финансовый кризис. Только благодаря практичному уму поросенка Наф Нафа, два его беспечных друга, остались в живых, а волк просто вылетел в трубу.";
            var text2 = "Баба-Яга фольклорный персонаж, который встречается не только в славянской мифологии, но и в других национальных сказках. Она может выступать как в роли доброй, так и злой хранительницы леса, как, например, в этой сказочной истории. Однако собиратель славянского фольклора уверены, что в русском лесу живет не одна, а целых две сказочных бабушки-ведьмы, одна из которых помогает, а другая вредит. Читать сказку Баба-Яга своим детям вы можете прямо на этой странице.";
            var text3 = "";
            var text4 = "";
            var text5 = "";
            var text6 = "";
            #endregion
            var textId = random.Next(1, 7);
            return textId switch
            {
                1 => text1,
                2 => text2,
                3 => text3,
                4 => text4,
                5 => text5,
                _ => text6,
            };

        }
        public static Language RandomLang()
        {
            var langId = random.Next(1, 5);
            return langId switch
            {
                1 => Language.Russian,
                2 => Language.English,
                3 => Language.Chichewa,
                _ => Language.German
            };
        }
        public static async Task<string> TranslateText(string text, Language language)
        {

            var translator = new GoogleTranslator();

            Language from = Language.Russian;
            Language to = language;

            TranslationResult result = await translator.TranslateLiteAsync(text, from, to);
            //You can get all text using MergedTranslation property
            return result.MergedTranslation;

        }
        public override TaskInfo GenerateQuestion()
        {
            var randomText = RandomText();
            var randomLang = RandomLang();
            var translatedText = TranslateText(randomText, randomLang);

            return new TaskInfo
            {
                Question = $"На каком языке написан этот текст: {translatedText}",
                AnswerOptions = new object[]
                {
                    randomLang.ToString(),
                    randomLang.ToString(),
                    randomLang.ToString()
                },
                RightAnswer = randomLang.ToString()
            };
        }

    }
}
