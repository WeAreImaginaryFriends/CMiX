// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using CMiX.Core.BaseControls;
using CMiX.Core.Prefabs;

namespace CMiX.Core.Modulation.Modulators
{
    // One Redis address of the project as a bool output.
    public class RedisBoolModulator : RedisModulatorBase<bool>
    {
        public RedisBoolModulator(PrefabService prefabService, RedisAddressRegistry registry,
                                  GenericValue<Guid?> selectedAddressID)
            : base(prefabService, registry, selectedAddressID)
        {
        }

        public override IControlModel ToModel()
        {
            var model = new RedisBoolModulatorModel();
            FillBaseModel(model);
            return model;
        }

        public override void FromModel(IControlModel model) => ReadBaseModel((RedisBoolModulatorModel)model);
    }
}
