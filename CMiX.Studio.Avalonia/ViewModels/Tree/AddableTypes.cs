// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CMiX.Core.Prefabs;
using CMiX.Core.Texturing.Filters;
using CMiX.Core.Texturing.Sources;

namespace CMiX.Studio.Avalonia.ViewModels.Tree
{
    // The types that can be added to a list of modifiers, filters or textures. The panels that add an item and the menus of the tree use it.
    public static class AddableTypes
    {
        private static readonly Dictionary<Type, IReadOnlyList<Type>> cache = new();

        // The modifiers that an owner of this type can take, by the ModifierPanel attribute of the modifier.
        public static IReadOnlyList<Type> ForOwner(Type ownerType) =>
            Cached(ownerType, () => typeof(ModifierPanelAttribute).Assembly
                .GetTypes()
                .Concat(ownerType.Assembly.GetTypes())
                .Distinct()
                .Where(IsAddable)
                .Where(type => type.GetCustomAttributes<ModifierPanelAttribute>().Any(attribute => attribute.PanelOwner == ownerType)));

        // The types that implement an interface, in the assembly of the interface.
        public static IReadOnlyList<Type> ForInterface(Type interfaceType) =>
            Cached(interfaceType, () => interfaceType.Assembly
                .GetTypes()
                .Where(IsAddable)
                .Where(interfaceType.IsAssignableFrom));

        // What a list of a group takes: filters and textures by interface, modifiers by the type of the owner.
        public static IReadOnlyList<Type> ForGroup(TreeGroup group) => group switch
        {
            FiltersGroup => ForInterface(typeof(ITextureFilter)),
            TexturesGroup => ForInterface(typeof(ITextureSource)),
            ModifiersGroup => ForOwner(group.OwnerType),
            _ => Array.Empty<Type>()
        };

        public static string LabelOf(Type type) =>
            ControlFactory.StringHelper.LabelOf(type) ?? ControlFactory.StringHelper.PascalCaseToDisplay(type.Name);

        private static bool IsAddable(Type type) =>
            !type.IsAbstract && !type.IsInterface && type.GetCustomAttribute<HiddenInMenuAttribute>() == null;

        private static IReadOnlyList<Type> Cached(Type key, Func<IEnumerable<Type>> discover)
        {
            lock (cache)
            {
                if (!cache.TryGetValue(key, out var types))
                    cache[key] = types = discover().OrderBy(LabelOf).ToList();

                return types;
            }
        }
    }
}
