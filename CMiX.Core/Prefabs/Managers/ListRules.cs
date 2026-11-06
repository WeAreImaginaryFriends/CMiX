// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System.Reflection;
using CMiX.Core.Modulation;
using CMiX.Core.Modulation.Modulators;
using CMiX.Core.Texturing;
using CMiX.Core.Texturing.Filters;
using CMiX.Core.Texturing.Sources;

namespace CMiX.Core.Prefabs.Managers
{
    // The one rule of which kinds of item an owner type can hold. The tree and the add menus use it.
    public static class ListRules
    {
        // A modifier goes only to an owner that its ModifierPanel attribute names.
        // A filter, a texture and a modulator go to an owner that has a list of that kind.
        // An item of another kind (an entity, a camera, a light, a layer) has no type rule here, so the result is true.
        public static bool CanHold(Type ownerType, Type itemType)
        {
            if (ownerType == null || itemType == null)
                return false;

            var panels = itemType.GetCustomAttributes<ModifierPanelAttribute>().ToList();
            var isFilter = typeof(ITextureFilter).IsAssignableFrom(itemType);
            var isTexture = typeof(ITextureSource).IsAssignableFrom(itemType);
            var isModulator = typeof(IModulator).IsAssignableFrom(itemType);

            if (panels.Count == 0 && !isFilter && !isTexture && !isModulator)
                return true;

            return panels.Any(panel => panel.PanelOwner == ownerType)
                || (isFilter && typeof(ITextureModifiable).IsAssignableFrom(ownerType))
                || (isTexture && typeof(ITexturable).IsAssignableFrom(ownerType))
                || (isModulator && typeof(IModulatable).IsAssignableFrom(ownerType));
        }
    }
}
