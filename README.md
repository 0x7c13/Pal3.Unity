<p align="center">
  <img width="128" align="center" src="Assets/Resources/UI/game-icon-PAL3.png">
  +
  <img width="128" align="center" src="Assets/Resources/UI/game-icon-PAL3A.png">
</p>
<h1 align="center">
  Pal3.Unity
</h1>
<p align="center">
  <<仙剑奇侠传三>>和<<仙剑奇侠传三外传：问情篇>>C#/Unity实现
</p>
<p align="center">
  简体中文 | <a href="README.en-US.md">English</a>
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

# 简介
这是一个使用C#/Unity重新实现的仙剑奇侠传三以及仙剑奇侠传三外传的开源项目（注意：不是移植，是重写）。实现方式为重写游戏逻辑和加载读取利用原始游戏资源并按照原始游戏的运行方式和效果进行的重新实现，具体实现方法请阅读源代码。

注意：仙剑奇侠传三以及仙剑奇侠传三外传属于原上海软星作品，版权属于软星科技以及大宇公司所有，本项目不包含任何仙剑奇侠传三以及仙剑奇侠传三外传的游戏本体数据。本项目遵循GPL-3.0协议，但仅限于此项目的代码，任何与仙剑奇侠传三或者外传有关的图片，音视频，游戏数据均不在此范围，任何未经版权方许可的情况下使用仙剑奇侠传三或者外传游戏数据进行商业行为都是违法的。

## 最新 PR 编译状态

CI 仅在 Pull Request 上运行；以下徽章显示各工作流最近一次 PR 构建的状态。点击徽章可查看对应的 PR 构建记录。

| 游戏版本                 | Windows | Linux | MacOS | Android | iOS |
|-------------------------|---------|-------|-------|---------|-----|
| 仙剑奇侠传三 | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3-mono-windows.yml?query=event%3Apull_request"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3-mono-windows.yml?event=pull_request" alt="PR build status" /></a> | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3-mono-linux.yml?query=event%3Apull_request"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3-mono-linux.yml?event=pull_request" alt="PR build status" /></a> | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3-mono-macos.yml?query=event%3Apull_request"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3-mono-macos.yml?event=pull_request" alt="PR build status" /></a> | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3-mono-android.yml?query=event%3Apull_request"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3-mono-android.yml?event=pull_request" alt="PR build status" /></a> | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3-mono-ios.yml?query=event%3Apull_request"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3-mono-ios.yml?event=pull_request" alt="PR build status" /></a> |
| 仙剑奇侠传三外传：问情篇 | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3a-mono-windows.yml?query=event%3Apull_request"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3a-mono-windows.yml?event=pull_request" alt="PR build status" /></a> | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3a-mono-linux.yml?query=event%3Apull_request"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3a-mono-linux.yml?event=pull_request" alt="PR build status" /></a> | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3a-mono-macos.yml?query=event%3Apull_request"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3a-mono-macos.yml?event=pull_request" alt="PR build status" /></a> | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3a-mono-android.yml?query=event%3Apull_request"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3a-mono-android.yml?event=pull_request" alt="PR build status" /></a> | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3a-mono-ios.yml?query=event%3Apull_request"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3a-mono-ios.yml?event=pull_request" alt="PR build status" /></a> |

## 如何运行项目
使用 [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt) 指定的 Unity 版本打开项目。项目使用 URP Forward+。
因为项目本身不含有仙剑奇侠传三或仙剑奇侠传三外传的游戏数据，所以你需要持有一份仙剑三或外传的游戏原始文件（Steam或者方块游戏获得皆可）。
  - 第一次打开 Unity 项目之后，双击选择 `Scenes/Game` 作为当前场景，然后点播放键即可。浏览资源使用菜单 `PAL3 / PAL3A → Resource Browser`，无需进入 Play Mode。
  - 第一次打开的时候会自动弹出文件夹选择窗口，请选择当前电脑上仙剑奇侠传三（或者外传）的安装文件夹即可。
  - 如果你在Steam获取的游戏是繁体中文版的，游戏中的文字会出现乱码，请在游戏设置里选择繁体中文版，然后重启游戏后即可生效。
  - 因为原始游戏的过场动画为Bink格式，Unity并不原生支持，所以请自行转码视频为Unity所支持的格式放在游戏根目录下的movie文件夹即可（大部分设备和系统支持.mp4等主流格式视频，Linux下仅支持.webm格式视频）。
  - Linux 用户可以使用 FFmpeg 将视频转码为 .webm。先打开 `Resource Browser`，选择原版安装目录并索引，搜索 `ext:bik`，使用 `Original → Export filtered…` 提取视频。超过 64 MiB 的归档资源需用 `Export original…` 单独确认提取。输出保留归档内层级。
    ```
    ffmpeg -i input.mp4 -c:v libvpx -b:v 3M -c:a libvorbis output.webm  // vp8 + vorbis
    ```

## 编辑器资源浏览

