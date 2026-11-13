// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using CMiX.Core.BaseControls;
using CMiX.Core.Prefabs;

namespace CMiX.Core.Modulation.Modulators
{
    public class TrackingModulator : TrackingModulatorBase
    {
        public TrackingModulator(PrefabService prefabService,
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
            var model = new TrackingModulatorModel();
            FillBaseModel(model);
            return model;
        }

        public override void FromModel(IControlModel model)
        {
            ReadBaseModel((TrackingModulatorModel)model);
        }
    }
}
