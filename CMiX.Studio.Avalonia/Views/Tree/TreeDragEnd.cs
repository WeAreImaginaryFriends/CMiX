// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

namespace CMiX.Studio.Avalonia.Views
{
    // How a drag of a row ends (see TreeDragSession.End).
    public enum TreeDragEnd
    {
        // The row goes to the marked place. Nothing moves when no place is marked.
        Drop,

        // The row stays where it is.
        Cancel
    }
}
