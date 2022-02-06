using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace SchVictorina.WebAPI.Utilities
{
    [XmlRoot("settings")]
    public sealed class GlobalConfig
    {
        public static GlobalConfig Instance
        {
            get
            {
                return File.ReadAllText("Config/settings.xml").FromXml<GlobalConfig>();
            }
        }

        [XmlElement("telegrambot")]
        public TelegramBotSettings TelegramBot { get; set; }
        [XmlElement("discordbot")]
        public DiscordBotSettings DiscordBot { get; set; }
        [XmlElement("logging")]
        public LoggingSettings Logging { get; set; }
        public class LoggingSettings
        {
            [XmlElement("errors")]
            public BaseLog Errors { get; set; }
            [XmlElement("warnings")]
            public BaseLog Warnings { get; set; }
            [XmlElement("requests")]
            public BaseLog Requests { get; set; }
            
            public class BaseLog
            {
                [XmlAttribute("enabled")]
                public bool Enabled { get; set; }

                [XmlAttribute("maxSizeInKB")]
                public int MaxSizeInKB { get; set; }

                [XmlAttribute("sendToUser")]
                public bool SendToUser { get; set; }

                [XmlText]
                public string Path { get; set; }
            }
        }
        public class TelegramBotSettings
        {
            [XmlElement("token")]
            public string Token { get; set; }

            [XmlElement("webhook")]
            public WebhookHoster Webhook { get; set; }

            public class WebhookHoster
            {
                [XmlAttribute("enabled")]
                public bool Enabled { get; set; }
                [XmlElement]
                public string Url { get; set; }
            }
        }

        public class DiscordBotSettings
        {
            [XmlElement("token")]
            public string Token { get; set; }
        }
    }
}
