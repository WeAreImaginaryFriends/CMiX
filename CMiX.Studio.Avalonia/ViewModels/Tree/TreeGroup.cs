// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using CMiX.Core;
using CMiX.Core.Compositing;
using CMiX.Core.Modifiers;
using CMiX.Core.Modulation;
using CMiX.Core.Modulation.Modulators;
using CMiX.Core.Prefabs.Managers;
using CMiX.Core.Rendering.Cameras;
using CMiX.Core.Rendering.Lights;
using CMiX.Core.Texturing;
using CMiX.Core.Texturing.Filters;
using CMiX.Core.Texturing.Sources;

namespace CMiX.Studio.Avalonia.ViewModels.Tree
{
    // What the inspector shows for a group row of the tree: the item that owns the group, and the list of the group.
    // The groups of a layer are Entities, Cameras and Lights. Any item can also have Textures, Filters, Modifiers and Modulators,
    // by what it can hold (ITexturable, ITextureModifiable, IModifiable, IModulatable).
    // Each kind is its own type, so the inspector picks the panel of the kind with a data template.
    // Each kind gives its header, its icon, its category and the types that its add menu offers.
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
        public Type OwnerType => Owner.GetType();

        // The text of the group row.
        public abstract string Header { get; }

        // The key of the icon of the group row (see CMiX.UI.Themes.Icons).
        public abstract string IconKey { get; }

        // The kind of the items in the group.
        public abstract TreeCategory Category { get; }

        // The item types that the add menu of the group offers.
        public abstract IReadOnlyList<Type> AddableTypes { get; }

        // The groups of an item, in the order of the tree: the lists of a layer, then the textures, the filters, the modifiers
        // and the modulators.
        public static IReadOnlyList<TreeGroup> GroupsOf(IControl? owner)
        {
            var groups = new List<TreeGroup>();
            if (owner is Layer layer)
            {
                groups.Add(new EntitiesGroup(layer, layer.ModelEntityManager));
                groups.Add(new CamerasGroup(layer, layer.CameraManager));
                groups.Add(new LightsGroup(layer, layer.LightManager));
            }

            if (owner is ITexturable textured && textured.Texture?.DiffuseTexture?.TextureManager is { } textures)
                groups.Add(new TexturesGroup(owner, textures));

            if (owner is ITextureModifiable filtered && filtered.TextureModifierManager is { } filters)
                groups.Add(new FiltersGroup(owner, filters));

            if (owner is IModifiable modified && modified.ModifierManager is { } modifiers)
                groups.Add(new ModifiersGroup(owner, modifiers));

            if (owner is IModulatable modulated && modulated.ModulatorManager is { } modulators)
                groups.Add(new ModulatorsGroup(owner, modulators));

            return groups;
        }

        // The group of an item for a list, or null when the list is not a list that the tree shows as a group of the item.
        public static TreeGroup? For(IControl? owner, PrefabManager? list) =>
            list == null ? null : GroupsOf(owner).FirstOrDefault(group => ReferenceEquals(group.Manager, list));
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
        public const string Title = "Entities";
        private static readonly Type[] addable = { typeof(Entity), typeof(FullscreenQuad), typeof(TextEntity) };

        public EntitiesGroup(Layer layer, PrefabManager manager) : base(layer, manager) { }

        public override string Header => Title;
        public override string IconKey => "Geometry";
        public override TreeCategory Category => TreeCategory.Entity;
        public override IReadOnlyList<Type> AddableTypes => addable;
    }

    public sealed class CamerasGroup : LayerGroup
    {
        public const string Title = "Cameras";
        private static readonly Type[] addable = { typeof(Camera) };

        public CamerasGroup(Layer layer, PrefabManager manager) : base(layer, manager) { }

        public override string Header => Title;
        public override string IconKey => "Camera";
        public override TreeCategory Category => TreeCategory.Camera;
        public override IReadOnlyList<Type> AddableTypes => addable;
    }

    public sealed class LightsGroup : LayerGroup
    {
        public const string Title = "Lights";
        private static readonly Type[] addable = { typeof(LightEntity) };

        public LightsGroup(Layer layer, PrefabManager manager) : base(layer, manager) { }

        public override string Header => Title;
        public override string IconKey => "MeshLight";
        public override TreeCategory Category => TreeCategory.Light;
        public override IReadOnlyList<Type> AddableTypes => addable;
    }

    // The texture sources of an item.
    public sealed class TexturesGroup : TreeGroup
    {
        public const string Title = "Textures";

        public TexturesGroup(IControl owner, PrefabManager manager) : base(owner, manager) { }

        public override string Header => Title;
        public override string IconKey => "Texture";
        public override TreeCategory Category => TreeCategory.Texture;
        public override IReadOnlyList<Type> AddableTypes => Tree.AddableTypes.ForInterface(typeof(ITextureSource));
    }

    // The texture filters of an item, a composition, a layer or a texture.
    public sealed class FiltersGroup : TreeGroup
    {
        public const string Title = "Filters";

        public FiltersGroup(IControl owner, PrefabManager manager) : base(owner, manager) { }

        public override string Header => Title;
        public override string IconKey => "Filter";
        public override TreeCategory Category => TreeCategory.Filter;
        public override IReadOnlyList<Type> AddableTypes => Tree.AddableTypes.ForInterface(typeof(ITextureFilter));
    }

    // The modifiers of an item. ListRules.CanHold picks the modifiers that the type of the owner can take.
    public sealed class ModifiersGroup : TreeGroup
    {
        public const string Title = "Modifiers";

        public ModifiersGroup(IControl owner, PrefabManager manager) : base(owner, manager) { }

        public override string Header => Title;
        public override string IconKey => "Modifier";
        public override TreeCategory Category => TreeCategory.Modifier;
        public override IReadOnlyList<Type> AddableTypes => Tree.AddableTypes.ForOwner(OwnerType);
    }

    // The modulators of a modifier, a filter or a texture.
    public sealed class ModulatorsGroup : TreeGroup
    {
        public const string Title = "Modulators";

        public ModulatorsGroup(IControl owner, PrefabManager manager) : base(owner, manager) { }

        public override string Header => Title;
        public override string IconKey => "Modulator";
        public override TreeCategory Category => TreeCategory.Modulator;
        public override IReadOnlyList<Type> AddableTypes => Tree.AddableTypes.ForInterface(typeof(IModulator));
    }
}