菜单 `PAL3 / PAL3A → Resource Browser` 打开可停靠的原生编辑器窗口。选择直接包含 `basedata/` 的数据目录与 936（简体）或 950（繁体）编码，再点击 `Index / Reload`。切换作品使用 `Switch Variant`。

数据目录及其父目录不能含符号链接，请选择链接的真实目标路径；索引也会跳过目录内的符号链接。此限制用于确保导出不会通过路径别名写入原始数据目录。

- 搜索支持路径多词、`-排除词`、`ext:dds`，可按资源类别与归档筛选，上下键选择结果。
- 贴图使用 Unity 原生预览与通道检查，可导出 PNG；模型提供独立视口、原版／Toon、动作选择、播放／时间轴及贴图依赖跳转。
- SCN 检查场景／NPC／物件并跳转关联资源，可预览静态背景与位置标记；NAV 显示分层通行网格和格子数据；LGT 检查光源字段。
- SCE 按块查看指令、变量与对话；GDB 按物品、角色、技能、组合技检索。已解析资源可查看字段和导出 JSON，脚本支持批量导出指令清单或对话。
- 文本支持编码识别、查找、Undo 和另存副本；音频使用原生预览；视频使用当前平台的 Unity 解码器，BIK 仅支持提取；未知资源提供分页十六进制。

原始数据只读，导出保留目录层级且不覆盖已有文件。选择 `Original / Script / Dialogue / Json` 后可批量导出筛选结果，并随时取消。普通预览限制 64 MiB，文本读取限制 4 MiB、窗口编辑限制 60,000 字符。媒体临时提取位于工程相邻的 `workspace/ResourceBrowserMedia/`，换资源或关闭窗口后清理；重新打开窗口时也会清理已退出进程留下的临时目录。特效播放和 override 编辑继续使用 `Effect Editor`。原 `ResourceViewer` 场景已移除。

