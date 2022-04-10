using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using System.Linq;
using DocumentFormat.OpenXml.Spreadsheet;

namespace SchVictorina.WebAPI.Utilities
{
    public class ExcelDocument
    {
        public string FilePath { get; private set; }
        public List<Sheet> Sheets { get; set; } = new List<Sheet>();

        public static ExcelDocument Open(string filepath)
        {
            filepath = Path.GetFullPath(filepath);
            if (string.IsNullOrWhiteSpace(filepath))
                throw new ArgumentNullException("filename");
            if (!File.Exists(filepath))
                throw new ArgumentException($"File {filepath} not exists.");
            var document = new ExcelDocument() { FilePath = filepath };
            using (var doc = SpreadsheetDocument.Open(filepath, false))
            {
                var workbookPart = doc.WorkbookPart;
                var sharedStringsTable = workbookPart.SharedStringTablePart?.SharedStringTable;
                foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
                {
                    var worksheet = worksheetPart.Worksheet;
                    var partId = workbookPart.GetIdOfPart(worksheetPart);
                    var sheetName = workbookPart.Workbook.Sheets.Cast<DocumentFormat.OpenXml.Spreadsheet.Sheet>().First(x => x.Id == partId).Name;
                    var sheet = new Sheet() { Name = sheetName };
                    document.Sheets.Add(sheet);

                    var sheetData = worksheet.GetFirstChild<SheetData>();
                    var rows = sheetData.OfType<DocumentFormat.OpenXml.Spreadsheet.Row>().OrderBy(x => x.RowIndex.Value).ToArray();
                    foreach (var row in rows)
                    {
                        var values = ParseExcelRow(row, sharedStringsTable);
                        if (sheet.Columns.Count == 0)
                            sheet.Columns.AddRange(values.Values.Select(x => new Column() { Name = x }));
                        else
                            sheet.Rows.Add(new Row(
                                Enumerable.Range(0, sheet.Columns.Count)
                                          .Select(i => values.ContainsKey(i) ? values[i] : null)
                                          .ToArray()
                            ));
                    }
                }
            }
            return document;
        }
        private static Dictionary<int, string> ParseExcelRow(DocumentFormat.OpenXml.Spreadsheet.Row row, SharedStringTable sharedStringsTable)
        {
            var values = new Dictionary<int, string>();
            var cells = row.OfType<Cell>().ToArray();
            foreach (var cell in cells)
            {
                var value = cell.CellValue?.Text;
                if (cell.DataType != null)
                {
                    switch (cell.DataType.Value)
                    {
                        case CellValues.SharedString:
                        {
                            if (int.TryParse(cell.CellValue?.Text, out int index))
                                value = sharedStringsTable.Elements<SharedStringItem>().ElementAt(index).InnerText ?? null;
                            break;
                        }
                        case CellValues.InlineString:
                        {
                            value = cell.InlineString?.InnerText;
                            break;
                        }
                    }
                }
                values[cell.CellReference.Value[0] - 'A'] = value;
            }
            return values;
        }
        public class Sheet
        {
            public string Name { get; set; }
            public List<Column> Columns { get; set; } = new List<Column>();
            public List<Row> Rows { get; set; } = new List<Row>();
            public int GetColumnIndex(string columnName)
            {
                for (var i = 0; i < Columns.Count; i++)
                {
                    if (Columns[i].Name == columnName)
                        return i;
                }
                return -1;
            }
            public override string ToString()
            {
                return $"{Name} ({Columns.Count} columns, {Rows.Count} rows)";
            }
        }
        public class Column
        {
            public string Name { get; set; }
            public override string ToString()
            {
                return Name;
            }
        }
        public class Row
        {
            public Row(string[] values)
            {
                Values = values;
            }
            public string[] Values { get; private set; }
        }
    }
}