// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace CMiX.Studio.Avalonia.Views
{
    // Avalonia runs the key bindings of the focused element and its parents before the text box sees the key. So a text box under the
    // owner would lose Delete, Enter or Ctrl+Z to a key binding. While a text box under the owner has the focus, the owner has no key
    // bindings. They come back when the focus leaves the text box.
    // An owner can call RunPausedBinding for a key that the text box does not handle, when the key comes up to the owner.
    internal sealed class KeyBindingTextGuard
    {
        private readonly InputElement owner;
        private readonly List<KeyBinding> paused = new();

        private KeyBindingTextGuard(InputElement owner)
        {
            this.owner = owner;
            owner.AddHandler(InputElement.GotFocusEvent, OnGotFocus, RoutingStrategies.Bubble, true);
            owner.AddHandler(InputElement.LostFocusEvent, OnLostFocus, RoutingStrategies.Bubble, true);
        }

        public static KeyBindingTextGuard Install(InputElement owner) => new(owner);

        // Runs the first paused binding that matches the key, and handles the key.
        public void RunPausedBinding(KeyEventArgs e)
        {
            if (e.Handled)
                return;

            foreach (var binding in paused)
            {
                if (binding.Gesture?.Matches(e) != true || binding.Command is not { } command || !command.CanExecute(binding.CommandParameter))
                    continue;

                command.Execute(binding.CommandParameter);
                e.Handled = true;
                return;
            }
        }

        public bool IsPaused => paused.Count > 0;

        private void OnGotFocus(object? sender, GotFocusEventArgs e) => Update(e.Source);

        // The next focus is known only after the event.
        private void OnLostFocus(object? sender, RoutedEventArgs e) =>
            Dispatcher.UIThread.Post(() => Update(TopLevel.GetTopLevel(owner as Visual)?.FocusManager?.GetFocusedElement()));

        private void Update(object? focused)
        {
            if (IsInTextBox(focused))
                Pause();
            else
                Resume();
        }

        private void Pause()
        {
            if (IsPaused || owner.KeyBindings.Count == 0)
                return;

            paused.AddRange(owner.KeyBindings);
            owner.KeyBindings.Clear();
        }

        private void Resume()
        {
            if (!IsPaused)
                return;

            owner.KeyBindings.AddRange(paused);
            paused.Clear();
        }

        private bool IsInTextBox(object? focused)
        {
            if (focused is not Visual visual || !ReferenceEquals(owner, visual) && !((Visual)owner).IsVisualAncestorOf(visual))
                return false;

            for (var current = visual; current != null; current = current.GetVisualParent())
                if (current is TextBox)
                    return true;

            return false;
        }
    }
}
