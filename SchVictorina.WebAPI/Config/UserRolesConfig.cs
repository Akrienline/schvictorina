using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace SchVictorina.WebAPI.Utilities
{
    [XmlRoot("userroles")]
    public sealed class UserRolesConfig
    {
        public static UserRolesConfig Instance
        {
            get
            {
                return File.ReadAllText("Config/user_roles.xml").FromXml<UserRolesConfig>();
            }
        }

        [XmlElement("role")]
        public UserRole[] Roles;

        public class UserRole
        {
            [XmlAttribute("name")]
            public string Name;

            [XmlAttribute("type")]
            public RoleType Type;

            [XmlElement("user")]
            public User[] Users;

            public class User
            {
                [XmlAttribute("name")]
                public string Name;

                [XmlAttribute("source")]
                public UserSourceType Source;
            }

        }

        public enum RoleType
        {
            Admin,
            Teacher
        }
    }

    public enum UserSourceType
    {
        Telegram,
        Discord
    }
}
