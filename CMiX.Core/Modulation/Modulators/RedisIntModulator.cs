// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using CMiX.Core.BaseControls;
using CMiX.Core.Prefabs;

namespace CMiX.Core.Modulation.Modulators
{
    // One Redis address of the project as an int output.
    public class RedisIntModulator : RedisModulatorBase<int>
    {
        public RedisIntModulator(PrefabService prefabService, RedisAddressRegistry registry,
                                 GenericValue<Guid?> selectedAddressID)
            : base(prefabService, registry, selectedAddressID)
        {
        }

        public override IControlModel ToModel()
        {
            var model = new RedisIntModulatorModel();
            FillBaseModel(model);
            return model;
        }

        public override void FromModel(IControlModel model) => ReadBaseModel((RedisIntModulatorModel)model);
    }
}
