// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using CMiX.Core.Compositing;
using CMiX.Core.Rendering.Cameras;
using CMiX.Core.Rendering.Lights;

namespace CMiX.Studio.Avalonia.ViewModels.Tree
{
    // The icons of the types of the tree (see StudioIcons). A row of the tree and an entry of the Add panel read the same keys.
    // Only a type that has an icon of its own is here. A filter, a modifier or a texture has the icon of its kind, and a node uses that
    // one (see TreeNode). The Add panel shows no icon for such an entry.
    public static class TreeIcons
    {
        // The key of the icon of a type, or null when the type has none of its own.
        public static string? OfType(Type type)
        {
            if (type == typeof(Composition)) return "Composition";
            if (type == typeof(Layer)) return "LayerScene";
            if (type == typeof(Entity)) return "Geometry";
            if (type == typeof(FullscreenQuad)) return "FullscreenQuad";
            if (type == typeof(TextEntity)) return "TextIcon";
            if (type == typeof(Camera)) return "Camera";
            if (type == typeof(LightEntity)) return "MeshLight";
            return null;
        }
    }
}
