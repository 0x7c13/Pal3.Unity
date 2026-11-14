<p align="center">
  <img width="128" align="center" src="Assets/Resources/UI/game-icon-PAL3.png">
  +
  <img width="128" align="center" src="Assets/Resources/UI/game-icon-PAL3A.png">
</p>
<h1 align="center">
  Pal3.Unity
</h1>
<p align="center">
  The Legend of Sword and Fairy 3 & The Legend of Sword and Fairy 3 Gaiden: Wenqing Pian re-implementation using C#/Unity
</p>
<p align="center">
  <a href="README.md">简体中文</a> | English
</p>
<p align="center">
  <a style="text-decoration:none">
    <img src="https://img.shields.io/badge/unity-6-blue?style=flat-square" alt="Unity Version" />
  </a>
  <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/releases">
    <img src="https://img.shields.io/github/v/release/0x7c13/Pal3.Unity.svg?label=alpha&style=flat-square&color=yellow" alt="Releases" />
  </a>
  <a style="text-decoration:none">
    <img src="https://img.shields.io/badge/platform-Linux%20%7C%20Win%20%7C%20Mac%20%7C%20iOS%20%7C%20Android-orange?style=flat-square" alt="Platform" />
  </a>
  <a style="text-decoration:none">
    <img src="https://img.shields.io/badge/license-GPL--3.0-green?style=flat-square" alt="License" />
  </a>
  <a style="text-decoration:none">
    <img src="https://img.shields.io/github/repo-size/jasonstein/pal3.unity?style=flat-square" alt="Size" />
  </a>
</p>

