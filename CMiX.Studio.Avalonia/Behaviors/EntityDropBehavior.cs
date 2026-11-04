// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System.Collections.Generic;
using Avalonia;
using CMiX.Core.Prefabs.Managers;
using CMiX.Studio.Avalonia.Services;
using CMiX.UI.Behaviors;

namespace CMiX.Studio.Avalonia.Behaviors
{
    // Files and folders dropped on the control become entities of the prefab manager.
    // PathDropService holds the rules: what a path becomes.
    public class EntityDropBehavior : FileDropBehavior
    {
        private readonly PathDropService pathDrop = new();

        static EntityDropBehavior()
        {
            ModeProperty.OverrideDefaultValue<EntityDropBehavior>(FileDropMode.FilesAndFolders);
        }

        public static readonly StyledProperty<PrefabManager> PrefabManagerProperty =
            AvaloniaProperty.Register<EntityDropBehavior, PrefabManager>(nameof(PrefabManager));

        public PrefabManager PrefabManager
        {
            get => GetValue(PrefabManagerProperty);
            set => SetValue(PrefabManagerProperty, value);
        }

        protected override void OnPathsDropped(IReadOnlyList<string> paths)
        {
            base.OnPathsDropped(paths);

            if (PrefabManager == null)
                return;

            foreach (var path in paths)
                pathDrop.AddItem(PrefabManager, path);
        }
    }
}
