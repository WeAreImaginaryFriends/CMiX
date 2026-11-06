// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System.Collections.Generic;

namespace CMiX.Studio.Avalonia.Views.Dialogs
{
    // The result of a message dialog. Cancel is the first value, so a dialog that closes without an answer gives Cancel.
    public enum MessageDialogResult
    {
        Cancel,
        Ok,
        Save,
        DontSave
    }

    // One button of a message dialog. Enter runs the default button. Escape runs the cancel button.
    public sealed record MessageDialogButton(string Caption, MessageDialogResult Result, bool IsDefault = false, bool IsCancel = false);

    // What a message dialog shows. The title is a caption above the text. An empty title shows no caption.
    public sealed record MessageDialogOptions(string Text, string IconKey, IReadOnlyList<MessageDialogButton> Buttons, string Title = "")
    {
        public static readonly MessageDialogButton OkButton = new("OK", MessageDialogResult.Ok, IsDefault: true, IsCancel: true);

        // A message with one OK button. Enter and Escape both close it.
        public static MessageDialogOptions Notice(string text, string iconKey, string title = "") =>
            new(text, iconKey, new[] { OkButton }, title);
    }
}
