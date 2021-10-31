using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Core.TerrainGenerator
{
    [System.Serializable]
    public class FileFormatSeeker
    {
        public string format = "Name_{x}_{y}";
        public string extension = "png";

        public FileFormatSeeker()
        {
            
        }
        public FileFormatSeeker(FileFormatSeeker clone)
        {
            format = clone.format;
            extension = clone.extension;
        }

        public string SearchInFolder(Vector2Int position, string path)
        {
            string f = format.Replace("{x}", "{0}").Replace("{y}", "{1}");
            if (path[path.Length - 1] != '/' && path[path.Length - 1] != '\\') path = path + "/";
            string p = path + string.Format(f, position.x, position.y) + "." + extension;
            if (File.Exists(p)) return p;
            return null;
        }
        
        public Dictionary<Vector2Int, string> SearchInFolder(string path)
        {
            string f = format.Replace("{x}", "{0}").Replace("{y}", "{1}");
            if (path[path.Length - 1] != '/' && path[path.Length - 1] != '\\') path = path + "/";
            Dictionary<Vector2Int, string> result = new Dictionary<Vector2Int, string>();

            int x = 0;
            int y = 0;
            //positive x
            while (true)
            {
                int add = 0;
                //positive y
                while (true)
                {
                    string p = path + string.Format(f, x, y) + "." + extension;

                    if (File.Exists(p))
                    {
                        result.Add(new Vector2Int(x, y), p);
                        add++;
                        y++;
                    }else break;
                }

                y = -1;
            
                //negative y
                while (true)
                {
                    string p = path + string.Format(f, x, y) + "." + extension;

                    if (File.Exists(p))
                    {
                        result.Add(new Vector2Int(x, y), p);
                        add++;
                        y--;
                    }else break;
                }

                if (add == 0) break;
                y = 0;
                x++;
            }

            x = -1;
        
            //negative x
            while (true)
            {
                int add = 0;
                //positive y
                while (true)
                {
                    string p = path + string.Format(f, x, y) + "." + extension;

                    if (File.Exists(p))
                    {
                        result.Add(new Vector2Int(x, y), p);
                        add++;
                        y++;
                    }else break;
                }

                y = -1;
            
                //negative y
                while (true)
                {
                    string p = path + string.Format(f, x, y) + "." + extension;

                    if (File.Exists(p))
                    {
                        result.Add(new Vector2Int(x, y), p);
                        add++;
                        y--;
                    }else break;
                }

                if (add == 0) break;
                y = 0;
                x--;
            }

            return result;
        }
    }
}
