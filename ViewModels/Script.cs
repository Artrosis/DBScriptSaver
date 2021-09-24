using DBScriptSaver.Helpers;
using DBScriptSaver.Parse;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.IO;

namespace DBScriptSaver.ViewModels
{
    public class Script
    {
        public string FileName;
        public string FullPath;
        public string ScriptText;
        public string ObjectType;
        public ChangeType ChangeState;
        public Urn urn;
        public string objName;
    }
}