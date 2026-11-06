// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

namespace CMiX.Studio.Avalonia.Behaviors
{
    // Files and folders dropped on the control become entities of the prefab manager.
    public class EntityDropBehavior : PrefabFileDropBehavior
    {
        protected override bool Accepts(string path) => Service?.AcceptsItem(path) == true;

        protected override void Add(string path) => Service!.AddItem(PrefabManager, path);
    }
}
