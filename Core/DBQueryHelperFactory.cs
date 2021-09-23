using System;

namespace DBScriptSaver.Core
{
    public class DBQueryHelperFactory
    {
        public static IDBQueryHelper Create(DBType type, string path, string login, string pass)
        {
            if (type == DBType.PostgreSql)
            {
                return new PGQueryHelper(path, login, pass);
            }
            else if (type == DBType.MSSql)
            {
                return new MSSQLQueryHelper(path, login, pass);
            }
            else
            {
                throw new Exception($@"Неизвестный тип базы данных: {type}");
            }
        }
    }
}
