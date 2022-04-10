using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using SchVictorina.WebAPI.Utilities;

namespace SchVictorina.WebAPI.Engines
{
    public class DictionaryEngine : BaseEngine
    {
        public string[] FilePath { get; set; }
        public string[] Filter { get; set; }
        public int WrongAnswerCount { get; set; } = 2;

        private DictionaryDocument[] documents;

        public override QuestionInfo GenerateQuestion()
        {
            if (documents == null)
            {
                documents = FilePath.Zip(Filter, (filePath, filter) => new { filePath, filter } )
                                    .Select(x =>
                                    {
                                        var doc = DictionaryDocument.Open(x.filePath);
                                        doc.DataRows = doc.DataRows.WithinFilter(x.filter).ToArray();
                                        return doc;
                                    })
                                    .ToArray();
            }

            var documentIndex = RandomUtilities.GetRandomIndex(documents.Length);
            var document = documents[documentIndex];
            var questionRowIndex = RandomUtilities.GetRandomIndex(document.Questions.Length);
            var questionRow = document.Questions[questionRowIndex];
            //questionRow = document.Questions[6];
            var dataRows = document.DataRows.WithinFilter(questionRow.Filter).ToArray();

            var randomRowIndex = RandomUtilities.GetRandomIndex(dataRows.Length);
            var randomRow = dataRows[randomRowIndex];
            if (!string.IsNullOrWhiteSpace(questionRow.Equal))
                dataRows = dataRows.WithinFilter($"{questionRow.Equal} = {randomRow[questionRow.Equal]}").ToArray();

            var answerRow = !string.IsNullOrWhiteSpace(questionRow.OrderBy)
                                ? dataRows.OrderBy(row => row[questionRow.OrderBy].ToDouble()).FirstOrDefault()
                                : !string.IsNullOrWhiteSpace(questionRow.OrderByDescending)
                                    ? dataRows.OrderByDescending(row => row[questionRow.OrderByDescending].ToDouble()).FirstOrDefault()
                                    : randomRow;

            var question = questionRow.Question;
            foreach (var columnName in answerRow.Keys)
            {
                var value = answerRow[columnName];
                if (value.ToDouble().HasValue)
                    value = value.ToDouble().Value.ToString("N0", new CultureInfo("ru-RU"));
                    question = question.Replace("{" + columnName + "}", "**" + value + "**");
            }

            var wrongCandidates = dataRows.Where(candidate => candidate != answerRow).ToArray();
            var wrongAnswers = dataRows.OrderByRandom()
                                       .Select(row => row[questionRow.Answer])
                                       .Where(x => x != answerRow[questionRow.Answer])
                                       .Distinct()
                                       .Take(WrongAnswerCount)
                                       .ToArray();

            if (wrongAnswers.Length < WrongAnswerCount)
                wrongAnswers = wrongAnswers.Concat(
                                                document.DataRows
                                                        .Select(row => row[questionRow.Answer])
                                                        .Distinct()
                                                        .Except(wrongAnswers)
                                                        .OrderByRandom()
                                                        .Take(WrongAnswerCount - wrongAnswers.Length)
                                           ).ToArray();

            return new QuestionInfo
            {
                Question = question,
                QuestionImagePath = !string.IsNullOrEmpty(questionRow.QuesitonImage) ? GetImagePath(document.FilePath, answerRow[questionRow.QuesitonImage]) : null,
                RightAnswer = new AnswerOption($"d{documentIndex}_q{questionRowIndex}_c{document.DataRows.IndexOf(answerRow)}_s{document.DataRows.IndexOf(answerRow)}", answerRow[questionRow.Answer]),
                WrongAnswers = wrongAnswers.Select(x =>
                {
                    var prefixId = $"d{documentIndex}_q{questionRowIndex}_c{document.DataRows.IndexOf(answerRow)}";
                    var dataRow = dataRows.FirstOrDefault(y => object.Equals(y[questionRow.Answer], x));
                    return dataRow != null
                            ? new AnswerOption($"{prefixId}_s{document.DataRows.IndexOf(dataRow)}", x)
                            : new AnswerOption(prefixId, x);
                }).ToArray()
            };
        }
        
        public override AnswerInfo ParseAnswerId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            var parts = id.Split('_');
            if (parts.Length < 3)
                return null;

            var document = documents[parts[0].Trim('d').ToInt()];
            var question = document.Questions[parts[1].Trim('q').ToInt()];
            var correctAnswerRow = document.DataRows[parts[2].Trim('c').ToInt()];
            var selectedAnswerRow = parts.Length == 4 ? document.DataRows[parts[3].Trim('s').ToInt()] : null;
            
            var answerInfo = new AnswerInfo
            {
                RightAnswer = correctAnswerRow[question.Answer],
                SelectedAnswer = selectedAnswerRow != null ? selectedAnswerRow[question.Answer] : null,
            };

            if (!string.IsNullOrEmpty(question.Description))
                answerInfo.Description = correctAnswerRow[question.Description];
            
            if (question.DescriptionImage != null && string.IsNullOrEmpty(question.QuesitonImage))
                answerInfo.DescriptionImagePath = GetImagePath(document.FilePath, correctAnswerRow[question.DescriptionImage]);

            return answerInfo;
        }

        private static string GetImagePath(string docFileName, string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            var imagePath = $"Config/excels/{Path.GetFileNameWithoutExtension(docFileName)}/{name}";
            foreach (var ext in new[] { ".jpeg", ".jpg", ".png" })
            {
                if (File.Exists(imagePath + ext))
                    return imagePath + ext;
            }
            return null;
        }
    }
    public class DictionaryDocument
    {
        public string FilePath { get; set; }
        public Dictionary<string, string>[] DataRows { get; set; }
        public QuestionInfo[] Questions { get; set; }
        public static DictionaryDocument Open(string filename)
        {
            var excelDocument = ExcelDocument.Open(filename);
            var doc = new DictionaryDocument();
            doc.FilePath = filename;
            var questionSheet = excelDocument.Sheets.First(x => x.Columns.Exists(y => y.Name == "question") &&
                                                                x.Columns.Exists(z => z.Name == "answer") &&
                                                                x.Columns.Exists(a => a.Name == "equal"));
            var dataSheet = excelDocument.Sheets.First(x => x != questionSheet);

            doc.DataRows = dataSheet.Rows.Select(
                    x => dataSheet.Columns.Zip(x.Values, (key, value) => new { Key = key.Name, Value = value })
                                          .ToDictionary(x => x.Key, x => x.Value)
                ).Where(x => x.Any(y => !string.IsNullOrEmpty(y.Value))).ToArray();

            doc.Questions = questionSheet.Rows.Select(x => new QuestionInfo
            {
                Question = x.Values[questionSheet.GetColumnIndex("question")],
                Filter = x.Values[questionSheet.GetColumnIndex("filter")],
                Equal = x.Values[questionSheet.GetColumnIndex("equal")],
                OrderBy = x.Values[questionSheet.GetColumnIndex("orderBy")],
                OrderByDescending = x.Values[questionSheet.GetColumnIndex("orderByDesc")],
                Answer = x.Values[questionSheet.GetColumnIndex("answer")],
                Description = x.Values[questionSheet.GetColumnIndex("description")],
                DescriptionImage = x.Values[questionSheet.GetColumnIndex("descriptionImage")],
                QuesitonImage = x.Values[questionSheet.GetColumnIndex("questionImage")]
            }).Where(x => !string.IsNullOrEmpty(x.Question)).ToArray();

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
            public string Description { get; set; }
            public string DescriptionImage { get; set; }
            public string QuesitonImage { get; set; }
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
