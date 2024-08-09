﻿namespace Kitty.UI.Dialogs
{
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public class MultiSelection : IEnumerable<string>, IReadOnlyList<string>
    {
        private readonly List<string> selection = [];
        private bool allowMultipleSelection = false;
        private string selectionString = string.Empty;
        private string? rootPath;

        public bool AllowMultipleSelection
        {
            get => allowMultipleSelection;
            set => allowMultipleSelection = value;
        }

        public string? RootPath
        {
            get => rootPath;
            set
            {
                rootPath = value;
                Clear(); // selection is no longer valid
            }
        }

        public int Count => selection.Count;

        public string this[int index] => selection[index];

        public string SelectionString
        {
            get => selectionString;
        }

        [Flags]
        public enum ValidationOptions
        {
            None = 0,
            MustExist = 1,
            AllowFiles = 2,
            AllowFolders = 4,
        }

        public void SetSelectionString(string value, ValidationOptions options)
        {
            selectionString = value;
            selection.Clear();
            var paths = value.Split(';');
            if (paths.Length == 0)
            {
                return;
            }

            bool mustExist = (options & ValidationOptions.MustExist) != 0;
            bool allowFiles = (options & ValidationOptions.AllowFiles) != 0;
            bool allowFolders = (options & ValidationOptions.AllowFolders) != 0;
            bool allowAny = allowFiles && allowFolders;

            for (int i = 0; i < paths.Length; i++)
            {
                var path = paths[i];

                if (RootPath != null)
                {
                    path = Path.Combine(RootPath, path);
                }

                bool isFile = File.Exists(path);
                bool isFolder = Directory.Exists(path);
                bool exists = isFile || isFolder;

                if (mustExist)
                {
                    if ((!allowAny || !exists) && (!allowFiles || !isFile) && (!allowFolders || !isFolder))
                    {
                        Remove(ref i);
                        continue;
                    }
                }
                else
                {
                    if ((!allowAny || !exists) && (!allowFiles || !isFile) && (!allowFolders || !isFolder) && !PathValidator.IsValidPath(path))
                    {
                        Remove(ref i);
                        continue;
                    }
                }

                selection.Add(path);
                if (!allowMultipleSelection)
                {
                    break;
                }
            }

            void Remove(ref int i)
            {
                selection.RemoveAt(i);
                i--;
            }
        }

        public void Validate(ValidationOptions options)
        {
            bool mustExist = (options & ValidationOptions.MustExist) != 0;
            bool allowFiles = (options & ValidationOptions.AllowFiles) != 0;
            bool allowFolders = (options & ValidationOptions.AllowFolders) != 0;
            bool allowAny = allowFiles && allowFolders;

            StringBuilder sb = new();
            for (int i = 0; i < selection.Count; i++)
            {
                var path = selection[i];

                bool isFile = File.Exists(path);
                bool isFolder = Directory.Exists(path);
                bool exists = isFile || isFolder;

                if (mustExist)
                {
                    if ((!allowAny || !exists) && (!allowFiles || !isFile) && (!allowFolders || !isFolder))
                    {
                        Remove(ref i);
                        continue;
                    }
                }
                else
                {
                    if ((!allowAny || !exists) && (!allowFiles || !isFile) && (!allowFolders || !isFolder) && !PathValidator.IsValidPath(path))
                    {
                        Remove(ref i);
                        continue;
                    }
                }

                ReadOnlySpan<char> relative = GetRelativePath(rootPath, path);

                if (sb.Length > 0)
                {
                    sb.Append($";{relative}");
                }
                else
                {
                    sb.Append(relative);
                }
            }
            selectionString = sb.ToString();

            void Remove(ref int i)
            {
                selection.RemoveAt(i);
                i--;
            }
        }

        public void Clear()
        {
            selection.Clear();
            selectionString = string.Empty;
        }

        public void Add(string item)
        {
            ReadOnlySpan<char> relative = GetRelativePath(rootPath, item);

            if (!allowMultipleSelection)
            {
                selection.Clear();
                selectionString = relative.ToString();
            }
            else
            {
                if (selection.Count > 0)
                {
                    selectionString += $";{relative}";
                }
                else
                {
                    selectionString = relative.ToString();
                }
            }

            if (selection.Count == 0 || !selection.Contains(item))
            {
                selection.Add(item);
            }
        }

        private static ReadOnlySpan<char> GetRelativePath(ReadOnlySpan<char> relativeTo, ReadOnlySpan<char> path)
        {
            if (path.StartsWith(relativeTo, StringComparison.Ordinal))
            {
                return path[relativeTo.Length..].TrimStart(Path.DirectorySeparatorChar).TrimStart(Path.AltDirectorySeparatorChar);
            }
            return path;
        }

        public void Remove(string item)
        {
            int index = selection.IndexOf(item);
            selection.RemoveAt(index);

            ReadOnlySpan<char> relative = GetRelativePath(rootPath, item);

            int stringIndex = selectionString.AsSpan().IndexOf(relative);
            if (index > 0)
            {
                selectionString = selectionString.Remove(stringIndex - 1, relative.Length + 1);
            }
            else
            {
                selectionString = selectionString.Remove(stringIndex, relative.Length);
            }
        }

        public bool Contains(string item)
        {
            return selection.Contains(item);
        }

        public IEnumerable<string> GetSelection()
        {
            return selection;
        }

        public IEnumerator<string> GetEnumerator()
        {
            return selection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return selection.GetEnumerator();
        }
    }
}