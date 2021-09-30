using DBScriptSaver.ViewModels;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using System.IO;

namespace DBScriptSaver.Core
{
    public class MSSQLScript : BaseScript
    {
        public Urn urn;      

        public new IScript Copy()
        {
            MSSQLScript result = new MSSQLScript()
            {
                FileName = FileName,
                FullPath = FullPath,
                ScriptText = ScriptText,
                ChangeState = ChangeState,
                ObjectType = ObjectType,
                urn = urn,
                ObjName = ObjName
            };
            
            return result;
        }
    }
}