## 如何在终端设备上运行游戏
  - 在打包后的运行时，第一次游戏启动的时候，游戏会搜索 [Application.persistentDataPath](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-persistentDataPath.html) 目录或者 [StreamingAssets](https://docs.unity3d.com/6000.0/Documentation/Manual/StreamingAssets.html) 目录来检测游戏数据是否存在。在桌面版本的游戏中，如果未能检测到原始游戏的数据文件，游戏则会弹出文件夹选择窗口，请选择当前设备上原游戏安装文件的根目录即可。
  - 如果上一步未成功，请拷贝整个仙剑奇侠三（或者外传）原游戏目录到 [Application.persistentDataPath](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-persistentDataPath.html) 下即可。注意：如果自行拷贝游戏数据至这个目录的话，如果是仙剑三，文件夹的名称必须为PAL3，如果是仙剑三外传，文件夹名必须为PAL3A。iOS设备请使用iTunes将文件夹共享给游戏应用（这个位置就是persistentDataPath所在的地方）或者在编译时自行打包游戏原始数据到StreamingAssets文件夹下也可。

## 如何切换仙剑奇侠传三/仙剑奇侠传三外传
两部游戏版本在项目中的切换是靠 [Custom scripting symbols](https://docs.unity3d.com/6000.0/Documentation/Manual/CustomScriptingSymbols.html) 实现的，PAL3对应仙剑奇侠传三，PAL3A对应仙剑奇侠传三外传。我在Unity编辑器菜单栏里面加了一个快捷按钮可以一键切换（菜单栏->Pal3->Switch Variant->PAL3A），切换之后对应的游戏文件夹也自动的从PAL3转换成PAL3A。切换的过程中，包括Symbol，图标和应用程序名在内的所有名称和标示都会由PAL3变为PAL3A。

## 旧版卡通渲染截图
![PAL3 卡通渲染+光影](Screenshots/PAL3_ToonShading_Lighting.png?raw=true)
![PAL3A 卡通渲染+光影](Screenshots/PAL3A_ToonShading_Lighting.png?raw=true)
![PAL3 卡通渲染+光追](Screenshots/PAL3_ToonShading_RayTracing.png?raw=true)

## 关于特效和卡通渲染
两作都默认使用 URP 原版画风，保留原作贴图、烘焙光照和透明混合。可在「游戏设置 → 画风」选择「卡通与实时光影」，重启后生效。两档都使用仓库内的 shader，运行时从玩家本地读取原版游戏资源。

「游戏设置 → 画面与特效设置」提供接触阴影、角色光照、增强夜景火光和火光投影四个独立开关，在卡通画风中即时生效。接触阴影和火光投影在桌面设备默认开启，手持设备可按需开启。夜景火光使用暖色灯光与冷色环境底色。火光投影也适用于暗色迷宫，桌面默认四盏，控制台可通过 `Settings.ToonFireShadowBudget` 设置 0–4 盏，阴影图集随预算扩容。泛光可以独立开关，默认仅桌面原版画风开启。

上方截图来自旧版卡通渲染。两作已支持的剧情与场景特效由原版数据驱动，运行时读取本地 CPK 的特效组、任务、粒子、贴图和模型，使用自有 URP shader 与 Unity 原生渲染组件。Unity 菜单 `PAL3 / PAL3A → Effect Editor` 可选择作品、播放、检查对象、修改组任务和旧粒子曲线；覆盖资产分别保存到 `Assets/Resources/Effects/PAL3Overrides.asset` 或 `PAL3AOverrides.asset` 后供游戏读取。资源浏览器的特效视图可直接打开 Effect Editor；组 ID `-1` 支持 PAL3／PAL3A 两作传送点。

PAL3 已覆盖当前清单中的 56 个剧情／场景根组，PAL3A 已覆盖 93 个，均由原版数据驱动。传送点按原版圆盘、双光锥及场景参数配色播放。SPar 关键帧、路径、六槽控制器和材质混合独立实现，原始 SPar 字段在编辑器中只读。尚未实现的战斗导演等类型会给出诊断，留待对应系统开发时补齐。

实现所需的代码、Shader 和构建资源均随本仓库提供。运行时读取玩家本地的原版游戏数据；原版数据不随项目分发。

## 按键以及操作
  - 鼠标键盘：鼠标左键点击操作人物行走方向（键盘方向键也可以），AD控制镜头旋转，空格与周边附近的物品或者NPC交互（也是跳跃键），M键打开大地图，U键或ESC键打开剧情选择菜单，Tab键切换角色（迷宫中）。
  - 手柄：左摇杆控制人物行走，右摇杆控制镜头旋转，A键交互，菜单和选择按钮对应大地图和剧情选择菜单，LB/RB切换上一个/下一个角色（迷宫中）。
  - 触屏：仅在有触摸屏的手持设备上才会启用，虚拟摇杆控制行走，交互键互动。

## 项目进度以及路线图
仙剑奇侠传三以及仙剑奇侠传三外传的剧情部分已经全部完成，两部游戏都可以完整的从头玩到尾体验一遍剧情，也可以使用游戏内提供的剧情选择菜单跳转至预设好的剧情时间点。两部游戏中的迷宫机关也基本全部实现，剧情中出现的大部分特效也已经重新实现。但是战斗，小游戏以及围绕支持战斗系统的各类系统和功能均尚未完成。另外主菜单和大部分界面也需要重新设计和实现（毕竟现在还要做手机端的适配）。

## 如何贡献？
修改自有 C# 代码后，使用 `global.json` 指定的 .NET SDK，在工程根目录执行：

```sh
python3 tools/format-csharp.py
python3 tools/format-csharp.py --check
```

工具使用 SDK 自带的 `dotnet format`、`.editorconfig` 和固定版本的 StyleCop 布局规则，检查空格、缩进、函数／成员间空行及多条语句分行。默认只应用改动行及删除边界上的格式修正，保留其他行的既有格式；显式路径或 `--all` 可检查整个文件。工具分别检查 PAL3／PAL3A 条件分支，不需要 Unity 或游戏数据；StyleCop 仅用于开发工具，不进入 Unity 工程。PR 的 `Code style` 工作流执行相同检查。格式检查通过后仍需运行相关 Unity 测试；ShaderLab／HLSL 由人工整理并验证。

因为项目还处于早期实现过程中，很多系统还没有实现，暂时不接受比较大的Pull request，特别是feature类型，如果您有好的想法，意见或者发现了Bug请欢迎提交issue或者加入交流群与我讨论。
另外您还可以参考这个项目解析视频：https://www.bilibili.com/video/BV1Pr4y167sF

## 为什么要做这个？[TL;DR]
学了几周Unity之后，一直想找项目练手，一开始自己尝试做了一下新仙剑的第一关，大概了解了RPG游戏的制作流程，后因机缘巧合发现了zby大佬的[PAL3patch](https://github.com/zhangboyang/PAL3patch)项目以及dontpanic92大佬的[OpenPAL3](https://github.com/dontpanic92/OpenPAL3)项目，所以给技术上实现仙三复刻带来了可能。至于为什么选择C#/Unity？选择Unity的原因有两点，第一是Unity对全平台全端的支持和打包做的很好很方便，第二个是Unity提供Mac上arm64原生IDE支持（我大概有一半时间是在Mac上写代码的）。当然其实本项目的实现几乎没有用到Unity引擎的大部分功能，特别是编辑器功能，因为几乎是100%纯代码实现的，所以其实理论上不需要花太大的代价就可以把本项目移植到其他支持C#脚本的引擎中。

## 社区
  - 仙剑三（及外传）复刻版讨论群：252315306
  - 仙剑三（及外传）复刻版开发群：330680869

## 项目依赖
  - [UnitySimpleFileBrowser](https://github.com/yasirkula/UnitySimpleFileBrowser)
  - [UnityIngameDebugConsole](https://github.com/yasirkula/UnityIngameDebugConsole)
  - [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity)
  - [ini-parser](https://github.com/rickyah/ini-parser)
  - [Json.NET](https://www.newtonsoft.com/json)

