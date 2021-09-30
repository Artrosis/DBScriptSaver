using DBScriptSaver.ViewModels;
using System.IO;

namespace DBScriptSaver.Core
{
    internal class PGIndexData : IIndexData
    {
        public string Script;
        public string IndexFolder;
        public string Name;
        public PGTableData table;

        public IScript GetScript()
        {
            string fileName = $@"{table.Schema}.{table.Name}.{Name}.sql";
            string indexFileName = IndexFolder + fileName;

            if (File.Exists(indexFileName) && Script == File.ReadAllText(indexFileName))
            {
                return null;
            }
            ChangeType ChangeType = !File.Exists(indexFileName) ? ChangeType.Новый : ChangeType.Изменённый;
            return new PGScript()
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