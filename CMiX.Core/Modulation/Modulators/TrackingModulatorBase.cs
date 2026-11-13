// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using CMiX.Core.BaseControls;
using CMiX.Core.Prefabs;

namespace CMiX.Core.Modulation.Modulators
{
    // A modulator that gives the position and the count of a tracking source. Each value has a range.
    [ModulatorCategory(ModulatorCategory.External)]
    public abstract class TrackingModulatorBase : ModulatorBase
    {
        protected TrackingModulatorBase(PrefabService prefabService,
                                        GenericValue<int> count,
                                        GenericValue<float> x,
                                        GenericValue<float> y,
                                        FloatRange xRange,
                                        FloatRange yRange)
            : base(prefabService)
        {
            Count = count;
            X = x;
            Y = y;
            XRange = xRange;
            YRange = yRange;
        }

        public override IReadOnlyList<IModulatorOutput> Outputs { get; } = new IModulatorOutput[]
        {
            new ModulatorOutput<int>("Count"),
            new ModulatorOutput<float>("X"),
            new ModulatorOutput<float>("Y")
        };

        public GenericValue<int> Count { get; set; }
        public GenericValue<float> X { get; set; }
        public GenericValue<float> Y { get; set; }
        public FloatRange XRange { get; set; }
        public FloatRange YRange { get; set; }

        protected void FillBaseModel(TrackingModulatorModelBase model)
        {
            model.ID = ID;
            model.PrefabService = (PrefabServiceModel)PrefabService.ToModel();
            model.Count = (GenericValueModel<int>)Count.ToModel();
            model.X = (GenericValueModel<float>)X.ToModel();
            model.Y = (GenericValueModel<float>)Y.ToModel();
            model.XRange = (FloatRangeModel)XRange.ToModel();
            model.YRange = (FloatRangeModel)YRange.ToModel();
        }

        protected void ReadBaseModel(TrackingModulatorModelBase model)
        {
            ID = model.ID;
            PrefabService.FromModel(model.PrefabService);
            Count.FromModel(model.Count);
            X.FromModel(model.X);
            Y.FromModel(model.Y);
            XRange.FromModel(model.XRange);
            YRange.FromModel(model.YRange);
        }
    }
}
