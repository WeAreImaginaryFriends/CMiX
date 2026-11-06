// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

namespace CMiX.Core.Prefabs.Managers
{
    // What a move of an item to another list did.
    // Moved is true when the item is in the target list. Blocking lists the items that stop the move, empty when none.
    public sealed record MoveResult(bool Moved, IReadOnlyList<IPrefab> Blocking)
    {
        // The move changed nothing and nothing blocks it.
        public static MoveResult None { get; } = new(false, Array.Empty<IPrefab>());

        public static MoveResult Done { get; } = new(true, Array.Empty<IPrefab>());

        public static MoveResult BlockedBy(IReadOnlyList<IPrefab> blocking) => new(false, blocking);
    }
}
