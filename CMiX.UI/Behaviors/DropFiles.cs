// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Input;
using Avalonia.Platform.Storage;

namespace CMiX.UI.Behaviors
{
    // The paths of one drag, classified once. DragOver reads this record and does not read the disk.
    // Data is the data object of the drag. A drag with other data is a new drag.
    public sealed record DropFiles(IDataObject Data, IReadOnlyList<string> Paths, IReadOnlyList<string> Accepted)
    {
        public bool IsAccepted => Accepted.Count > 0;

        // Reads the local paths of the drag that fit the mode, and keeps the paths that accepts takes.
        public static DropFiles Classify(IDataObject data, FileDropMode mode, Func<string, bool> accepts)
        {
            var paths = LocalPaths(data, mode);
            return new DropFiles(data, paths, paths.Where(accepts).ToList());
        }

        // The local paths of the files and folders of the drag that fit the mode.
        public static IReadOnlyList<string> LocalPaths(IDataObject data, FileDropMode mode) =>
            (data.GetFiles() ?? Enumerable.Empty<IStorageItem>())
                .Select(item => item.TryGetLocalPath())
                .Where(path => path != null && FitsMode(mode, path))
                .Select(path => path!)
                .ToList();

        private static bool FitsMode(FileDropMode mode, string path) => mode switch
        {
            FileDropMode.Folders => Directory.Exists(path),
            FileDropMode.FilesAndFolders => File.Exists(path) || Directory.Exists(path),
            _ => File.Exists(path)
        };

        // True when the pointer of the leave is out of the control. A leave of a child inside the control is false.
        public static bool LeavesControl(Visual control, DragEventArgs e)
        {
            var point = e.GetPosition(control);
            return !new Rect(control.Bounds.Size).Contains(point);
        }
    }
}
