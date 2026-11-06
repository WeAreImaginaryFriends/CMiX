// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CMiX.Studio.Avalonia.Services
{
    // The model of the shortcut file. A value is a gesture string, for example "Ctrl+Shift+Z". An empty string means no gesture.
    public class ShortcutFile
    {
        public Dictionary<string, string> Shortcuts { get; set; } = new();
    }

    // The keyboard shortcuts of the Studio: a command name with its gesture. The part of the name before the first dot is the scope.
    // The XAML binds a gesture with the indexer, for example {Binding Shortcuts[Tree.Rename]}.
    public sealed class ShortcutMap : ObservableObject
    {
        public const string FileName = "studio-shortcuts.json";

        // The indexer name that a binding listens to.
        private const string IndexerName = "Item[]";

        public static string DefaultPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CMiX", FileName);

        private readonly (string Name, string Gesture)[] defaults =
        {
            ("Edit.Undo", "Ctrl+Z"),
            ("Edit.Redo", "Ctrl+Shift+Z"),
            ("Edit.RedoAlt", "Ctrl+Y"),
            ("Tree.Rename", "F2"),
            ("Tree.Remove", "Delete"),
            ("Tree.Duplicate", "Ctrl+D"),
            ("Tree.MoveUp", "Alt+Up"),
            ("Tree.MoveDown", "Alt+Down"),
            ("Tree.Open", "Enter"),
        };

        private readonly Dictionary<string, KeyGesture?> gestures = new();

        public ShortcutMap() : this(DefaultPath)
        {
        }

        // The constructor reads no file. Load reads it.
        public ShortcutMap(string filePath)
        {
            FilePath = filePath;
            Names = defaults.Select(entry => entry.Name).ToList();
            foreach (var (name, _) in defaults)
                gestures[name] = DefaultOf(name);
        }

        public string FilePath { get; }

        public IReadOnlyList<string> Names { get; }

        public IReadOnlyDictionary<string, KeyGesture?> Gestures => gestures;

        // An unknown name gives null.
        public KeyGesture? this[string name] => gestures.TryGetValue(name, out var gesture) ? gesture : null;

        // The names whose gesture another name of the same scope also has.
        public IReadOnlyCollection<string> Conflicts =>
            gestures
                .Where(entry => entry.Value != null)
                .GroupBy(entry => (Scope: ScopeOf(entry.Key), Gesture: entry.Value!.ToString()))
                .Where(group => group.Count() > 1)
                .SelectMany(group => group.Select(entry => entry.Key))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

        public static string ScopeOf(string name)
        {
            var dot = name.IndexOf('.');
            return dot < 0 ? name : name[..dot];
        }

        // An unknown name does nothing.
        public void Set(string name, KeyGesture? gesture)
        {
            if (!gestures.ContainsKey(name))
                return;

            gestures[name] = gesture;
            RaiseChanged();
        }

        public void Reset(string name) => Set(name, DefaultOf(name));

        public void ResetAll()
        {
            foreach (var name in Names)
                gestures[name] = DefaultOf(name);

            RaiseChanged();
        }

        // Reads the file. A missing file is written with the defaults. A file that can not be written does not stop the Studio.
        // An unknown name or a gesture that does not parse is ignored, and the default stays.
        public void Load()
        {
            if (!File.Exists(FilePath))
            {
                ResetAll();
                try
                {
                    Save();
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
                {
                    // The defaults stay in use.
                }

                return;
            }

            var settings = JsonSettingsFile.Load<ShortcutFile>(FilePath);
            foreach (var name in Names)
                gestures[name] = DefaultOf(name);

            foreach (var (name, text) in settings.Shortcuts ?? new Dictionary<string, string>())
                if (gestures.ContainsKey(name) && TryParse(text, out var gesture))
                    gestures[name] = gesture;

            RaiseChanged();
        }

        public void Save()
        {
            var settings = new ShortcutFile();
            foreach (var name in Names)
                settings.Shortcuts[name] = gestures[name]?.ToString() ?? string.Empty;

            JsonSettingsFile.Save(FilePath, settings);
        }

        private KeyGesture? DefaultOf(string name)
        {
            foreach (var (entry, gesture) in defaults)
                if (entry == name)
                    return KeyGesture.Parse(gesture);

            return null;
        }

        private static bool TryParse(string? text, out KeyGesture? gesture)
        {
            gesture = null;
            if (text == null)
                return false;

            if (text.Trim().Length == 0)
                return true;

            try
            {
                gesture = KeyGesture.Parse(text);
                return true;
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException or InvalidOperationException)
            {
                return false;
            }
        }

        private void RaiseChanged()
        {
            OnPropertyChanged("Item");
            OnPropertyChanged(IndexerName);
            OnPropertyChanged(nameof(Gestures));
            OnPropertyChanged(nameof(Conflicts));
        }
    }
}
