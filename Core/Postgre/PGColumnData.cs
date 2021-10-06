namespace DBScriptSaver.Core
{
    public class PGColumnData
    {
        public string Name;
        public string Script;

        public override string ToString()
        {
            return Name;
        }
    }
}