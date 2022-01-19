namespace SchVictorina.WebAPI
{
    public class TaskInfo
    {
        public string? Question { get; set; }
        public object? RightAnswer { get; set; }
        public object[]? AnswerOptions { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class EngineAttribute : Attribute
    {
        public string ApiName { get; private set; }
        public string UIName { get; private set; }
        public EngineAttribute(string apiName, string uiName)
        {
            ApiName = apiName;
            UIName = uiName;
        }
    }

    public abstract class BaseEngine
    {
        public abstract TaskInfo GenerateQuestion();

        internal readonly static Dictionary<EngineAttribute, Type> AllEngineTypes =
                typeof(BaseEngine).Assembly
                                .GetTypes()
                                .Where(x => x.IsSubclassOf(typeof(BaseEngine)))
                                .ToDictionary(x => ((EngineAttribute)x.GetCustomAttributes(typeof(EngineAttribute), true)[0]),
                                                x => x);

    }
    public abstract class Parameter
    {
        string Name = "";
        object? Value = "";
    }
}
