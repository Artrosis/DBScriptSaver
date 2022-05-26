using System.IO;

namespace DBScriptSaver.Helpers
{
    public static class FileHelper
    {
        public static string CreateMigrationName(this string baseName)
        {
            string totalFileName = $@"Create_{baseName}";

            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                totalFileName = totalFileName.Replace(invalidChar, '_');
            }
            return totalFileName;
        }
    }
}
