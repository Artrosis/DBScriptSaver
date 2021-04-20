﻿using Microsoft.SqlServer.Management.SqlParser.Parser;
using System;

namespace DBScriptSaver.ViewModels
{
    public class MSSQL_Token_JS
    {
        public readonly string SourceSQL;
        public readonly Tokens Value;
        public readonly string Text;
        public readonly int ScannerState;
        public readonly int Start;
        public readonly int End;
        public readonly bool IsPairMatch;
        public readonly bool IsExecAutoParamHelp;

        private MSSQL_Token_JS(string SourceSQL, int tokenId, int ScannerState, int Start, int End, bool IsPairMatch, bool IsExecAutoParamHelp)
        {
            this.SourceSQL = SourceSQL;
            this.Value = (Tokens)tokenId;
            if (this.Value != Tokens.EOF)
            {
                this.Text = SourceSQL.Substring(Start, End - Start + 1);
            }

            this.ScannerState = ScannerState;
            this.Start = Start;
            this.End = End;
            this.IsPairMatch = IsPairMatch;
            this.IsExecAutoParamHelp = IsExecAutoParamHelp;
        }

        public static MSSQL_Token_JS GetNext(Scanner scanner, string SourceSQL, ref int ScannerState)
        {
            int start, end;
            bool isPairMatch, isExecAutoParamHelp;
            int tokenId = scanner.GetNext(ref ScannerState, out start, out end, out isPairMatch, out isExecAutoParamHelp);
            return new MSSQL_Token_JS(SourceSQL, tokenId, ScannerState, start, end, isPairMatch, isExecAutoParamHelp);
        }

        public override string ToString()
        {
            return String.Format("{0}:{1}", this.Value, this.Text);
        }
    }
}