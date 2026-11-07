// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

namespace CMiX.UI.Popups
{
    // What a tooltip says, in lines: a title, the shortcut of the command, a description, the reason why the control is disabled, and a footer.
    // Only the title is needed. A line that is null is not shown. A plain string as a tip shows as a title only.
    // The reason has its own color, so the user sees why a control does nothing. The footer is a quiet line for a detail such as the
    // name of a command.
    public sealed record TipContent(string Title, string? Shortcut = null, string? Description = null, string? Reason = null, string? Footer = null)
    {
        // The text of a tip that is read as a string, such as by a test or a screen reader.
        public override string ToString() => Title;
    }
}
