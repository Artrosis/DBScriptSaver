namespace DBScriptSaver.ViewModels
{
    public class Script
    {
        public string FileName;
        public string FullPath;
        public string ScriptText;
        public string ObjectType;
        public ChangeType ChangeState;
        public Migration Migration;
    }
}