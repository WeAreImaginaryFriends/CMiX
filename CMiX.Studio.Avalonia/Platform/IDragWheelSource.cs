// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;

namespace CMiX.Studio.Avalonia.Platform
{
    // Gives the mouse wheel steps during a drag. Windows needs a hook for this; other systems give nothing.
    public interface IDragWheelSource : IDisposable
    {
        // onWheel gets the wheel steps: positive when the wheel turns away from the user (scroll up).
        void Start(Action<int> onWheel);

        void Stop();

        bool IsActive { get; }
    }
}
