using DBScriptSaver.ViewModels;
using System.Collections.Generic;

namespace DBScriptSaver.Core
{
    internal class PGTableData : ITableData
    {
        public int id;
        public string Name;
        public string Schema;
        public string TableFolder;

        public List<PGColumnData> Columns = new List<PGColumnData>();
        public List<PGConstrainsData> Constrains = new List<PGConstrainsData>();
        public List<PGIndexData> Indexes = new List<PGIndexData>();
        public List<PGCommentData> Comments = new List<PGCommentData>();

        public IScript GetScript()
        {
            throw new System.NotImplementedException();
        }
    }
}