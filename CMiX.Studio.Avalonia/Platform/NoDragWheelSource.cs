// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;

namespace CMiX.Studio.Avalonia.Platform
{
    // Gives no wheel steps. For the systems that are not Windows.
    public sealed class NoDragWheelSource : IDragWheelSource
    {
        public bool IsActive => false;

        public void Start(Action<int> onWheel)
        {
        }

        public void Stop()
        {
        }

        public void Dispose()
        {
        }
    }
}
