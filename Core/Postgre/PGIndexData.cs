using DBScriptSaver.ViewModels;
using System.Collections.Generic;
using System.IO;

namespace DBScriptSaver.Core
{
    public class PGIndexData : IIndexData, IPGObject
    {
        public string Script;
        public string IndexFolder;
        public string Name;
        public PGTableData table;

        public List<Migration> CreateAlterMirgrations(string oldScript)
        {
            return new List<Migration>();
        }

        public string CreateMirgration()
        {
            return Script.Replace("CREATE INDEX", "CREATE INDEX IF NOT EXISTS");
        }

        public IScript GetScript()
        {
            string fileName = $@"{table.Schema}.{table.Name}.{Name}.sql";
            string indexFileName = IndexFolder + fileName;

            if (File.Exists(indexFileName) && Script == File.ReadAllText(indexFileName))
            {
                return null;
            }
            ChangeType ChangeType = !File.Exists(indexFileName) ? ChangeType.Новый : ChangeType.Изменённый;
            return new PGScript(this)
            {
                FileName = fileName,
                FullPath = IndexFolder + fileName,
                ScriptText = Script,
                ObjectType = @"Индекс",
                ChangeState = ChangeType,
                ObjName = Name
            };
        }
    }
}