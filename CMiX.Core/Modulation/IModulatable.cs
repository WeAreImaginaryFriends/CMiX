// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using CMiX.Core.Prefabs.Managers;

namespace CMiX.Core.Modulation
{
    // An item that owns a list of modulators: a modifier, a texture filter or a texture source.
    public interface IModulatable
    {
        PrefabManager ModulatorManager { get; }
    }
}
