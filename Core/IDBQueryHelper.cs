using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBScriptSaver.Core
{
    public interface IDBQueryHelper
    {
        DbConnection GetConnection(string dBName = null);
        string GetDataBaseQuery();
        bool CheckConnection();
    }
}
