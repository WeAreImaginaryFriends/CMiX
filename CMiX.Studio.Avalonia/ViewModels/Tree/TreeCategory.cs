// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using CMiX.Core;
using CMiX.Core.Compositing;
using CMiX.Core.Modifiers;
using CMiX.Core.Modulation.Modulators;
using CMiX.Core.Rendering.Cameras;
using CMiX.Core.Rendering.Lights;
using CMiX.Core.Texturing.Filters;
using CMiX.Core.Texturing.Sources;

namespace CMiX.Studio.Avalonia.ViewModels.Tree
{
    // The kind of a row of the project tree. A kind has one color, and an item and its group have the same kind.
    // A composition and a layer are both composable, so they have the same kind.
    public enum TreeCategory
    {
        Composable,
        Entity,
        Camera,
        Light,
        Texture,
        Filter,
        Modifier,
        Modulator
    }

    public static class TreeCategories
    {
        // The kind of an item, or null for an item that the tree does not know.
        public static TreeCategory? Of(IControl item) => item switch
        {
            Composition or Layer => TreeCategory.Composable,
            Entity or FullscreenQuad or TextEntity => TreeCategory.Entity,
            Camera => TreeCategory.Camera,
            LightEntity => TreeCategory.Light,
            IModifier => TreeCategory.Modifier,
            IModulator => TreeCategory.Modulator,
            ITextureFilter => TreeCategory.Filter,
            ITextureSource => TreeCategory.Texture,
            _ => null
        };

        // The kind of the items in a group.
        public static TreeCategory? Of(TreeGroup group) => group switch
        {
            EntitiesGroup => TreeCategory.Entity,
            CamerasGroup => TreeCategory.Camera,
            LightsGroup => TreeCategory.Light,
            TexturesGroup => TreeCategory.Texture,
            FiltersGroup => TreeCategory.Filter,
            ModifiersGroup => TreeCategory.Modifier,
            ModulatorsGroup => TreeCategory.Modulator,
            _ => null
        };
    }
}
