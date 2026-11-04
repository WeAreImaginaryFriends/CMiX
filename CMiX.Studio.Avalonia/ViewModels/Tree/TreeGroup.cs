// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using CMiX.Core;
using CMiX.Core.Compositing;
using CMiX.Core.Modifiers;
using CMiX.Core.Modulation;
using CMiX.Core.Modulation.Modulators;
using CMiX.Core.Prefabs.Managers;
using CMiX.Core.Texturing;

namespace CMiX.Studio.Avalonia.ViewModels.Tree
{
    // What the inspector shows for a group row of the tree: the item that owns the group, and the list of the group.
    // The groups of a layer are Entities, Cameras and Lights. Any item can also have Textures, Filters and Modifiers,
    // by what it can hold (ITexturable, ITextureModifiable, IModifiable).
    // Each kind is its own type, so the inspector picks the panel of the kind with a data template.
    public abstract class TreeGroup
    {
        protected TreeGroup(IControl owner, PrefabManager manager)
        {
            Owner = owner;
            Manager = manager;
        }

        public IControl Owner { get; }
        public PrefabManager Manager { get; }

        // The type of the owner. The panels that add items list what can be added to this type.
        public System.Type OwnerType => Owner.GetType();

        // The group of an item for a list, or null when the list is not a list that the tree shows as a group of the item.
        public static TreeGroup? For(IControl? owner, PrefabManager? list)
        {
            if (owner == null || list == null)
                return null;

            if (owner is Layer layer)
            {
                if (ReferenceEquals(list, layer.ModelEntityManager))
                    return new EntitiesGroup(layer, list);

                if (ReferenceEquals(list, layer.CameraManager))
                    return new CamerasGroup(layer, list);

                if (ReferenceEquals(list, layer.LightManager))
                    return new LightsGroup(layer, list);
            }

            if (owner is ITexturable textured && ReferenceEquals(list, textured.Texture?.DiffuseTexture?.TextureManager))
                return new TexturesGroup(owner, list);

            if (owner is ITextureModifiable filtered && ReferenceEquals(list, filtered.TextureModifierManager))
                return new FiltersGroup(owner, list);

            if (owner is IModifiable modified && ReferenceEquals(list, modified.ModifierManager))
                return new ModifiersGroup(owner, list);

            if (owner is IModulatable modulated && ReferenceEquals(list, modulated.ModulatorManager))
                return new ModulatorsGroup(owner, list);

            return null;
        }
    }

    // The groups of a layer. The panels of the old tabs take the layer as their DataContext.
    public abstract class LayerGroup : TreeGroup
    {
        protected LayerGroup(Layer layer, PrefabManager manager) : base(layer, manager)
        {
            Layer = layer;
        }

        public Layer Layer { get; }
    }

    public sealed class EntitiesGroup : LayerGroup
    {
        public EntitiesGroup(Layer layer, PrefabManager manager) : base(layer, manager) { }
    }

    public sealed class CamerasGroup : LayerGroup
    {
        public CamerasGroup(Layer layer, PrefabManager manager) : base(layer, manager) { }
    }

    public sealed class LightsGroup : LayerGroup
    {
        public LightsGroup(Layer layer, PrefabManager manager) : base(layer, manager) { }
    }

    // The texture sources of an item.
    public sealed class TexturesGroup : TreeGroup
    {
        public TexturesGroup(IControl owner, PrefabManager manager) : base(owner, manager) { }
    }

    // The texture filters of an item, a composition, a layer or a texture.
    public sealed class FiltersGroup : TreeGroup
    {
        public FiltersGroup(IControl owner, PrefabManager manager) : base(owner, manager) { }
    }

    // The modifiers of an item.
    public sealed class ModifiersGroup : TreeGroup
    {
        public ModifiersGroup(IControl owner, PrefabManager manager) : base(owner, manager) { }
    }

    // The modulators of a modifier, a filter or a texture.
    public sealed class ModulatorsGroup : TreeGroup
    {
        public ModulatorsGroup(IControl owner, PrefabManager manager) : base(owner, manager) { }
    }
}
