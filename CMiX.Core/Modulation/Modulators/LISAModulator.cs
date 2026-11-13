// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using CMiX.Core.BaseControls;
using CMiX.Core.Prefabs;

namespace CMiX.Core.Modulation.Modulators
{
    // The tracking values of the LISA system: a position and a count.
    public class LISAModulator : TrackingModulatorBase
    {
        public LISAModulator(PrefabService prefabService,
                             GenericValue<int> count,
                             GenericValue<float> x,
                             GenericValue<float> y,
                             FloatRange xRange,
                             FloatRange yRange)
            : base(prefabService, count, x, y, xRange, yRange)
        {
        }

        public override IControlModel ToModel()
        {
            var model = new LISAModulatorModel();
            FillBaseModel(model);
            return model;
        }

        public override void FromModel(IControlModel model)
        {
            ReadBaseModel((LISAModulatorModel)model);
        }
    }
}
