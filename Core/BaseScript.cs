using DBScriptSaver.ViewModels;

namespace DBScriptSaver.Core
{
    public class BaseScript : IScript
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public string ScriptText { get; set; }
        public string ObjectType { get; set; }
        public ChangeType ChangeState { get; set; }
        public string ObjName { get; set; }
        public virtual IScript Copy()
        {
            BaseScript result = new BaseScript()
            {
                FileName = FileName,
                FullPath = FullPath,
                ScriptText = ScriptText,
                ChangeState = ChangeState,
                ObjectType = ObjectType,
                ObjName = ObjName
            };

            return result;
        }
    }
}