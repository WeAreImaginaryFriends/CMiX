// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using CMiX.Core.BaseControls;
using CMiX.Core.Prefabs;

namespace CMiX.Core.Modulation.Modulators
{
    public record IntegratorModulatorModel : IPrefabModel
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public PrefabServiceModel PrefabService { get; set; } = new();
        public GenericValueModel<float> Offset { get; set; } = new(0.0f);
        public GenericValueModel<bool> Step { get; set; } = new(true);
        public GenericValueModel<float> ResetValue { get; set; } = new(0.0f);
        public ButtonModel Reset { get; set; } = new();
    }
}
