using SchVictorina.WebAPI.Controllers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SchVictorina.WebAPI.Utilities
{
    public static class ButtonConfig
    {
        private static ButtonRoot _buttonRoot;
        private static Dictionary<string, BaseButton> _allButtons;
        private static FileSystemWatcher _buttonsWatcher;
        private static FileSystemWatcher _excelWatcher;

        public delegate void ButtonChangedEventHandler();
        public static event ButtonChangedEventHandler ButtonListChanged;

        static ButtonConfig()
        {
            _buttonsWatcher = new FileSystemWatcher("Config/buttons", "*.xml") { IncludeSubdirectories = true };
            _buttonsWatcher.SimpleWatchFiles(() => ClearCache());

            _excelWatcher = new FileSystemWatcher("Config/excels", "*.xlsx") { IncludeSubdirectories = true };
            _excelWatcher.SimpleWatchFiles(() => ClearCache());
        }

        public static BaseButton GetButton(string id)
        {
            return AllButtons.ContainsKey(id) ? AllButtons[id] : null;
        }

        public static Dictionary<string, BaseButton> AllButtons
        {
            get
            {
                if (_allButtons == null)
                {
                    _allButtons = RootButton.Descendant.ToDictionary(x => x.ID, x => x);
                }
                return _allButtons;
            }
        }

        public static ButtonRoot RootButton
        {
            get
            {
                if (_buttonRoot == null)
                {
                    _buttonRoot = new ButtonRoot
                    {
                        Children = Directory.GetFiles("Config", "buttons_*.xml", SearchOption.AllDirectories)
                                        .Select(xmlPath =>
                                        {
                                            try
                                            {
                                                return System.IO.File.ReadAllText(xmlPath).FromXml<ButtonRoot>();
                                            }
                                            catch { return null; }
                                        })
                                        .Where(x => x != null)
                                        .OrderBy(root => root.Priority)
                                        .Where(root =>
                                        {
                                            root.ClassID = root.ClassID;
                                            return true;
                                        })
                                        .SelectMany(root => root.Children)
                                        .ToArray()
                    };

                    foreach (var button in _buttonRoot.Children)
                        button.Parent = _buttonRoot;

                    foreach (var pair in AllButtons)
                    {
                        if (pair.Value is GroupButton groupButton)
                        {
                            if (groupButton.Children != null)
                            {
                                foreach (var button in groupButton.Children)
                                    button.Parent = groupButton;
                            }
                        }
                    }
                }
                return _buttonRoot;
            }
        }

        private static void ClearCache()
        {
            _buttonRoot = null;
            _allButtons = null;
            ButtonListChanged?.Invoke();
        }
    }

    public abstract class BaseButton
    {
        [XmlAttribute("label")]
        public string Label { get; set; }

        internal string LabelWithParents
        {
            get
            {
                if (Parent != null && !string.IsNullOrEmpty(Parent.Label))
                    return $"{Parent.LabelWithParents} -> {Label}";
                return Label;
            }
        }


        internal readonly string AutoID = Guid.NewGuid().ToString("N").Substring(0, 8);
        private string _id = null;

        [XmlAttribute("id")]
        public string ID { get { return (_id ?? (_id = AutoID)); } set { _id = value; } }


        [XmlIgnore]
        internal GroupButton Parent { get; set; }

        [XmlAttribute("from")]
        public string FromDate { get; set; }

        [XmlAttribute("to")]
        public string ToDate { get; set; }

        [XmlIgnore]
        public virtual bool IsValid
        {
            get
            {
                if (!string.IsNullOrEmpty(FromDate))
                {
                    if (!DateTime.TryParse(FromDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fromDate) || fromDate > DateTime.Now)
                        return false;
                }
                if (!string.IsNullOrEmpty(ToDate))
                {
                    if (!DateTime.TryParse(ToDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime toDate) || toDate < DateTime.Now)
                        return false;
                }
                return true;
            }
        }
        [XmlIgnore]
        public bool IsValidWithAscender
        {
            get
            {
                return IsValid && (Parent == null || Parent.IsValidWithAscender);
            }
        }
    }
    public interface IClassableButton
    {
        [XmlIgnore]
        string ClassID { get; set; }

    }

    public class GroupButton : BaseButton, IClassableButton
    {
        [XmlElement("group", typeof(GroupButton))]
        [XmlElement("engine", typeof(EngineButton))]
        [XmlElement("split", typeof(SplitButton))]
        [XmlElement("function", typeof(FunctionButton))]
        public BaseButton[] Children { get; set; }

        internal IEnumerable<BaseButton> Descendant
        {
            get
            {
                if (Children == null)
                    yield break;
                foreach (var button in Children)
                {
                    yield return button;
                    if (button is GroupButton groupButton)
                    {
                        foreach (var child in groupButton.Descendant)
                            yield return child;
                    }
                }
            }
        }

        private string _classID;
        [XmlAttribute("classid")]
        public string ClassID
        {
            get { return _classID; }
            set
            {
                _classID = value;
                if (string.IsNullOrEmpty(_classID))
                    return;
                if (Children == null)
                    return;
                foreach (var child in Children.OfType<IClassableButton>()
                                              .Where(x => string.IsNullOrEmpty(x.ClassID)))
                {
                    child.ClassID = value;
                }
            }
        }

        [XmlIgnore]
        public override bool IsValid
        {
            get
            {
                if (Children != null && !Children.Any(x => x.IsValid))
                    return false;
                return base.IsValid;
            }
        }

    }

    [XmlRoot("buttons")]
    public sealed class ButtonRoot: GroupButton
    {
        [XmlAttribute("priority")]
        public int Priority { get; set; }
    }

    public abstract class ClassableButton<T> : BaseButton, IClassableButton
        where T : class
    {
        public sealed class Parameter
        {
            [XmlAttribute("id")]
            public string ID { get; set; }
            [XmlText]
            public string Value { get; set; }
        }
        [XmlAttribute("rightscore")]
        public double RightScore { get; set; }
        [XmlAttribute("wrongscore")]
        public double WrongScore { get; set; }
        [XmlAttribute("classid")]
        public string ClassID { get; set; }

        [XmlElement("parameter")]
        public Parameter[] Parameters { get; set; }

        private T _class;
        internal T Class
        {
            get
            {
                if (_class == null)
                {
                    var classType = Type.GetType(ClassID);
                    if (classType == null)
                        return null;
                    _class = (T)Activator.CreateInstance(classType);
                    if (Parameters != null)
                    {
                        foreach (var parameter in Parameters.GroupBy(x => x.ID)
                                                            .ToDictionary(x => x.Key, x => x.Select(x => x.Value)
                                                                                            .ToArray()))
                        {
                            var property = classType.GetProperty(parameter.Key);
                            if (property != null)
                            {
                                if (property.PropertyType.IsArray)
                                {
                                    var propertyType = property.PropertyType.GetElementType();
                                    var values = parameter.Value.Select(x => x.ParseTo(propertyType))
                                                                //.Where(x => x is string || x != null)
                                                                .ToArray();
                                    if (values.Any())
                                    {
                                        var arrayValue = (IList)Array.CreateInstance(propertyType, values.Length);
                                        for (var i = 0; i < values.Length; ++i)
                                            arrayValue[i] = values[i];
                                        property.SetValue(_class, arrayValue);
                                    }
                                }
                                else
                                {
                                    var value = parameter.Value.Single().ParseTo(property.PropertyType);
                                    if (value != null)
                                        property.SetValue(_class, value);

                                }
                            }
                            else
                            {
                                throw new Exception($"Couldn't set parameter '{parameter.Key}' with value '{parameter.Value[0]}' on class '{ClassID}'");
                            }
                        }
                    }
                }
                return _class;
            }
        }

        [XmlIgnore]
        public override bool IsValid
        {
            get
            {
                if (string.IsNullOrEmpty(ClassID) || Type.GetType(ClassID) == null)
                    return false;
                return base.IsValid;
            }
        }
    }

    public sealed class EngineButton: ClassableButton<BaseEngine>
    {
    }

    public interface IFunction
    {
        FunctionButton.Result Invoke(Update update);
    }

    public sealed class FunctionButton : ClassableButton<IFunction>
    {
        public sealed class Result
        {
            public string Text;
            public string ImagePath;
            public ParseMode? ParseMode;
        }
    }

    public sealed class SplitButton : BaseButton
    {
    }
}
