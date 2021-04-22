using System.IO;

namespace DBScriptSaver.Helpers
{
    public static class FileHelper
    {
        public static string CreateMigrationName(this string baseName)
        {
            string MigrationName = $@"Create_{baseName}";

            int postIndex = 0;
            string postFix = "";

            while (File.Exists(MigrationName + postFix))
            {
                postIndex++;
                postFix = $@"({postIndex})";
            }

            MigrationName += postFix;

            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                MigrationName = MigrationName.Replace(invalidChar, '_');
            }
            return MigrationName;
        }
    }
}
