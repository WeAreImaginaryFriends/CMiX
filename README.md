# CMiX

CMiX is a realtime graphic composition tool.

## Requirements

- Windows and .NET 10
- A Redis or Memurai server
- vvvv gamma for the engine in `CMiX.Engine`. vvvv has its own license: https://vvvv.org/licensing

## Build

    dotnet build CMiX.Studio.Avalonia\CMiX.Studio.Avalonia.csproj

The notes for developers are in [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md).

## Distribution

The Fallout pipeline in `build/Build.cs` publishes the Studio for distribution. The script `build.ps1` restores the tool and runs it. The pipeline needs the .NET 10 SDK. The targets `Pack` and `Push` also need Chocolatey.

Targets:

- `Clean`: empties `artifacts`.
- `Publish`: publishes the Studio to `artifacts/CMiX.Studio` and copies the settings file over `studio-settings.json`.
- `Zip`: zips the folder to `artifacts/CMiX.Studio_<version>_win-x64.zip`. This is the default target.
- `Pack`: builds the Chocolatey package in `artifacts` from the zip.
- `Push`: pushes the Chocolatey package to a feed.

This command builds `artifacts\CMiX.Studio` and the zip:

    .\build.ps1

This command ships your settings file as `studio-settings.json`. Without the parameter, the file in `CMiX.Studio.Avalonia` ships. Keep a file with project values outside the repository.

    .\build.ps1 --settings-file C:\path\to\studio-settings.json

This command adds the Chocolatey package:

    .\build.ps1 Pack

This command pushes the package. Fallout also reads both values from the environment variables `FEED` and `API_KEY`.

    .\build.ps1 Push --feed <url> --api-key <key>

The file `version.txt` holds the version of the zip, the package and the executable. The package installs the Studio into `C:\tools\CMiX.Studio` and adds a desktop shortcut. The Studio needs the .NET 10 runtime (package `dotnet-10.0-runtime`). An upgrade replaces `studio-settings.json` in that folder with the shipped file.

## Project files

The Studio saves a project as a `.cmix` file. The file stores the path of every asset relative to its own folder. A project and its assets move together to another machine when the folders between them stay the same. This example has a project and an asset:

    VJ\compositions\bar.cmix
    VJ\textures\foo.jpg

The file stores the asset as `..\textures\foo.jpg`.

An asset on another drive than the project file keeps its absolute path. A project file from an older version keeps its absolute paths until the next save.

The emergency save in the temp folder holds paths relative to that folder. Open it from there and the paths work. Copy the file to another folder before you open it and the paths break.

The start check reports "Not in sync" when you open a project on a machine other than the one that pushed the Redis snapshot, because the absolute paths differ. Click Push on that machine.

A project file saved before version 1.0.0 stores the Gamma of a gradient, the Frequency and Contrast of a bubble noise and the Cell Count of a checker board as plain values. `ModulatableValueUpgradeJsonConverter` reads that shape into the modulatable shape when the file opens, and the next save writes the new shape. Remove the class, its line in `ProjectSerializer.Options`, its test class and this note when no project file from before that version needs to open.

## Project tree

The tree shows the project as rows. The one row at the top is the group Compositions, and the compositions are under it. A composition holds the group Layers (with its layers under it), then its Filters and Modifiers. A layer holds Entities, Filters, Modifiers, Cameras and Lights. An item can hold textures, filters, modifiers and modulators. They are groups under the item. The groups Compositions and Layers have a hide button like the other groups, and they have no panel in the inspector yet. A layer dropped on a composition goes to the end of its Layers group.

Each row has a kind: composable (a composition and a layer), entity, camera, light, texture, filter, modifier or modulator. A group has the kind of its items. The kind sets the color of the row.

