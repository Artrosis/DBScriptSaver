using DBScriptSaver.ViewModels;
using System;

namespace DBScriptSaver.Core
{
    public interface IScriptWriter : IDisposable
    {
        Action<string, int> changeProgress { get; set; }
        Action<Script> observer { get; set; }

        void ObserveScripts();
    }
}