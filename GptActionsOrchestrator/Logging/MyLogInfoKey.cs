using NuciLog.Core;

namespace GptActionsOrchestrator.Logging
{
    public sealed class MyLogInfoKey : LogInfoKey
    {
        private MyLogInfoKey(string name) : base(name) { }

        public static LogInfoKey GptAction => new MyLogInfoKey(nameof(GptAction));
        public static LogInfoKey AppId => new MyLogInfoKey(nameof(AppId));
        public static LogInfoKey Count => new MyLogInfoKey(nameof(Count));
        public static LogInfoKey DateBeginning => new MyLogInfoKey(nameof(DateBeginning));
        public static LogInfoKey DateEnd => new MyLogInfoKey(nameof(DateEnd));
        public static LogInfoKey Localisation => new MyLogInfoKey(nameof(Localisation));
        public static LogInfoKey Path => new MyLogInfoKey(nameof(Path));
        public static LogInfoKey Reference => new MyLogInfoKey(nameof(Reference));
        public static LogInfoKey Repository => new MyLogInfoKey(nameof(Repository));
        public static LogInfoKey Template => new MyLogInfoKey(nameof(Template));
        public static LogInfoKey Username => new MyLogInfoKey(nameof(Username));
    }
}
