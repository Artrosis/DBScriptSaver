using DBScriptSaver.ViewModels;
using System.Collections.Generic;
using System.Data.Common;

namespace DBScriptSaver.Core
{
    internal class PGMigrationMaker : IMigrationMaker
    {
        private readonly DbConnection dbConnection;
        private readonly PGScript script;

        public PGMigrationMaker(DbConnection dbConnection, PGScript script)
        {
            this.dbConnection = dbConnection;
            this.script = script;
        }

        public List<Migration> Make()
        {
            throw new System.NotImplementedException();
        }
    }
}