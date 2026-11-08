// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using CMiX.Core.BaseControls;
using CMiX.Core.Modulation;
using CMiX.Core.Prefabs;
using CMiX.Core.Prefabs.Managers;

namespace CMiX.Core.Texturing.Sources
{
    public record GradientMeshModel : IControlModel, IPrefabModel, ITextureSourceModel
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public PrefabServiceModel PrefabService { get; set; } = new();
        public PrefabManagerModel TextureModifierManager { get; set; } = new();
        public PrefabManagerModel ModulatorManager { get; set; } = new();
        public Integer2Model Resolution { get; set; } = new(512, 512);
        public GenericValueModel<bool> UseCompositionResolution { get; set; } = new(false);
        public ModulatableValueModel<float> Time { get; set; } = ModulatableValueModel<float>.Of("Time", 0.0f);
        public ModulatableValueModel<float> RotationSpeed { get; set; } = ModulatableValueModel<float>.Of("Rotation Speed", 0.1f);
        public GenericValueModel<string> Color1 { get; set; } = new("#FFF4CD9F");
        public GenericValueModel<string> Color2 { get; set; } = new("#FF3162EE");
        public GenericValueModel<string> Color3 { get; set; } = new("#FFE882CC");
        public GenericValueModel<string> Color4 { get; set; } = new("#FF59B5F3");
        public GenericValueModel<GradientMeshType> GradientType { get; set; } = new(GradientMeshType.Linear);
    }
}
