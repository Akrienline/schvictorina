using SchVictorina_WebAPI;

namespace SchVictorina_WebAPI.Engines
{
    public abstract class BaseButton
    {
        public string Text = "";
    }
    public sealed class GroupButton : BaseButton
    {
        public BaseButton[]? buttons;
    }
    public sealed class BasicEngineButton : BaseButton // Engine's button without any parameters
    {
        public BaseEngine? engine;
    }
    public sealed class AdvancedEngineButton : BaseButton // Engine's button with parameters.
    {
        public BaseEngine? engine;
        public Parameter[]? parameters;
    }

}
