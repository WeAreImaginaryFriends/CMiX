// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using CMiX.Core;
using CMiX.Core.Assets;
using CMiX.Core.Compositing;
using CMiX.Core.Prefabs.Managers;
using CMiX.Core.Texturing;
using CMiX.Core.Texturing.Sources;
using CMiX.UI.Behaviors;

namespace CMiX.Studio.Avalonia.Behaviors
{
    // Files and folders dropped on the control become entities of the prefab manager.
    // A folder with images gives a FullscreenQuad with an image sequence.
    // An image or a video gives an Entity with that texture.
    // A geometry file gives an Entity with that mesh.
    public class EntityDropBehavior : FileDropBehavior
    {
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
            {
                if (Directory.Exists(path))
                {
                    if (AssetTypes.ContainsImages(path))
                        AddTextured(typeof(FullscreenQuad), typeof(SequencePlayer), path);
                    continue;
                }

                if (!AssetTypes.TryResolve(path, out var kind))
                    continue;

                switch (kind)
                {
                    case AssetKind.Image:
                        AddTextured(typeof(Entity), typeof(CMiX.Core.Texturing.Sources.Image), path);
                        break;
                    case AssetKind.Video:
                        AddTextured(typeof(Entity), typeof(VideoPlayer), path);
                        break;
                    case AssetKind.Geometry:
                        AddEntity(path);
                        break;
                }
            }
        }

        // The item and its texture are one undo step.
        private void AddTextured(Type itemType, Type textureType, string path)
        {
            PrefabManager.AddItem(itemType, item =>
            {
                var textured = (ITexturable)item;
                textured.Texture.DiffuseTexture.TextureManager.AddItem(textureType, texture =>
                {
                    if (texture is IAssetTextureSource source)
                        source.AssetSelector.SetAssetFromPath(path);
                });
            });
        }

        private void AddEntity(string path)
        {
            PrefabManager.AddItem(typeof(Entity), item =>
            {
                var entity = (Entity)item;
                entity.Mesh.MeshTypeSelector.Value = MeshType.FromFile;
                entity.Mesh.Geometry.SetAssetFromPath(path);
            });
        }
    }
}
