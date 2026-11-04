// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using CMiX.Core.Compositing;

namespace CMiX.Studio.Avalonia.ViewModels.Tree
{
    // What the inspector shows for a group row of the tree (Entities, Cameras, Lights): the layer that owns the group.
    // Each kind is its own type, so the inspector picks the panel of the kind with a data template.
    public abstract class TreeGroup
    {
        protected TreeGroup(Layer layer)
        {
            Layer = layer;
        }

        public Layer Layer { get; }

        // The group of a layer for the list of items, or null when the list is not one of the lists of the layer.
        public static TreeGroup? Of(Layer layer, CMiX.Core.Prefabs.Managers.PrefabManager? list)
        {
            if (ReferenceEquals(list, layer.ModelEntityManager))
                return new EntitiesGroup(layer);

            if (ReferenceEquals(list, layer.CameraManager))
                return new CamerasGroup(layer);

            if (ReferenceEquals(list, layer.LightManager))
                return new LightsGroup(layer);

            return null;
        }
    }

    public sealed class EntitiesGroup : TreeGroup
    {
        public EntitiesGroup(Layer layer) : base(layer) { }
    }

    public sealed class CamerasGroup : TreeGroup
    {
        public CamerasGroup(Layer layer) : base(layer) { }
    }

    public sealed class LightsGroup : TreeGroup
    {
        public LightsGroup(Layer layer) : base(layer) { }
    }
}