- The colors of the kinds are in `Colors.axaml`, with the names `Category` and the kind. A composable inside a composable has one more color for each level, `CategoryComposableLevel1` to `CategoryComposableLevel3`.
- A row is filled with its color mixed with the gray of the panel. The fill under the pointer is as bright as the fill of a selected row, with no border and a white name. A selected row also has a border of one pixel of the screen in the bright color of its kind, and its name is white. A group and its items have the same tone. What a row holds of another kind is weaker by one step for each change of kind (`TreeNode.KindDepth`), down to a floor. `CategoryPalette` holds the strength of each step, and a test checks the contrast of the text on every kind, level and state.
- The fill of a row starts at its indent, so no fill touches the left border of the tree. The space between two rows is 3 pixels (`ProjectTreeView.RowGap`). The space between a row and its toggles stays 1 pixel.
- A collapsed row shows what it holds: a small square for each kind, in the color of the kind, with the number of items. An open row shows none. A new row, made by the user, starts open, and the rows above it open too. When a project opens, every row starts collapsed.
- The toggles are icons inside the colored row, at its right end, with no fill behind them. The mask toggle is the first, on a composition and a layer. The visibility toggle is the last, at the right end. A place without a toggle (a group, or the mask of an entity) stays empty, so the icons of every row line up. A new toggle is a new slot in `RowToggles`. A group row (Entities, Filters, Cameras and so on) has a hide button in the place of the visibility toggle. It hides the whole list in the engine (`PrefabManager.Hidden`, applied in `VisibleItemsProcessor`). The items keep their own visibility, so showing the list again brings back what was shown. While a list is hidden, the visibility toggles of every row below it look greyed, as if disabled, but they still work (`TreeNode.IsUnderHiddenParent`). The state is saved in the project file (`PrefabManagerModel.Hidden`) and sent to the peers; a file without it opens with the list visible.
- The title bar of the inspector has the color of the kind of the selected row. It shows the icon of the row, then its name. The name of an item can be edited there. At the right end are, for a composition or a layer, the mask toggle, then the visibility toggle. A group shows its name only.
- The tabs beside the tree behave as normal tabs: a click on a row of the tree does not change the selected tab. Every icon has the one size `IconSizeXS`, also in the menus.
- The tooltips of the rows of the tree (the eye, the mask, the hide button of a group, the numbers of a collapsed row) are opened by the tree itself (`TreeTooltips`), not by the tooltip service of Avalonia, which did not open them in the running app. The tooltips of the toolbar still use the service.
- While a row is dragged, a picture of the whole row (fill, icon and name) follows the pointer (`TreeDragGhost`, on the overlay layer of the window). It keeps the point that was pressed under the pointer, and it is taken before the row changes its look. The row that is dragged stays in its place at 40% opacity.
- Two bands alternate behind the rows of the tree (`RowStripes`, one pitch per row: the row height plus `TreeRowGap`). They follow the rows when the tree scrolls or a row opens.
- Every search field is a `SearchBox`. A drop-down gets one with `ComboBoxSearch.IsSearchable`, or `IsSearchable` on a `ComboBoxCaptioned` (the blend modes use it). The field filters the rows, and Enter picks the first one left.
- Every panel (the tree, the lists, the inspector and the tabs) shows its scroll bar over the content (`ScrollViewerOverlay`). The content keeps a gutter for the bar, so it does not move when the bar appears. A tab view in the inspector does not scroll on its own: the inspector scrolls it.

While you drag a row, the rows that take it inside are lit, with no border: for a modifier, the group Modifiers of its own entity and of the other entities, for a filter the groups Filters, and for a layer the compositions. A row dropped on the group of its own list goes to the end of the list. The row under the pointer is lit more. The rows next to which the row can go keep their look, and the drop line shows the place: it fills the space between the two rows, under the row above the place (the first row of the tree has it at its top). A row drag is a pointer drag of the tree, not a drag of the system: the tree captures the pointer, so the mouse wheel scrolls the tree during the drag, Escape ends the drag without a drop, and the cursor is the move cursor on every row (`TreeRowDrag`). The tree scrolls while the pointer of a drag is near its top or bottom edge, and faster when it is closer to the edge. A file drag from the file manager is a drag of the system; it scrolls the tree by the edge zone. A drop does not move the view: the first row in view stays where it is, and the tree does not scroll to the moved row. A tree that is higher than its view has room under its last row, so its last row can reach the top of the view. A row that has rows under it takes the drop after it on the bottom half of the last of those rows. The other rows are covered. The rows use the rules of the drop itself (`TreeDropService`), so a lit row always takes the drop. A collapsed row with a lit or a neighbor row inside is not covered. A layer row takes an item of one of its groups inside it and passes it to that group, so an entity dropped on a layer goes to its Entities. A row that takes the kind of the dragged row but refuses it by a rule (another layer of the composition also holds the entity, or the owner cannot hold the modifier) is blocked: it keeps its fill, has a thin frame in a muted warning tone, takes no drop, and its tooltip gives the reason.

A row can move inside its list. It can also move to a list of the same kind of another owner: an entity to another layer, a modifier to another entity, a filter, a texture or a modulator to another owner of the same kind of list.

### Adding to the tree

One panel adds things under a row. It has a search field, then the groups that the row can take, in this order: Layer, Content, Texture, Filter, Modifier, Modulator and Project. An entry that the row cannot take is not in the panel, and a group with no entry is not in it either. A layer takes content (entities, fullscreen quads, text, cameras and lights), filters and modifiers. An entity takes textures and modifiers. A group of rows takes only its own kind. An entry has a small square in the color of its kind, and a group does not show a number. A group with many rows starts closed, and the search opens them. A long list has a scroll bar. The menu of a row shows Add, Duplicate, Rename, the moves and Remove with an icon each. A row that cannot do one of them shows it greyed, and a group row shows all of them greyed.

The panel opens in three ways:

- The Add row of the menu of a row. The panel opens to the right of the menu, with its search field on the Add row.
- The Add button of the toolbar over the tree. The panel opens with the middle of its search field under the pointer and names the selected row. A click from the keyboard opens it under the button. With nothing selected, the panel offers a composition.
- The shortcut `Tree.Add` (Shift+A) opens the Add panel of the selected row with the middle of its search field under the pointer, with the title Add. With nothing selected, the panel offers a composition. It works while the pointer is over the tree, whatever has the focus, and does nothing when the pointer is elsewhere. The panel shows every category open: a category is a name, with no expander (`GroupedSearchList.Collapsible`).

