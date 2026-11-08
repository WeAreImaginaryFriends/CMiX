// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using CMiX.Core.BaseControls;
using CMiX.Core.Modulation;
using CMiX.Core.Prefabs;
using CMiX.Core.Prefabs.Managers;

namespace CMiX.Core.Texturing.Filters
{
    public record PulseModel : IPrefabModel, ITextureFilterModel
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public PrefabServiceModel PrefabService { get; set; } = new();
        public BlendModel Blend { get; set; } = new();
        public ModulatableValueModel<float> Center { get; set; } = ModulatableValueModel<float>.Of("Center", 1.0f);
        public ModulatableValueModel<float> SlopeWidth { get; set; } = ModulatableValueModel<float>.Of("Slope Width", 1.0f);
        public ModulatableValueModel<float> CenterWidth { get; set; } = ModulatableValueModel<float>.Of("Center Width", 1.0f);
        public GenericValueModel<bool> Invert { get; set; } = new(false);
        public GenericValueModel<PulseChannel> Channel { get; set; } = new(PulseChannel.Red);
        public PrefabManagerModel ModulatorManager { get; set; } = new();
    }
}
