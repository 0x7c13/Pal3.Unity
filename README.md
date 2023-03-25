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
    <img src="https://img.shields.io/badge/unity-2022.2.12-blue?style=flat-square" alt="Unity Version" />
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

## 主分支当前编译状态
| 游戏版本                 | Windows | Linux | MacOS | Android | iOS |
|-------------------------|---------|-------|-------|---------|-----|
| 仙剑奇侠传三 | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3-mono-windows.yml"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3-mono-windows.yml" alt="Size" /></a> | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3-mono-linux.yml"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3-mono-linux.yml" alt="Size" /></a> | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3-mono-macos.yml"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3-mono-macos.yml" alt="Size" /></a> | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3-mono-android.yml"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3-mono-android.yml" alt="Size" /></a> | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3-mono-ios.yml"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3-mono-ios.yml" alt="Size" /></a> |
| 仙剑奇侠传三外传：问情篇 | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3a-mono-windows.yml"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3a-mono-windows.yml" alt="Size" /></a> | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3a-mono-linux.yml"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3a-mono-linux.yml" alt="Size" /></a> | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3a-mono-macos.yml"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3a-mono-macos.yml" alt="Size" /></a> | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3a-mono-android.yml"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3a-mono-android.yml" alt="Size" /></a> | <a style="text-decoration:none" href="https://github.com/0x7c13/Pal3.Unity/actions/workflows/build-pal3a-mono-ios.yml"><img src="https://img.shields.io/github/actions/workflow/status/0x7c13/Pal3.Unity/build-pal3a-mono-ios.yml" alt="Size" /></a> |

## 如何运行项目
使用 Unity 2022.2.X 打开当前项目文件夹即可，具体当前项目所需要的Unity版本请查看上面的Badge显示的版本，理论上任何Unity 2022.2+版本都没问题。
因为项目本身不含有仙剑奇侠传三或仙剑奇侠传三外传的游戏数据，所以你需要持有一份仙剑三或外传的游戏原始文件（Steam或者方块游戏获得皆可，注意：暂时仅支持简体版游戏）。
- 第一次打开Unity项目之后，先双击选择Scenes\Game作为当前场景，然后点播放键即可。如果选择Scenes\ResourceViewer，则会打开游戏资源查看器。
- 第一次打开的时候会自动弹出文件夹选择窗口，请选择当前电脑上仙剑奇侠传三（或者外传）的安装文件夹即可。
- 因为原始游戏的过场动画为Bink格式，Unity并不原生支持，所以请自行转码视频为Unity所支持的格式放在游戏根目录下的movie文件夹即可（大部分设备和系统支持.mp4等主流格式视频，Linux下仅支持.webm格式视频）。
* Linux用户可以使用FFmpeg转码视频为.webm格式封装，首先您需要选择Scenes\ResourceViewer场景，然后输入"ExtractAllCpkArchives"指令来解压所有原始游戏中的.cpk压缩包（包含movie.cpk，所有的Bink动画文件均在这个压缩包中）
  ```
  ffmpeg -i input.mp4 -c:v libvpx -b:v 3M -c:a libvorbis output.webm  // vp8 + vorbis
  ```

