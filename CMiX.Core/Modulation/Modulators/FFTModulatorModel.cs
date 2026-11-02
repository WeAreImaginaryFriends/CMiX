// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using CMiX.Core.BaseControls;
using CMiX.Core.Prefabs;

namespace CMiX.Core.Modulation.Modulators
{
    public record FFTModulatorModel : IPrefabModel
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public PrefabServiceModel PrefabService { get; set; } = new();
        public GenericValueModel<int> TrackIndex { get; set; } = new(0);
        public FloatRangeModel BassRange { get; set; } = new(0.0f, 1.0f);
        public FloatRangeModel LowerMidRange { get; set; } = new(0.0f, 1.0f);
        public FloatRangeModel HigherMidRange { get; set; } = new(0.0f, 1.0f);
        public FloatRangeModel HighRange { get; set; } = new(0.0f, 1.0f);
    }
}
