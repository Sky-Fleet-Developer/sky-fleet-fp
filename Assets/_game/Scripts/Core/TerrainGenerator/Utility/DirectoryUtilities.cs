using System.IO;

namespace Core.TerrainGenerator.Utility
{
    public static class DirectoryUtilities
    {
        public static DirectoryInfo GetDirectory(string directoryName)
        {
            string[] directories = Directory.GetDirectories(PathStorage.GetPathToLandscapesDirectory());
            foreach (string t in directories)
            {
                DirectoryInfo info = new DirectoryInfo(t);
                if (info.Name == directoryName)
                {
                    return info;
                }
            }

            return null;
        }
    }
}
