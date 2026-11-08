// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using CMiX.Core.BaseControls;
using CMiX.Core.Modulation;
using CMiX.Core.Prefabs;
using CMiX.Core.Prefabs.Managers;

namespace CMiX.Core.Texturing.Sources
{
    public record GradientModel : IControlModel, IPrefabModel, ITextureSourceModel
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public PrefabServiceModel PrefabService { get; set; } = new();
        public PrefabManagerModel TextureModifierManager { get; set; } = new();
        public PrefabManagerModel ModulatorManager { get; set; } = new();
        public Integer2Model Resolution { get; set; } = new(512, 512);
        public GenericValueModel<bool> UseCompositionResolution { get; set; } = new(false);
        public ModulatableValueModel<float> Gamma { get; set; } = ModulatableValueModel<float>.Of("Gamma", 2.2f);
        public GenericValueModel<string> From { get; set; } = new("#FFFFFFFF");
        public GenericValueModel<string> To { get; set; } = new("#FF000000");
        public GenericValueModel<bool> Horizontal { get; set; } = new(false);
    }
}
