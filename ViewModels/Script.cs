using DBScriptSaver.Helpers;
using DBScriptSaver.Parse;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.IO;

namespace DBScriptSaver.ViewModels
{
    public interface IScript
    {
        string FileName { get; }

        IScript Copy();

        string FullPath { get; }
        string ScriptText { get; set; }
        string ObjectType { get; }
        ChangeType ChangeState { get; }
        string ObjName { get; }
    }
}