using System.IO;

namespace RuntimeTerrainEditor
{
    public static class FileUtility
    {
        public static void SaveToPath(string path, byte[] bytes)
        {
            File.WriteAllBytes(path, bytes);
        }

        public static byte[] LoadFromPath(string path)
        {
            return File.ReadAllBytes(path);
        }

        public static FileInfo[] ReadFilesFromDirectory(string dir, string pattern)
        {
            DirectoryInfo di = new DirectoryInfo(dir);
            return di.GetFiles(pattern);
        }

        public static void DeleteFileAtPath(string path)
        {
            File.Delete(path);
        }

        public static string GetFileName(FileInfo info)
        {
            return Path.GetFileNameWithoutExtension(info.FullName);
        }
    }
}