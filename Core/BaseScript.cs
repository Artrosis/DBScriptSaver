using DBScriptSaver.ViewModels;

namespace DBScriptSaver.Core
{
    public abstract class BaseScript : IScript
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public string ScriptText { get; set; }
        public string ObjectType { get; set; }
        public ChangeType ChangeState { get; set; }
        public string ObjName { get; set; }
        public abstract IScript Copy();
    }
}