// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using CMiX.Core.Animations;
using CMiX.Core.BaseControls;
using CMiX.Core.Prefabs;

namespace CMiX.Core.Modulation.Modulators
{
    // One Redis address of the project as a float output, mapped into a range.
    public class RedisFloatModulator : RedisModulatorBase<float>
    {
        public RedisFloatModulator(PrefabService prefabService, RedisAddressRegistry registry,
                                   GenericValue<Guid?> selectedAddressID, FloatRange range)
            : base(prefabService, registry, selectedAddressID)
        {
            Range = range;
        }

        public FloatRange Range { get; set; }

        public override IControlModel ToModel()
        {
            var model = new RedisFloatModulatorModel { Range = (FloatRangeModel)Range.ToModel() };
            FillBaseModel(model);
            return model;
        }

        public override void FromModel(IControlModel model)
        {
            var m = (RedisFloatModulatorModel)model;
            ReadBaseModel(m);
            Range.FromModel(m.Range);
        }
    }
}
