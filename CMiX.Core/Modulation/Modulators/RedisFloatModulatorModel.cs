// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System.Text.Json.Serialization;
using CMiX.Core.BaseControls;

namespace CMiX.Core.Modulation.Modulators
{
    public record RedisFloatModulatorModel : RedisModulatorModelBase
    {
        // Range stays the last key in a project file, after the keys of the base.
        [JsonPropertyOrder(1)]
        public FloatRangeModel Range { get; set; } = new(0.0f, 1.0f);
    }
}
