using System;
using System.IO;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Core.Utilities;
using Core;
using Core.UIStructure.Utilities;

namespace Runtime.Explorer.SessionViewer
{
    public class SessionFilerManager : MonoBehaviour
    {
        public event Action<string> SelectFile;

        [SerializeField] private ButtonItemPointer prefabDirectoryButton;
        [SerializeField] private ButtonItemPointer prefabFileButton;

        [Space(10)]
        [Header("Elements for creating directorys.")]
        [SerializeField] private Button newDirectoryButton;
        [SerializeField] private InputField nameDirectory;

        [Space(10)]
        [SerializeField] private Transform content;


        private FileManager fileManager;

        private ItemPath selectFile;

        private LinkedList<ItemPath> items;

        private string currentPath;

        private enum PathType
        {
            Directory = 0,
            File = 1,
        }
        private class PathWrite
        {
            public string name;
            public string path;
            public PathType type;
        }

        private class ItemPath
        {
            public PathWrite path;
            public ButtonItemPointer button;

            public ItemPath(PathWrite path, ButtonItemPointer button)
            {
                this.path = path;
                this.button = button;
            }
        }

        private void InitFileManager()
        {
            items = new LinkedList<ItemPath>();
            fileManager = new FileManager();
        }

        private void Awake()
        {
            InitFileManager();
            if (newDirectoryButton != null && nameDirectory != null)
                newDirectoryButton.onClick.AddListener(CreateNewDirectory);

        }

        public void SetStartPath(string path)
        {
            currentPath = path;
            fileManager.SetStartPath(path);
            fileManager.UpdateDirectory();
        }



        public void UpdateFileManager()
        {
            ClearItemsPaths();
            GenerateFromPath(fileManager.GetPaths(currentPath));
        }


        private void CallDirectoryBack()
        {
            ClearItemsPaths();
            string newPath = Directory.GetParent(currentPath).FullName;
            currentPath = newPath;
            GenerateFromPath(fileManager.GetPaths(newPath));
        }

        private void CallDirectoryOpen(ItemPath item)
        {
            ClearItemsPaths();
            currentPath = item.path.path;
            GenerateFromPath(fileManager.GetPaths(item.path.path));

        }

        private void CallFileOpen(ItemPath item)
        {
            SelectFile?.Invoke(item.path.path);
        }

        private void CreateNewDirectory()
        {
            if (nameDirectory.text != "")
            {
                ClearItemsPaths();
                GenerateFromPath(fileManager.CreateDirectoryAndUpdate(currentPath, nameDirectory.text));
            }
            nameDirectory.text = "";
        }

        public string GetCurrentPath()
        {
            return currentPath;
        }

        public string GetCurrentPathFile()
        {
            return selectFile.path.path;
        }

        private void GenerateFromPath(LinkedList<PathWrite> paths)
        {
            if (currentPath != fileManager.GetStartPath())
            {
                items.AddLast(CreateItem(
                    new PathWrite() { name = "\\...", path = "", type = PathType.Directory },
                    (System.Action)(() => { CallDirectoryBack(); })
                    ));
            }
            foreach (PathWrite path in paths)
            {
                if (path.type == PathType.Directory)
                {
                    ItemPath item = CreateItem(
                    path,
                    (System.Action)(() => { })
                    );
                    item.button.SetVisual((System.Action)(() => { CallDirectoryOpen(item); }));
                    items.AddLast(item);
                }
                else if (path.type == PathType.File)
                {

                    ItemPath item = CreateItem(
                    path,
                    (System.Action)(() => { })
                    );
                    item.button.SetVisual((System.Action)(() => { CallFileOpen(item); }));
                    items.AddLast(item);
                }
            }

        }



        private ItemPath CreateItem(PathWrite path, Action action)
        {
            ButtonItemPointer itemB = null;
            if (path.type == PathType.Directory)
            {
                itemB = DynamicPool.Instance.Get(prefabDirectoryButton, content);
            }
            else
            {
                itemB = DynamicPool.Instance.Get(prefabFileButton, content);
            }
            ItemPath item = new ItemPath(path, itemB);
            item.button.SetVisual(action, path.name);
            return item;
        }

        private void ClearItemsPaths()
        {
            foreach (ItemPath item in items)
            {
                item.button.SetVisual((System.Action)(() => { }));
                DynamicPool.Instance.Return(item.button);
            }
            items.Clear();
            selectFile = null;
        }

        private class FileManager
        {
            private string cashStartPath = "";

            public void UpdateDirectory()
            {
                string path = GetStartPath();
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }

            public void SetStartPath(string path)
            {
                cashStartPath = path;
            }

            public string GetStartPath()
            {
                if (cashStartPath != "")
                    return cashStartPath;
                else
                    throw new NullReferenceException();
            }

            public LinkedList<PathWrite> GetBackPaths(string toPath)
            {
                return GetPaths(Directory.GetParent(toPath).FullName);
            }

            public LinkedList<PathWrite> GetPaths(string currentPath)
            {
                LinkedList<PathWrite> paths = new LinkedList<PathWrite>();
                string[] directories = Directory.GetDirectories(currentPath);
                for (int i = 0; i < directories.Length; i++)
                {
                    PathWrite w = new PathWrite() { name = "\\" + Path.GetFileName(directories[i]), path = directories[i], type = PathType.Directory };
                    paths.AddLast(w);
                }
                string[] files = Directory.GetFiles(currentPath, "*." + PathStorage.SESSION_TYPE_FILE);
                for (int i = 0; i < files.Length; i++)
                {
                    PathWrite w = new PathWrite() { name = Path.GetFileName(files[i]), path = files[i], type = PathType.File };
                    paths.AddLast(w);
                }
                return paths;
            }

            public LinkedList<PathWrite> CreateDirectoryAndUpdate(string currentPath, string name)
            {
                string path = currentPath + "\\" + name;
                Directory.CreateDirectory(path);
                return GetPaths(currentPath);
            }
        }
    }
}