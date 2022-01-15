using System;
using System.Net;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace Victorina
{
    internal class TemperatureEngine : BaseEngine
    {
        private static double? GetActualTemperature(int stationId)
        {
            var xmlElement = XElement.Load($"https://meteoinfo.ru/rss/forecasts/index.php?s={stationId}");
            var desc = xmlElement.Element("channel").Element("item").Element("description").Value;
            var temperature = desc.FindSubstring("днём", "°");
            return double.Parse(temperature, CultureInfo.InvariantCulture);
        }

        public override TaskInfo GenerateQuestion()
        {
            var nskTemperature = GetActualTemperature(29637); //Температура в Новосибирске
            var iskTemperature = GetActualTemperature(29730); //Температура в Искитиме
            var tlmTemperature = GetActualTemperature(29630); //Температура около Толмачёво
            return new TaskInfo
            {
                Question = "Какая погода сейчас в новосибирске (в центре)?",
                //IVQuestion = $"{nskTemperature}{iskTemperature}{tlmTemperature}",
                AnswerOptions = new object[]
                {
                    tlmTemperature.ToString(),
                    iskTemperature.ToString(),
                    nskTemperature.ToString()
                },
                RightAnswer = nskTemperature.ToString()
            };
        }

    }

    internal static class ConvertUtilities
    {
        public static string FindSubstring(this string text, string from, string to)
        {
            var startIndex = text.IndexOf(from);
            var endIndex = text.IndexOf(to, startIndex);
            startIndex += from.Length;
            var result = text.Substring(startIndex, endIndex - startIndex);
            return result;
        }
    }

}
