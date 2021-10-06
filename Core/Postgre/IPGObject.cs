using DBScriptSaver.ViewModels;
using System.Collections.Generic;

namespace DBScriptSaver.Core
{
    public interface IPGObject
    {
        string CreateMirgration();
        List<Migration> CreateAlterMirgrations(string oldScript);
    }
}