// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.IO;
using CMiX.Core;
using CMiX.Core.Assets;
using CMiX.Core.Compositing;
using CMiX.Core.Prefabs.Managers;
using CMiX.Core.Texturing;
using CMiX.Core.Texturing.Sources;

namespace CMiX.Studio.Avalonia.Services
{
    // Turns a dropped file or folder into an item of an entity list. The entity slot and the project tree use the same rules:
    // a folder with images gives a FullscreenQuad with an image sequence.
    // An image or a video gives an Entity with that texture.
    // A geometry file gives an Entity with that mesh.
    // A text file gives a TextEntity with the text of the file.
    public sealed class PathDropService
    {
        // A larger text file is ignored: the text goes into a text field and over the wire.
        public const long MaxTextBytes = 100 * 1024;

        private const string TextExtension = ".txt";

        private enum Kind
        {
            None,
            SequenceQuad,
            Image,
            Video,
            Geometry,
            Text
        }

        // True when AddItem makes an item from this path.
        public bool Accepts(string path) => Classify(path) != Kind.None;

        // Adds the item of the path to the list as one undo step. A path that is not accepted adds nothing.
        public void AddItem(PrefabManager entities, string path)
        {
            switch (Classify(path))
            {
                case Kind.SequenceQuad:
                    AddTextured(entities, typeof(FullscreenQuad), typeof(SequencePlayer), path);
                    break;
                case Kind.Image:
                    AddTextured(entities, typeof(Entity), typeof(CMiX.Core.Texturing.Sources.Image), path);
                    break;
                case Kind.Video:
                    AddTextured(entities, typeof(Entity), typeof(VideoPlayer), path);
                    break;
                case Kind.Geometry:
                    AddGeometry(entities, path);
                    break;
                case Kind.Text:
                    AddText(entities, path);
                    break;
            }
        }

        private static Kind Classify(string path)
        {
            if (Directory.Exists(path))
                return AssetTypes.ContainsImages(path) ? Kind.SequenceQuad : Kind.None;

            if (IsText(path))
                return Kind.Text;

            if (!AssetTypes.TryResolve(path, out var kind))
                return Kind.None;

            return kind switch
            {
                AssetKind.Image => Kind.Image,
                AssetKind.Video => Kind.Video,
                AssetKind.Geometry => Kind.Geometry,
                _ => Kind.None
            };
        }

        private static bool IsText(string path) =>
            string.Equals(Path.GetExtension(path), TextExtension, StringComparison.OrdinalIgnoreCase)
            && File.Exists(path)
            && new FileInfo(path).Length <= MaxTextBytes;

        // The item and its texture are one undo step.
        private static void AddTextured(PrefabManager entities, Type itemType, Type textureType, string path)
        {
            entities.AddItem(itemType, item =>
            {
                var textured = (ITexturable)item;
                textured.Texture.DiffuseTexture.TextureManager.AddItem(textureType, texture =>
                {
                    if (texture is IAssetTextureSource source)
                        source.AssetSelector.SetAssetFromPath(path);
                });
            });
        }

        private static void AddGeometry(PrefabManager entities, string path)
        {
            entities.AddItem(typeof(Entity), item =>
            {
                var entity = (Entity)item;
                entity.Mesh.MeshTypeSelector.Value = MeshType.FromFile;
                entity.Mesh.Geometry.SetAssetFromPath(path);
            });
        }

        // The text is a copy. The entity does not follow the file.
        private static void AddText(PrefabManager entities, string path)
        {
            var text = File.ReadAllText(path);
            entities.AddItem(typeof(TextEntity), item => ((TextEntity)item).Text.Value = text);
        }
    }
}
