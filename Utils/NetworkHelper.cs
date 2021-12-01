using System;

namespace Utils
{
    public static class NetworkHelper
    {
        public static string GetUncPath(string musicFolderPath, string internalPath)
        {
            if (string.IsNullOrEmpty(musicFolderPath))
                throw new ArgumentException(nameof(musicFolderPath));
            if (string.IsNullOrEmpty(internalPath))
                throw new ArgumentException(nameof(internalPath));

            var relativePath = internalPath.Replace("/", "\\");
            if (!relativePath.StartsWith("\\"))
                relativePath = "\\" + relativePath;
            var uncPath = @"\\" + musicFolderPath.Trim('\\').Split('\\')[0] + relativePath;
            return uncPath;
        }
    }
}
