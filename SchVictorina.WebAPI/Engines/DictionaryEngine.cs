using System.Collections.Generic;
using System.Linq;
using SchVictorina.WebAPI.Utilities;

namespace SchVictorina.WebAPI.Engines
{
    public class DictionaryEngine
    {
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
                WrongOrderMaxIndex = x.Values[questionSheet.GetColumnIndex("wrongOrderMaxIndex")],
                NotEqual = x.Values[questionSheet.GetColumnIndex("notequal")],
                OrderBy = x.Values[questionSheet.GetColumnIndex("orderBy")],
                Answer = x.Values[questionSheet.GetColumnIndex("answer")]
            }).ToArray();

            return doc;
        }
        public class QuestionInfo
        {
            public string Question { get; set; }
            public string Filter { get; set; }
            public string Equal { get; set; }
            public string WrongOrderMaxIndex { get; set; }
            public string NotEqual { get; set; }
            public string OrderBy { get; set; }
            public string Answer { get; set; }
        }
    }
}
