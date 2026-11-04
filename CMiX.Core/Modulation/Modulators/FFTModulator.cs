// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using CMiX.Core.BaseControls;
using CMiX.Core.Prefabs;

namespace CMiX.Core.Modulation.Modulators
{
    // The four bands of one FFT track, each mapped into its own range.
    public class FFTModulator : ModulatorBase
    {
        public FFTModulator(PrefabService prefabService,
                            GenericValue<int> trackindex,
                            FloatRange bassRange,
                            FloatRange lowerMidRange,
                            FloatRange higherMidRange,
                            FloatRange highRange)
            : base(prefabService)
        {
            TrackIndex = trackindex;
            BassRange = bassRange;
            LowerMidRange = lowerMidRange;
            HigherMidRange = higherMidRange;
            HighRange = highRange;
        }

        public override IReadOnlyList<IModulatorOutput> Outputs { get; } = new IModulatorOutput[]
        {
            new ModulatorOutput<float>("Bass"),
            new ModulatorOutput<float>("LowerMid"),
            new ModulatorOutput<float>("HigherMid"),
            new ModulatorOutput<float>("High"),
        };

        public GenericValue<int> TrackIndex { get; set; }
        public FloatRange BassRange { get; set; }
        public FloatRange LowerMidRange { get; set; }
        public FloatRange HigherMidRange { get; set; }
        public FloatRange HighRange { get; set; }

        public override IControlModel ToModel() => new FFTModulatorModel
        {
            ID = ID,
            PrefabService = (PrefabServiceModel)PrefabService.ToModel(),
            TrackIndex = (GenericValueModel<int>)TrackIndex.ToModel(),
            BassRange = (FloatRangeModel)BassRange.ToModel(),
            LowerMidRange = (FloatRangeModel)LowerMidRange.ToModel(),
            HigherMidRange = (FloatRangeModel)HigherMidRange.ToModel(),
            HighRange = (FloatRangeModel)HighRange.ToModel()
        };

        public override void FromModel(IControlModel model)
        {
            var m = (FFTModulatorModel)model;
            ID = m.ID;
            PrefabService.FromModel(m.PrefabService);
            TrackIndex.FromModel(m.TrackIndex);
            BassRange.FromModel(m.BassRange);
            LowerMidRange.FromModel(m.LowerMidRange);
            HigherMidRange.FromModel(m.HigherMidRange);
            HighRange.FromModel(m.HighRange);
        }
    }
}
