// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

namespace CMiX.Core.Prefabs
{
    // An item that can refuse to move to another list.
    public interface IMoveRestricted
    {
        // The items that block the move. An empty list means the item can move.
        // siblings are the items of the list that the item leaves.
        IReadOnlyList<IPrefab> GetItemsBlockingMove(IEnumerable<IControl> siblings);
    }
}
