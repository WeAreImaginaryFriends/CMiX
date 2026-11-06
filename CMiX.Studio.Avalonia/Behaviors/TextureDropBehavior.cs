// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

namespace CMiX.Studio.Avalonia.Behaviors
{
    // Files dropped on the control become textures of the prefab manager.
    // A folder with images becomes an image sequence.
    public class TextureDropBehavior : PrefabFileDropBehavior
    {
        protected override bool Accepts(string path) => Service?.AcceptsTexture(path) == true;

        protected override void Add(string path) => Service!.AddTexture(PrefabManager, path);
    }
}
