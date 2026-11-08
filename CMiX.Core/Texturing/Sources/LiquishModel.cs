// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using CMiX.Core.BaseControls;
using CMiX.Core.Modulation;
using CMiX.Core.Prefabs;
using CMiX.Core.Prefabs.Managers;

namespace CMiX.Core.Texturing.Sources
{
    public record LiquishModel : IControlModel, IPrefabModel, ITextureSourceModel
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public PrefabServiceModel PrefabService { get; set; } = new();
        public PrefabManagerModel TextureModifierManager { get; set; } = new();
        public PrefabManagerModel ModulatorManager { get; set; } = new();
        public Integer2Model Resolution { get; set; } = new(512, 512);
        public GenericValueModel<bool> UseCompositionResolution { get; set; } = new(false);
        public ModulatableValueModel<float> Time { get; set; } = ModulatableValueModel<float>.Of("Time", 0.0f);
        public ModulatableVector2Model Scale { get; set; } = ModulatableVector2Model.Of(5f, 5f);
        public GenericValueModel<string> ColorA { get; set; } = new("#FF000000");
        public GenericValueModel<string> ColorB { get; set; } = new("#FFFFFFFF");
        public GenericValueModel<string> ColorC { get; set; } = new("#FF800026");
    }
}
