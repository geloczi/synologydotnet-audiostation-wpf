namespace SynAudio.Utils
{
    public static class WindowsHelper
    {
        public static void HighlightFileInFileExplorer(string path)
        {
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
        }
    }
}