## 如何在手持设备上运行
- 在打包后的运行时，所有平台都默认使用Application.persistentDataPath目录读取仙剑三文件，具体这个目录在哪里，根据平台决定，请阅读Unity文档：[Application.persistentDataPath](https://docs.unity3d.com/2022.2/Documentation/ScriptReference/Application-persistentDataPath.html)
- 第一次安装游戏到手持设备后，请先运行一次，然后将手持设备连接电脑，拷贝整个仙剑奇侠三（或者外传）原游戏目录到游戏App目录下即可，如果是仙剑三，文件夹的名必须为PAL3，如果是仙剑三外传，文件夹名必须为PAL3A。
- iOS设备请使用iTunes将文件夹共享给游戏应用或者自己打包游戏原始数据到 [StreamingAssets](https://docs.unity3d.com/2022.2/Documentation/Manual/StreamingAssets.html) 文件夹下。

## 如何切换仙剑奇侠传三/仙剑奇侠传三外传
两部游戏版本在项目中的切换是靠 [Custom scripting symbols](https://docs.unity3d.com/2022.2/Documentation/Manual/CustomScriptingSymbols.html) 实现的，PAL3对应仙剑奇侠传三，PAL3A对应仙剑奇侠传三外传。我在Unity编辑器菜单栏里面加了一个快捷按钮可以一键切换（菜单栏->Pal3->Switch Variant->PAL3A），切换之后对应的游戏文件夹也自动的从PAL3转换成PAL3A。切换的过程中，包括Symbol，图标和应用程序名在内的所有名称和标示都会由PAL3变为PAL3A。

## 卡通渲染与光影下的截图
![PAL3 卡通渲染+光影](Screenshots/PAL3_ToonShading_Lighting.png?raw=true)
![PAL3A 卡通渲染+光影](Screenshots/PAL3A_ToonShading_Lighting.png?raw=true)
![PAL3 卡通渲染+光追](Screenshots/PAL3_ToonShading_RayTracing.png?raw=true)

## 关于特效和卡通渲染
注意：游戏当前实现的所有特效以及卡通渲染部分使用了Unity Asset Store的资源，所以这部分实现（特效Prefab和Toon shader）没有办法开源。项目启动后默认会使用开源实现的Shader进行渲染（与原始游戏渲染风格一致），特效的话则会不显示，注意：这并不会影响游戏的编译和运行。

## 按键以及操作
- 鼠标键盘：鼠标左键点击操作人物行走方向（键盘方向键也可以），AD控制镜头旋转，空格与周边附近的物品或者NPC交互（也是跳跃键），M键打开大地图，U键或ESC键打开剧情选择菜单，Tab键切换角色（迷宫中）。
- 手柄：左摇杆控制人物行走，右摇杆控制镜头旋转，A键交互，菜单和选择按钮对应大地图和剧情选择菜单，LB/RB切换上一个/下一个角色（迷宫中）。
- 触屏：仅在有触摸屏的手持设备上才会启用，虚拟摇杆控制行走，交互键互动。

## 项目进度以及路线图
仙剑奇侠传三以及仙剑奇侠传三外传的剧情部分已经全部完成，两部游戏都可以完整的从头玩到尾体验一遍剧情，也可以使用游戏内提供的剧情选择菜单跳转至预设好的剧情时间点。两部游戏中的迷宫机关也基本全部实现，剧情中出现的大部分特效也已经重新实现。但是战斗，小游戏以及围绕支持战斗系统的各类系统和功能均尚未完成。另外主菜单和大部分界面也需要重新设计和实现（毕竟现在还要做手机端的适配）。

## 如何贡献？
因为项目还处于早期实现过程中，很多系统还没有实现，暂时不接受比较大的Pull request，特别是feature类型，如果您有好的想法，意见或者发现了Bug请欢迎提交issue或者加入交流群与我讨论。
另外您还可以参考这个项目解析视频：https://www.bilibili.com/video/BV1Pr4y167sF

## 为什么要做这个？[TL;DR]
学了几周Unity之后，一直想找项目练手，一开始自己尝试做了一下新仙剑的第一关，大概了解了RPG游戏的制作流程，后因机缘巧合发现了zby大佬的[PAL3patch](https://github.com/zhangboyang/PAL3patch)项目以及dontpanic92大佬的[OpenPAL3](https://github.com/dontpanic92/OpenPAL3)项目，所以给技术上实现仙三复刻带来了可能。至于为什么选择C#/Unity？选择Unity的原因有两点，第一是Unity对全平台全端的支持和打包做的很好很方便，第二个是Unity提供Mac上arm64原生IDE支持（我大概有一半时间是在Mac上写代码的）。当然其实本项目的实现几乎没有用到Unity引擎的大部分功能，特别是编辑器功能，因为几乎是100%纯代码实现的，所以其实理论上不需要花太大的代价就可以把本项目移植到其他支持C#脚本的引擎中。

## 社区
* 仙剑三（及外传）复刻版讨论群：252315306
* 仙剑三（及外传）复刻版开发群：330680869