# Intro
This is an open-source project that re-implements [The Legend of Sword and Fairy 3](https://en.wikipedia.org/wiki/Chinese_Paladin_3) and [The Legend of Sword and Fairy 3 Gaiden: Wenqing Pian](https://en.wikipedia.org/wiki/Chinese_Paladin_3_Gaiden:_Wenqing_Pian) using C#/Unity. The implementation involves rewriting of the game logic, loading and utilizing the original game assets, and recreating the original game's behavior and effects. Please refer to the source code to see how it is been made.

Note: The Legend of Sword and Fairy 3 and The Legend of Sword and Fairy 3 Gaiden: Wenqing Pian are original works of Softstar Technology (Shanghai) Co., Ltd., with copyrights belonging to Softstar Technology (Shanghai) Co., Ltd. and Softstar International Inc. (I might be wrong but please verify yourself). This project does not contain any game data from the original game. The project is under the GPL-3.0 license but is limited to the code of this project excluding Unity assets and other assets and plugins that I used and get from Unity Asset Store. Any textures, audio, video, or game data related to The Legend of Sword and Fairy 3 or The Legend of Sword and Fairy 3 Gaiden: Wenqing Pian are not covered by this license.

## Latest PR build status

CI runs only on pull requests. Each badge shows the latest pull request build for that workflow; click it to view the corresponding PR build history.

| Game Variant            | Windows | Linux | MacOS | Android | iOS |
|-------------------------|---------|-------|-------|---------|-----|
| The Legend of Sword and Fairy 3  | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3-mono-windows.yml?query=event%3Apull_request"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3-mono-windows.yml?event=pull_request" alt="PR build status" /></a> | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3-mono-linux.yml?query=event%3Apull_request"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3-mono-linux.yml?event=pull_request" alt="PR build status" /></a> | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3-mono-macos.yml?query=event%3Apull_request"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3-mono-macos.yml?event=pull_request" alt="PR build status" /></a> | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3-mono-android.yml?query=event%3Apull_request"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3-mono-android.yml?event=pull_request" alt="PR build status" /></a> | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3-mono-ios.yml?query=event%3Apull_request"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3-mono-ios.yml?event=pull_request" alt="PR build status" /></a> |
| The Legend of Sword and Fairy 3 Gaiden: Wenqing Pian | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3a-mono-windows.yml?query=event%3Apull_request"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3a-mono-windows.yml?event=pull_request" alt="PR build status" /></a> | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3a-mono-linux.yml?query=event%3Apull_request"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3a-mono-linux.yml?event=pull_request" alt="PR build status" /></a> | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3a-mono-macos.yml?query=event%3Apull_request"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3a-mono-macos.yml?event=pull_request" alt="PR build status" /></a> | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3a-mono-android.yml?query=event%3Apull_request"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3a-mono-android.yml?event=pull_request" alt="PR build status" /></a> | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3a-mono-ios.yml?query=event%3Apull_request"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3a-mono-ios.yml?event=pull_request" alt="PR build status" /></a> |

## How to run the project
Open the project with the Unity version specified in [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt). The project uses URP Forward+.
  - Since the project itself does not contain any game data from the original game, you will need a copy of the original game files for The Legend of Sword and Fairy 3 or The Legend of Sword and Fairy 3 Gaiden: Wenqing Pian (Link to the game on Steam: [The Legend of Sword and Fairy 3](https://store.steampowered.com/app/1536070) and [The Legend of Sword and Fairy 3 Gaiden: Wenqing Pian](https://store.steampowered.com/app/1536080)).

  - Open `Scenes/Game` and press Play to run the game. Use `PAL3 / PAL3A → Resource Browser` to browse resources in Edit Mode.
  - The first time you open it, a folder selection window will automatically pop up. Please select the installation folder for The Legend of Sword and Fairy 3 (or The Legend of Sword and Fairy 3 Gaiden: Wenqing Pian) on your computer.
  - Since the original game's cutscene videos are in Bink format, which Unity does not natively support, please convert the videos to a format supported by Unity and place them in the movie folder at the game's root directory (most devices and platforms support mainstream formats like .mp4, while Linux only supports .webm format).
  - Linux users can use FFmpeg to convert videos to .webm format
    ```
    ffmpeg -i input.mp4 -c:v libvpx -b:v 3M -c:a libvorbis output.webm  // vp8 + vorbis
    ```
    To extract the original videos, open `Resource Browser`, index the original installation directory, search `ext:bik`, and choose `Original → Export filtered…`. Archive entries larger than 64 MiB require individual confirmation with `Export original…`. The archive directory structure is preserved.

## Editor resource browser

Open the dockable window through `PAL3 / PAL3A → Resource Browser`. Choose the data directory containing `basedata/`, select GBK (936) or Big5 (950), and click `Index / Reload`. Use `Switch Variant` to change games.

The data directory and its ancestors must not be symbolic links; select the link's real target path instead. Indexing also skips links inside the directory. This restriction keeps path aliases from bypassing original-data export protection.

- Search paths with multiple terms, `-exclusions`, or `ext:dds`; filter by category/archive and navigate results with arrow keys.
- Textures use Unity's native preview and channel controls, with PNG export. Models offer an isolated viewport, original/Toon rendering, animation playback and seeking, and texture dependency links.
- SCN provides scene/NPC/object records, dependency links, and a static background with position markers. NAV provides layered walkability maps and tile details; LGT exposes light records.
- SCE shows script blocks, commands, variables and dialogue. GDB provides searchable item, actor, skill and combo tables. Parsed resources support field inspection and JSON export; scripts support batch listings and dialogue export.
- Text supports encoding detection, search, Undo and saving a copy. Audio uses native preview controls. Video uses Unity's platform decoder; BIK is extraction only. Unknown resources have a paged hex view.

Original data is read-only. Exports preserve hierarchy and never overwrite existing files. Choose `Original / Script / Dialogue / Json` before exporting filtered results; batch export is cancellable. Ordinary previews are limited to 64 MiB, text reads to 4 MiB, and in-window editing to 60,000 characters. Temporary media is extracted under `workspace/ResourceBrowserMedia/` beside the project and cleaned up when switching resources or closing the window. Opening the window also removes temporary directories left by exited processes. Effect playback and override editing remain in `Effect Editor`. The old `ResourceViewer` scene has been removed.

## How to run the game
  - In runtime, when the game is launched for the first time, it will search the [Application.persistentDataPath](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-persistentDataPath.html) directory or the [StreamingAssets](https://docs.unity3d.com/6000.0/Documentation/Manual/StreamingAssets.html) directory to detect whether the game data exists. If the original game data files cannot be detected on desktop clients, the game will pop up a folder selection window for you to select the root directory of the original game installation files on the current device.
  - If the previous step is unsuccessful, please copy the entire "The Legend of Sword and Fairy 3" (or "The Legend of Sword and Fairy 3 Gaiden: Wenqing Pian") original game installation directory to [Application.persistentDataPath](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-persistentDataPath.html). Note: If you manually copy the game data to this directory, the folder name must be PAL3 for "The Legend of Sword and Fairy 3," and PAL3A for The Legend of Sword and Fairy 3 Gaiden: Wenqing Pian. For iOS devices, please use iTunes to share the folder with the game application (this location is where the persistentDataPath is located), or you can package the original game data into the StreamingAssets folder during compilation with Unity, so you don't have to pick a folder when deployed.

## How to switch between two variants of the game
The version switching is implemented through [Custom scripting symbols](https://docs.unity3d.com/6000.0/Documentation/Manual/CustomScriptingSymbols.html). PAL3 corresponds to The Legend of Sword and Fairy 3, and PAL3A corresponds to The Legend of Sword and Fairy 3 Gaiden: Wenqing Pian. I added a shortcut button in the Unity editor menu bar for one-click switching (Menu bar->Pal3->Switch Variant->PAL3A). After switching, the corresponding game folder will automatically change from PAL3 to PAL3A.

## Screenshots with realtime lighting and toon shading enabled
![PAL3 卡通渲染+光影](Screenshots/PAL3_ToonShading_Lighting.png?raw=true)
![PAL3A 卡通渲染+光影](Screenshots/PAL3A_ToonShading_Lighting.png?raw=true)
![PAL3 卡通渲染+光追](Screenshots/PAL3_ToonShading_RayTracing.png?raw=true)

## About VFX and Toon Rendering
Both games default to the original visual style in URP, preserving the original textures, baked lighting, and transparency. Choose **Toon with real-time lighting** under the visual style option in game settings and restart to apply it. Both styles use shaders from this repository and load game data from the player’s local installation.

The visual effects settings provide independent switches for contact shadows, character lighting, enhanced night fire lighting, and fire shadows. Contact shadows and fire shadows default to enabled on desktop devices. Enhanced fire lighting combines warm lights with a cool ambient tone. Fire shadows also work in dark maze scenes. Desktop defaults to four shadowed fires. Set `Settings.ToonFireShadowBudget` to 0–4 in the debug console. The shadow atlas grows with the budget. Bloom has a separate switch and defaults to enabled only for the original style on desktop.

The screenshots above show the previous toon renderer. Supported story and scene effects in both games use the original data: effect groups, tasks, particles, textures, and models loaded from local CPK archives, rendered with this project's URP shaders and Unity components. Open `PAL3 / PAL3A → Effect Editor` in Unity to select a game, play effects, inspect objects, and edit group tasks and legacy particle curves. Save overrides to `Assets/Resources/Effects/PAL3Overrides.asset` or `PAL3AOverrides.asset` for use in the game. The resource browser links effect resources to Effect Editor. Group ID `-1` supports transport points in both games.

The current inventory covers all 56 PAL3 and 93 PAL3A story and scene root groups using original data. Transport points reproduce the original disc, two light cones, and colors selected by scene parameters. SPar keyframes, paths, six controller slots, and material blending are implemented independently; raw SPar fields are read-only in the editor. Unsupported types such as battle directors produce diagnostics and will be added alongside their related systems.

All implementation code, shaders, and build resources are included in this repository. The game reads the player's local original game data at runtime; that data is not distributed with this project.

## Controls
  - Mouse and keyboard: Left mouse button to click and control character movement (arrow keys can also be used), A/D to control camera rotation, Spacebar to interact with nearby items or NPCs (also used as the jump button when available), M key to toggle the big map, U key or ESC key to open the story selection menu, Tab key to switch characters (in the maze).
  - Gamepad: Left joystick to control character movement, right joystick to control camera rotation, use gamepad south button to interact, menu and select buttons corresponding to the map and story selection menu, LB/RB to switch to the previous/next character (in the maze).
  - Touchscreen: Only enabled on handheld devices with a touch screen, virtual joystick for movement, interaction button for interaction.

## Project State and Roadmap
The story parts of The Legend of Sword and Fairy 3 and The Legend of Sword and Fairy 3 Gaiden: Wenqing Pian are complete. Both games can be played entirely from start to finish to experience the story, and the story selection menu provided in the game can be used to jump to different story points in the game. Most of the switches and puzzles in the mazes in both games have also been implemented, but combat, mini-games, and various systems and features related to supporting the combat system have not been implemented or realized. In addition, the main menu and most of the UI/UX need to be redesigned and implemented as well.

## How to Contribute?
Since the project is still in the early stages, many systems have not yet been implemented, thus I am temporarily not accepting large Pull Requests, especially feature ones. If you have good ideas, suggestions, or have discovered bugs, please feel free to submit an issue.

## Why make this [TL;DR]
After learning Unity for a few weeks, I've been looking for a project to practice. As I surfing the Internet, I discovered the [PAL3patch](https://github.com/zhangboyang/PAL3patch) project created by zby and the [OpenPAL3](https://github.com/dontpanic92/OpenPAL3) project created by dontpanic92, which made the re-implementation of a remake for The Legend of Sword and Fairy 3 possible. As for why I chose C#/Unity, there are two reasons: First, Unity has excellent cross-platform support and packaging, and second, Unity provides native IDE support for Mac with M1/M2 chips (Why? I spend about half of my time coding on a Macbook). Although this project barely uses most of Unity's engine features, especially its editor features, since it's almost 100% code-based, it could theoretically be ported to other engines that support C# scripting with minimal effort.

## Dependencies
  - [UnitySimpleFileBrowser](https://github.com/yasirkula/UnitySimpleFileBrowser)
  - [UnityIngameDebugConsole](https://github.com/yasirkula/UnityIngameDebugConsole)
  - [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity)
  - [ini-parser](https://github.com/rickyah/ini-parser)
  - [Json.NET](https://www.newtonsoft.com/json)


