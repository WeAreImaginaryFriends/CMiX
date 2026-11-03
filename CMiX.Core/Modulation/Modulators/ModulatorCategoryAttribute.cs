// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System.Collections.Concurrent;
using System.Reflection;

namespace CMiX.Core.Modulation.Modulators
{
    // Sets the category of a modulator type. A derived type inherits it. A type without it is Internal.
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class ModulatorCategoryAttribute : Attribute
    {
        private static readonly ConcurrentDictionary<Type, ModulatorCategory> Cache = new();

        public ModulatorCategoryAttribute(ModulatorCategory category) => Category = category;

        public ModulatorCategory Category { get; }

        public static ModulatorCategory Of(Type modulatorType) =>
            Cache.GetOrAdd(modulatorType, type =>
                type.GetCustomAttribute<ModulatorCategoryAttribute>()?.Category ?? ModulatorCategory.Internal);
    }
}