The menu of a row opens with its Add row under the pointer. After the user picks another row of a menu, such as Duplicate, the next menu opens with that row under the pointer.

Every panel ends with the group Project, so a composition can be added under any row. The search field has the focus when the panel opens. Down and Up move over the rows, Enter picks a row, and Escape clears the text and then closes the panel.

A tooltip is a dark card with a tip that points at the pointer. It shows a title and, for the buttons of the toolbar, the shortcut. A disabled button says why in its tooltip.

## Keyboard shortcuts

The Studio reads its keyboard shortcuts from `%AppData%\CMiX\studio-shortcuts.json`. This file is separate from `studio-settings.json`. When the file is missing, the Studio writes it with the defaults. When the Studio cannot write it, the Studio starts with the defaults.

The file maps a command name to a gesture:

    {
      "Shortcuts": {
        "Edit.Undo": "Ctrl+Z",
        "Tree.Rename": "F2"
      }
    }

| Name | Default | Action |
| --- | --- | --- |
| `Edit.Undo` | Ctrl+Z | Undo |
| `Edit.Redo` | Ctrl+Shift+Z | Redo |
| `Edit.RedoAlt` | Ctrl+Y | Redo |
| `Tree.Add` | Shift+A | Open the Add panel of the selected row at the pointer |
| `Tree.Menu` | W | Open the menu of the selected row at the pointer |
| `Tree.Expand` | E | Expand or collapse the row that has the keyboard focus, else the selected row |
| `Tree.Rename` | F2 | Rename the selected row |
| `Tree.Remove` | Delete | Remove the selected item |
| `Tree.Duplicate` | Shift+D | Duplicate the selected item |
| `Tree.MoveUp` | Alt+Up | Move the selected item up |
| `Tree.MoveDown` | Alt+Down | Move the selected item down |
| `Tree.Open` | Enter | Select the row that has the keyboard focus |

The part of a name before the dot is its scope. Two names of one scope must not have the same gesture. An entry with an unknown name, or with a gesture that does not parse, is ignored and the default stays. An empty gesture removes the shortcut.

`Tree.Add`, `Tree.Menu` and `Tree.Expand` work while the pointer is over the tree, whatever has the focus, but not while a text box has the focus. The file holds every default since its first save, so a default of an earlier version that is still in the file gives the default of now: a `Tree.Duplicate` of Ctrl+D becomes Shift+D.

The Shortcuts section of the Settings tab edits the shortcuts and saves each change to the file at once, and a missing file is created on save. A red dot marks a gesture that two commands of one scope have, and its tooltip names the other command.

While a text box has the focus, the text box gets the keys first. In the tree, the shortcuts do nothing while a name is edited.

In the tree, the arrow keys, Page Up, Page Down, Home and End move the keyboard focus from row to row. Left collapses a row or goes to its parent. Right expands a row or goes to its first child. The row with the focus has a thin border in the bright color of its kind. The keyboard focus does not select the row: it writes no undo step and sends nothing to the peers. Enter or a click selects the row.

## Redis addresses

The Settings tab holds the list of Redis addresses of the project. Click an address to edit it. The buttons at the side add, remove and move addresses. The project file stores the list.

The Redis Float modulator selects one address of the list in its combo box. It has one float output, `Value`. Assign a value to it in the same way as to any other modulator output. The row of the assign menu shows the address text, or `Value` when the modulator selects nothing. A change of the selection keeps the assignments. The Reset menu of the combo box clears the selection. When you delete the selected address, the selection clears and the values bound to the modulator go back to normal in the same undo step.

Each Redis Float modulator has one range with the start values 0 and 1. The vvvv side maps the Redis value of the selected address into the range. The range stays when you select another address.

The Redis Bool modulator selects an address in the same way. It has one bool output, `Value`, for a bool such as the Play or the Do Seek of a player. It has no gain. The vvvv side decides how the Redis value becomes a bool.

The Redis Int modulator selects an address in the same way. It has one int output, `Value`, for a whole number such as the Seek Target of a player or the Count of a modifier. It has no gain. The vvvv side decides how the Redis value becomes an int.

The list has a fixed manager ID, `ManagerIDs.RedisAddressManager`. The vvvv side creates the list at startup with this ID.

## FFT modulator

The FFT modulator reads one track of the FFT signal. The track index selects the track. It has four float outputs, `Bass`, `LowerMid`, `HigherMid` and `High`. Each band has one range with the start values 0 and 1. The vvvv side maps the band value of the track into the range of the band.

## Hidden types

A class with the attribute `[HiddenInMenu]` is not in the add menus. A saved project can still hold it, and it still works. To show the class again, remove the attribute.

These classes are hidden now: `RenderRandomEntityModifier`, `RenderSequenceEntityModifier` and `BillboardModifier`.

## License

CMiX is free software under the [GNU LGPL, version 3 or later](LICENSE). The GPL text is in [COPYING](COPYING).

## Credits

Created by CloneProduction

Sponsored by Refik Anadol Studio
