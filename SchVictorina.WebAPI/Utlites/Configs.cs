using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace SchVictorina.WebAPI.Utilites
{
    public static class Config
    {
        private static ButtonRoot _buttonRoot;
        private static Dictionary<string, BaseButton> _allButtons;
        private static FileSystemWatcher _watcher;

        static Config()
        {
            _watcher = new FileSystemWatcher("Config", "*.xml");
            _watcher.EnableRaisingEvents = true;
            _watcher.IncludeSubdirectories = true;
            _watcher.NotifyFilter = NotifyFilters.LastWrite;
            _watcher.Changed += delegate { _buttonRoot = null; };
            _watcher.Created += delegate { _buttonRoot = null; };
            _watcher.Deleted += delegate { _buttonRoot = null; };
            _watcher.Renamed += delegate { _buttonRoot = null; };
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
                                                return File.ReadAllText(xmlPath).FromXml<ButtonRoot>();
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
    }

    public abstract class BaseButton
    {
        [XmlAttribute("label")]
        public string Label { get; set; }

        [XmlIgnore]
        internal readonly string ID = Guid.NewGuid().ToString("N");
        [XmlIgnore]
        internal GroupButton Parent { get; set; }
    }
    public interface IEnginableButton
    {
        string ClassID { get; set; }
    }

    public class GroupButton : BaseButton, IEnginableButton
    {
        [XmlElement("group", typeof(GroupButton))]
        [XmlElement("engine", typeof(EngineButton))]
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
                foreach (var child in Children.OfType<IEnginableButton>()
                                              .Where(x => string.IsNullOrEmpty(x.ClassID)))
                {
                    child.ClassID = value;
                }
            }
        }
    }

    [XmlRoot("buttons")]
    public sealed class ButtonRoot: GroupButton
    {
        [XmlAttribute("priority")]
        public int Priority { get; set; }
    }
    public sealed class EngineButton: BaseButton, IEnginableButton
    {
        [XmlAttribute("classid")]
        public string ClassID { get; set; }
        [XmlElement("parameter")]
        public EngineParameter[] Parameters { get; set; }

        private BaseEngine _engine;
        internal BaseEngine Engine
        {
            get
            {
                if (_engine == null)
                {
                    var engineType = Type.GetType(ClassID);
                    if (engineType == null)
                        return null;
                    _engine = (BaseEngine)Activator.CreateInstance(engineType);
                    if (Parameters != null)
                    {
                        foreach (var parameter in Parameters)
                        {
                            var property = engineType.GetProperty(parameter.ID);
                            if (property != null)
                            {
                                var value = parameter.Value.ParseTo(property.PropertyType);
                                if (value != null)
                                    property.SetValue(_engine, value);
                            }
                        }
                    }
                }
                return _engine;
            }
        }
    }

    public sealed class EngineParameter
    {
        [XmlAttribute("id")]
        public string ID { get; set; }
        [XmlText]
        public string Value { get; set; }
    }
}
