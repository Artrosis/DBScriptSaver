using DBScriptSaver.ViewModels;

namespace DBScriptSaver.Core
{
    public class PGScript : BaseScript
    {
        public IPGObject PGObject;
        public PGScript(IPGObject obj)
        {
            PGObject = obj;
        }

        public new IScript Copy()
        {
            PGScript result = new PGScript(PGObject)
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