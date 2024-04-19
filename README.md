# SoCreator

[![Twitter](https://img.shields.io/badge/Follow-Twitter?logo=twitter&color=white)](https://twitter.com/NullTale)
[![Discord](https://img.shields.io/badge/Discord-Discord?logo=discord&color=white)](https://discord.gg/CkdQvtA5un)
[![Boosty](https://img.shields.io/badge/Support-Boosty?logo=boosty&color=white)](https://boosty.to/nulltale)
[![Forum](https://img.shields.io/badge/Forum-asd?logo=ChatBot&color=blue)](https://forum.unity.com/threads/1351019/)
[![Asset Store](https://img.shields.io/badge/Asset%20Store-asd?logo=Unity&color=red)](https://assetstore.unity.com/packages/tools/utilities/228650)

Quick access menu for ScriptableObjects creation by name.

- Project specific search assemblies
- Attribute object marking
- Organization folders
- Shortcuts

## Installation
Install from Package Manager git url 
```
https://github.com/NullTale/SoCreator.git
```

![image](https://user-images.githubusercontent.com/1497430/181345613-b81a77c6-c449-4b19-ab1e-88b1ef06f6fc.png)

## Usage

Create -> ScriptableObject or `Shift + I`<br>
> Can be configured in detail in the Preferences window

![out](https://user-images.githubusercontent.com/1497430/191845515-311216d0-57c3-4294-8b69-0bf226fab911.gif)

> If shift key held when opening creation menu scriptable objects from all assemblies will be searched.<br>

Classes can be marked with `SoCreateAttribute` to manually define their visibility.

For each project, manually can be defined assemblies in which extra search will be performed by default.<br>
Default folders can be specified for each ScriptableObject type. If such object or its derivative is created using a hotkey, it will automatically placed in the specified folder.

![image](https://cdn.discordapp.com/attachments/934699103462494220/1081210636089970728/SoC.png)
