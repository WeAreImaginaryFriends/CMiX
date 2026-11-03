// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using CMiX.Core.Assets;
using CMiX.Core.Prefabs.Managers;
using CMiX.Core.Texturing.Sources;
using CMiX.UI.Behaviors;

namespace CMiX.Studio.Avalonia.Behaviors
{
    // Files dropped on the control become textures of the prefab manager.
    // A folder with images becomes an image sequence.
    public class TextureDropBehavior : FileDropBehavior
    {
        static TextureDropBehavior()
        {
            ModeProperty.OverrideDefaultValue<TextureDropBehavior>(FileDropMode.FilesAndFolders);
        }

        public static readonly StyledProperty<PrefabManager> PrefabManagerProperty =
            AvaloniaProperty.Register<TextureDropBehavior, PrefabManager>(nameof(PrefabManager));

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

            foreach (var file in paths)
            {
                if (Directory.Exists(file))
                {
                    if (AssetTypes.ContainsImages(file))
                        PrefabManager.AddItem(typeof(SequencePlayer), item =>
                        {
                            if (item is IAssetTextureSource source)
                                source.AssetSelector.SetAssetFromPath(file);
                        });
                    continue;
                }

                if (!AssetTypes.TryResolve(file, out var kind))
                    continue;

                Type? textureType = kind switch
                {
                    AssetKind.Image => typeof(CMiX.Core.Texturing.Sources.Image),
                    AssetKind.Video => typeof(VideoPlayer),
                    _ => null
                };
                if (textureType == null)
                    continue;

                PrefabManager.AddItem(textureType, item =>
                {
                    if (item is IAssetTextureSource source)
                        source.AssetSelector.SetAssetFromPath(file);
                });
            }
        }
    }
}
