// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

namespace CMiX.Core.Modulation.Modulators
{
    // Where the values of a modulator come from. The studio gives each category its own colors.
    public enum ModulatorCategory
    {
        // The modulator makes its values itself.
        Internal,

        // The modulator reads its values from outside, for example from Redis.
        External
    }
}
