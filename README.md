# CMiX

CMiX is a realtime graphic composition tool.

## Requirements

- Windows and .NET 8
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

The file `version.txt` holds the version of the zip, the package and the executable. The package installs the Studio into `C:\tools\CMiX.Studio` and adds a desktop shortcut. The Studio needs the .NET 8 runtime (package `dotnet-8.0-runtime`). An upgrade replaces `studio-settings.json` in that folder with the shipped file.

## Project files

The Studio saves a project as a `.cmix` file. The file stores the path of every asset relative to its own folder. A project and its assets move together to another machine when the folders between them stay the same. This example has a project and an asset:

    VJ\compositions\bar.cmix
    VJ\textures\foo.jpg

The file stores the asset as `..\textures\foo.jpg`.

An asset on another drive than the project file keeps its absolute path. A project file from an older version keeps its absolute paths until the next save.

The emergency save in the temp folder holds paths relative to that folder. Open it from there and the paths work. Copy the file to another folder before you open it and the paths break.

The start check reports "Not in sync" when you open a project on a machine other than the one that pushed the Redis snapshot, because the absolute paths differ. Click Push on that machine.

## Project tree

The tree shows the project as rows. A row is a composition, a layer, an item of a layer, or a group of items. An item can hold textures, filters, modifiers and modulators. They are groups under the item.

Each row has a kind: composable (a composition and a layer), entity, camera, light, texture, filter, modifier or modulator. A group has the kind of its items. The kind sets the color of the row.

- The colors of the kinds are in `Colors.axaml`, with the names `Category` and the kind. A composable inside a composable has one more color for each level, `CategoryComposableLevel1` to `CategoryComposableLevel3`.
- A row is filled with its color mixed with the gray of the panel. The fill is one step stronger under the pointer, and more when the row is selected. A selected row also has a border of one pixel of the screen in the bright color of its kind, and its name is white. A group and its items have the same tone. What a row holds of another kind is weaker by one step for each change of kind (`TreeNode.KindDepth`), down to a floor. `CategoryPalette` holds the strength of each step, and a test checks the contrast of the text on every kind, level and state.
- The fill of a row starts at its indent, so no fill touches the left border of the tree. The space between two rows is 3 pixels (`ProjectTreeView.RowGap`). The space between a row and its toggles stays 1 pixel.
- A collapsed row shows what it holds: a small square for each kind, in the color of the kind, with the number of items. An open row shows none.
- The toggles are at the right of the colored row. The visibility toggle is the first square. The mask toggle follows it on a composition and a layer. A new toggle is a new square in `RowToggles`.
- The title bar of the inspector shows the icon of the selected row on a square in the color of its kind (`CategoryBadge`).

While you drag a row, the rows that take it inside are lit, with no border: for a modifier, the group Modifiers of its own entity and of the other entities, for a filter the groups Filters, and for a layer the compositions. A row dropped on the group of its own list goes to the end of the list. The row under the pointer is lit more. The rows next to which the row can go keep their look, and the drop line shows the place: at the top of a row for a drop before it, and at the bottom of the block of a row for a drop after it. A row that has rows under it takes the drop after it on the bottom half of the last of those rows. The other rows are covered. The rows use the rules of the drop itself (`TreeDropService`), so a lit row always takes the drop. A collapsed row with a lit or a neighbor row inside is not covered.

A row can move inside its list. It can also move to a list of the same kind of another owner: an entity to another layer, a modifier to another entity, a filter, a texture or a modulator to another owner of the same kind of list.

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
