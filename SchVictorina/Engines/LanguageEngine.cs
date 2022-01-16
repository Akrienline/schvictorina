using GoogleTranslateFreeApi;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using SchVictorina;
using SchVictorina.Utilites;

namespace SchVictorina.Engines
{
    //[Engine("language", "Угадай язык")]
    public class LanguageEngine// : BaseEngine
    {
        static Random random = new Random();
        public static string RandomText()
        {
            #region Texts
            var text1 = "Мало кто знает, что английская народная сказка Три поросенка на самом деле сатирическая история с политическим подтекстом. Образы забавных героев олицетворяли глав трех государств, которые дружили между собой. А в роли злобного и беспощадного волка выступил финансовый кризис. Только благодаря практичному уму поросенка Наф Нафа, два его беспечных друга, остались в живых, а волк просто вылетел в трубу.";
            var text2 = "Баба-Яга фольклорный персонаж, который встречается не только в славянской мифологии, но и в других национальных сказках. Она может выступать как в роли доброй, так и злой хранительницы леса, как, например, в этой сказочной истории. Однако собиратель славянского фольклора уверены, что в русском лесу живет не одна, а целых две сказочных бабушки-ведьмы, одна из которых помогает, а другая вредит. Читать сказку Баба-Яга своим детям вы можете прямо на этой странице.";
            var text3 = "У Лукоморья дуб зеленый переносит малыша в сказочное пространство. Каким образом? Автор много раз употребляет слово ТАМ – и кроха начинает верить, что где-то ТАМ в фантазийном мире происходят чудесные события, прикоснуться к которым может каждый, кто слушает волшебную историю. Даже слова, использованные автором, способствуют созданию сказочной атмосферы. Невиданные звери и неведомые дорожки сопровождают ребенка в царство сказки. Строки поэмы непременно подстегнут неутомимое детское воображение.";
            var text4 = "Эта чудесная сказка Айболит которую вы можете читать для детей, развивает фантазию и расширяет их кругозор. Незаметно для себя дети впитывают хорошие советы и указания, которые автор дает в сказке. Иногда бывает скучно слушать маму и папу, гораздо интереснее, если советы дают львы, тигры или птицы. Своей сказкой Чуковский учит ребят понимать связь между явлениями, обогащает их словарный запас, учит думать и сопереживать. Сказки улучшают память и даже могут изменять поступки ребенка. Читайте своим детям эту яркую сказку Айболит!";
            var text5 = "Старушка Федора крайне небрежно относилась к чистоте кухонной утвари, да и ко внешнему виду тоже. Однажды ее вещи не выдержали подобного безобразия и решили сбежать от неряшливой хозяйки. Невыносимо стало жить Федоре: ни чаю попить не может – ведь самовар ушел, ни щи сварить – ведь кастрюли также нет. Призадумалась Федора и решила навести в жилище порядок – да тут и посуда вернулась. На радостях Федора обещает впредь быть аккуратной и заботится о чистоте всех своих вещей. ";
            var text6 = "Баба-Яга фольклорный персонаж, который встречается не только в славянской мифологии, но и в других национальных сказках. Она может выступать как в роли доброй, так и злой хранительницы леса, как, например, в этой сказочной истории. Однако собиратель славянского фольклора уверены, что в русском лесу живет не одна, а целых две сказочных бабушки-ведьмы, одна из которых помогает, а другая вредит.";
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
        public static string GetNormalizedLanguageString(Language language)
        {
            if (language == Language.Russian)
                return "Русский";
            else if (language == Language.English)
                return "Английский";
            else if (language == Language.Chichewa)
                return "Чичева";
            else
                return "Немецкий";
        }
        public static string TranslateText(string text, Language language)
        {

            var translator = new GoogleTranslator();

            Language from = Language.Russian;
            Language to = language;

            var result = Task.Run<TranslationResult>(async () => await translator.TranslateLiteAsync(text, from, Language.English)).Result;
            //TranslationResult result = translator.TranslateLiteAsync(text, from, to).wa;
            //You can get all text using MergedTranslation property
            return result.MergedTranslation;

        }
        public static TaskInfo GenerateQuestion()
        {
            var randomText = RandomText();
            var randomLang = RandomLang();
            var normalizedRandomLang = GetNormalizedLanguageString(randomLang);
            var translatedText = TranslateText(randomText, randomLang);

            return new TaskInfo
            {
                Question = $"На каком языке написан этот текст: {translatedText}",
                AnswerOptions = new object[]
                {
                    "Русский",
                    "Английский",
                    "Чичева",
                    "Немецкий"
                },
                RightAnswer = normalizedRandomLang
            };
        }
    }
}
