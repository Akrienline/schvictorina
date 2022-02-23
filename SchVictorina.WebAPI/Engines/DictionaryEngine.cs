using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SchVictorina.WebAPI.Utilities;

namespace SchVictorina.WebAPI.Engines
{
    public class DictionaryEngine : BaseEngine
    {
        public string FilePath { get; set; }
        public string Filter { get; set; }
        public int WrongAnswerCount { get; set; } = 2;

        DictionaryDocument document;
        public override QuestionInfo GenerateQuestion()
        {
            if (document == null)
            {
                document = DictionaryDocument.Open(FilePath);
                document.DataRows = document.DataRows.WithinFilter(Filter).ToArray();
            }

            var questionRow = document.Questions[RandomUtilities.GetRandomIndex(document.Questions.Length)];
            //questionRow = document.Questions[6];
            var dataRows = document.DataRows.WithinFilter(questionRow.Filter).ToArray();

            var randomRow = dataRows[RandomUtilities.GetRandomIndex(dataRows.Length)];
            if (!string.IsNullOrWhiteSpace(questionRow.Equal))
                dataRows = dataRows.WithinFilter($"{questionRow.Equal} = {randomRow[questionRow.Equal]}").ToArray();

            var answerRow = !string.IsNullOrWhiteSpace(questionRow.OrderBy)
                                ? dataRows.OrderBy(row => row[questionRow.OrderBy].ToDouble()).FirstOrDefault()
                                : !string.IsNullOrWhiteSpace(questionRow.OrderByDescending)
                                    ? dataRows.OrderByDescending(row => row[questionRow.OrderByDescending].ToDouble()).FirstOrDefault()
                                    : randomRow;

            var question = questionRow.Question;
            foreach (var columnName in answerRow.Keys)
                question = question.Replace("{" + columnName + "}", answerRow[columnName]);

            var wrongCandidates = dataRows.Where(candidate => candidate != answerRow).ToArray();

            var wrongAnswers = dataRows.OrderByRandom()
                                       .Select(row => row[questionRow.Answer])
                                       .Where(x => x != answerRow[questionRow.Answer])
                                       .Distinct()
                                       .Take(WrongAnswerCount)
                                       .ToArray();

            return new QuestionInfo()
            {
                Question = question,
                RightAnswer = answerRow[questionRow.Answer],
                WrongAnswers = wrongAnswers
            };
        }
    }
    public class DictionaryDocument
    {

        public Dictionary<string, string>[] DataRows { get; set; }
        public QuestionInfo[] Questions { get; set; }
        public static DictionaryDocument Open(string filename)
        {
            var excelDocument = ExcelDocument.Open(filename);
            var doc = new DictionaryDocument();
            var questionSheet = excelDocument.Sheets.First(x => x.Columns.Exists(y => y.Name == "question") &&
                                                                x.Columns.Exists(z => z.Name == "answer") &&
                                                                x.Columns.Exists(a => a.Name == "equal"));
            var dataSheet = excelDocument.Sheets.First(x => x != questionSheet);

            doc.DataRows = dataSheet.Rows.Select(
                    x => dataSheet.Columns.Zip(x.Values, (key, value) => new { Key = key.Name, Value = value })
                                          .ToDictionary(x => x.Key, x => x.Value)
                ).ToArray();

            doc.Questions = questionSheet.Rows.Select(x => new QuestionInfo
            {
                Question = x.Values[questionSheet.GetColumnIndex("question")],
                Filter = x.Values[questionSheet.GetColumnIndex("filter")],
                Equal = x.Values[questionSheet.GetColumnIndex("equal")],
                OrderBy = x.Values[questionSheet.GetColumnIndex("orderBy")],
                OrderByDescending = x.Values[questionSheet.GetColumnIndex("orderByDesc")],
                Answer = x.Values[questionSheet.GetColumnIndex("answer")]
            }).ToArray();

            return doc;
        }
        public class QuestionInfo
        {
            public string Question { get; set; }
            public string Filter { get; set; }
            public string Equal { get; set; }
            public string OrderBy { get; set; }
            public string OrderByDescending { get; set; }
            public string Answer { get; set; }
        }
    }

    internal static class DictionaryDocumentUtilities
    {
        public static bool WithinFilter(this Dictionary<string, string> row, params string[] filters)
        {

            if (row == null)
                throw new ArgumentNullException(nameof(row));
            if (filters == null)
                return true;
            filters = filters.Where(x => !string.IsNullOrWhiteSpace(x))
                             .SelectMany(x => x.Split(';'))
                             .ToArray();
            if (filters.Length == 0)
                return true;


            var processors = new Dictionary<string, object>
            {
                { "<=", new Func<double?, double?, bool>((x1, x2) => x1 <= x2 ) },
                { ">=", new Func<double?, double?, bool>((x1, x2) => x1 >= x2 ) },
                { "<>", new Func<string, string, bool>((x1, x2) => x1 != x2 ) },
                { "!=", new Func<string, string, bool>((x1, x2) => x1 != x2 ) },
                { "!~", new Func<string, string, bool>((x1, x2) => !x1.Contains(x2)) },
                { "~", new Func<string, string, bool>((x1, x2) => x1.Contains(x2)) },
                { "<", new Func<double?, double?, bool>((x1, x2) => x1 < x2) },
                { ">", new Func<double?, double?, bool>((x1, x2) => x1 > x2) },
                { "=", new Func<string, string, bool>((x1, x2) => x1 == x2) }
            };
            foreach (var filter in filters)
            {
                foreach (var processor in processors)
                {
                    if (!filter.Contains(processor.Key))
                        continue;
                    var parts = filter.Split(processor.Key).Select(x => x.Trim()).ToArray();
                    var columnName = parts[0];
                    var value = parts[1].Replace(" ", string.Empty).Replace(",", ".");
                    var rowValue = row[columnName]?.Replace(" ", string.Empty).Replace(",", ".");

                    if (processor.Value is Func<double?, double?, bool> doubleFunc)
                    {
                        if (!doubleFunc(rowValue.ToDouble(), value.ToDouble()))
                            return false;
                    }
                    else if (processor.Value is Func<string, string, bool> stringFunc)
                    {
                        if (!stringFunc(rowValue, value))
                            return false;
                    }
                }

            }
            return true;
        }
        public static IEnumerable<Dictionary<string, string>> WithinFilter(this IEnumerable<Dictionary<string, string>> rows, params string[] filters)
        {
            return rows.Where(row => row.WithinFilter(filters));
        }
    }
}
