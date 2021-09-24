using DBScriptSaver.ViewModels;
using System;

namespace DBScriptSaver.Core
{
    internal class PGScriptWriter : IScriptWriter
    {
        private ProjectDataBase projectDataBase;

        public PGScriptWriter(ProjectDataBase projectDataBase)
        {
            this.projectDataBase = projectDataBase;
        }

        private Action<string, int> _changeProgress;
        public Action<string, int> changeProgress
        {
            get => _changeProgress;
            set => _changeProgress = value;
        }

        private Action<Script> _observer;
        public Action<Script> observer
        {
            get => _observer;
            set => _observer = value;
        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public void ObserveScripts()
        {
            throw new NotImplementedException();
        }
    }
}