using System.IO;
using Avalonia.Input;
using CMiX.Studio.Avalonia.Services;
using Xunit;

namespace CMiX.Studio.Avalonia.Tests
{
    // The shortcut map against files in a temp folder only. The file of the user is never touched.
    public class ShortcutMapTests : IDisposable
    {
        private readonly string folder = Path.Combine(Path.GetTempPath(), $"cmix-shortcuts-{Guid.NewGuid():N}");

        private string FilePath => Path.Combine(folder, ShortcutMap.FileName);

        public void Dispose()
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        private void WriteFile(string json)
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(FilePath, json);
        }

        [Fact]
        public void Names_ByDefault_ListEveryNameWithAGesture()
        {
            var map = new ShortcutMap(FilePath);

            Assert.Equal(
                new[] { "Edit.Undo", "Edit.Redo", "Edit.RedoAlt", "Tree.Rename", "Tree.Remove", "Tree.Duplicate", "Tree.MoveUp", "Tree.MoveDown", "Tree.Open" },
                map.Names);
            foreach (var name in map.Names)
                Assert.NotNull(map[name]);

            Assert.Equal(new KeyGesture(Key.Z, KeyModifiers.Control | KeyModifiers.Shift), map["Edit.Redo"]);
            Assert.Equal(new KeyGesture(Key.Up, KeyModifiers.Alt), map["Tree.MoveUp"]);
        }

        [Fact]
        public void Conflicts_ByDefault_IsEmpty()
        {
            Assert.Empty(new ShortcutMap(FilePath).Conflicts);
        }

        [Fact]
        public void Load_AFileEntry_WinsOverTheDefault()
        {
            WriteFile("{ \"Shortcuts\": { \"Tree.Rename\": \"Ctrl+R\" } }");
            var map = new ShortcutMap(FilePath);

            map.Load();

            Assert.Equal(new KeyGesture(Key.R, KeyModifiers.Control), map["Tree.Rename"]);
            Assert.Equal(new KeyGesture(Key.Z, KeyModifiers.Control), map["Edit.Undo"]);
        }

        [Fact]
        public void Load_ABrokenEntry_KeepsTheDefault()
        {
            WriteFile("{ \"Shortcuts\": { \"Tree.Rename\": \"Ctrl+NoSuchKey\" } }");
            var map = new ShortcutMap(FilePath);

            map.Load();

            Assert.Equal(new KeyGesture(Key.F2), map["Tree.Rename"]);
        }

        [Fact]
        public void Load_ABrokenFile_KeepsTheDefaults()
        {
            WriteFile("{ not json");
            var map = new ShortcutMap(FilePath);

            map.Load();

            Assert.Equal(new KeyGesture(Key.F2), map["Tree.Rename"]);
            Assert.Equal("{ not json", File.ReadAllText(FilePath));
        }

        [Fact]
        public void Load_AnUnknownName_IsIgnored()
        {
            WriteFile("{ \"Shortcuts\": { \"Tree.NoSuchCommand\": \"Ctrl+R\" } }");
            var map = new ShortcutMap(FilePath);

            map.Load();

            Assert.DoesNotContain("Tree.NoSuchCommand", map.Names);
            Assert.Null(map["Tree.NoSuchCommand"]);
            Assert.Empty(map.Conflicts);
        }

        [Fact]
        public void Load_AnEmptyEntry_RemovesTheGesture()
        {
            WriteFile("{ \"Shortcuts\": { \"Tree.Open\": \"\" } }");
            var map = new ShortcutMap(FilePath);

            map.Load();

            Assert.Null(map["Tree.Open"]);
        }

        [Fact]
        public void Load_AMissingFile_WritesTheDefaults()
        {
            var map = new ShortcutMap(FilePath);

            map.Load();

            Assert.True(File.Exists(FilePath));
            var copy = new ShortcutMap(FilePath);
            copy.Set("Edit.Undo", null);
            copy.Load();
            foreach (var name in map.Names)
                Assert.Equal(map[name], copy[name]);
        }

        [Fact]
        public void Save_AfterSet_WritesTheChange()
        {
            var map = new ShortcutMap(FilePath);
            map.Set("Tree.Duplicate", new KeyGesture(Key.D, KeyModifiers.Control | KeyModifiers.Shift));

            map.Save();

            var copy = new ShortcutMap(FilePath);
            copy.Load();
            Assert.Equal(new KeyGesture(Key.D, KeyModifiers.Control | KeyModifiers.Shift), copy["Tree.Duplicate"]);
        }

        [Fact]
        public void Set_AGesture_RaisesTheChangeOfTheIndexer()
        {
            var map = new ShortcutMap(FilePath);
            var names = new List<string?>();
            map.PropertyChanged += (_, e) => names.Add(e.PropertyName);

            map.Set("Edit.Undo", new KeyGesture(Key.U, KeyModifiers.Control));

            Assert.Contains("Item[]", names);
        }

        [Fact]
        public void Reset_AfterSet_GivesTheDefault()
        {
            var map = new ShortcutMap(FilePath);
            map.Set("Edit.Undo", null);
            map.Set("Edit.Redo", null);

            map.Reset("Edit.Undo");
            Assert.Equal(new KeyGesture(Key.Z, KeyModifiers.Control), map["Edit.Undo"]);
            Assert.Null(map["Edit.Redo"]);

            map.ResetAll();
            Assert.Equal(new KeyGesture(Key.Z, KeyModifiers.Control | KeyModifiers.Shift), map["Edit.Redo"]);
        }

        [Fact]
        public void Conflicts_TwoNamesWithOneGesture_ListsThemInOneScopeOnly()
        {
            var map = new ShortcutMap(FilePath);

            map.Set("Tree.Rename", new KeyGesture(Key.Delete));
            Assert.Equal(new[] { "Tree.Remove", "Tree.Rename" }, map.Conflicts);

            map.ResetAll();
            map.Set("Tree.Rename", new KeyGesture(Key.Z, KeyModifiers.Control));
            Assert.Empty(map.Conflicts);
        }
    }
}
