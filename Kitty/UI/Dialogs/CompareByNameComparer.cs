﻿using Kitty.UI.Dialogs;

namespace Hexa.NET.Kitty.UI.Dialogs
{
    public readonly struct CompareByNameComparer : IComparer<FileSystemItem>
    {
        public int Compare(FileSystemItem a, FileSystemItem b)
        {
            int cmp = FileSystemItem.CompareByBase(a, b);
            if (cmp != 0)
            {
                return cmp;
            }
            return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
        }
    }
}