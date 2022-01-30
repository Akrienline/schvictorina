using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace SchVictorina.WebAPI.Utilities
{
    public static class GlobalConfig
    {
        public static GlobalSettings Instance
        {
            get
            {
                return File.ReadAllText("Config/settings.xml").FromXml<GlobalSettings>();
            }
        }
    }

    [XmlRoot("settings")]
    public class GlobalSettings
    {
        [XmlElement("telegrambot")]
        public TelegramBotSettings TelegramBot;
        [XmlElement("discordbot")]
        public DiscordBotSettings DiscordBot;

        public class TelegramBotSettings
        {
            [XmlElement("token")]
            public string Token;

            [XmlElement("webhook")]
            public WebhookHoster Webhook;

            public class WebhookHoster
            {
                [XmlAttribute("enabled")]
                public bool Enabled;
                [XmlElement]
                public string Url;
            }
        }

        public class DiscordBotSettings
        {
            [XmlElement("token")]
            public string Token;
        }
    }
}
