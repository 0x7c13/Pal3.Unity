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

## 战斗编辑器

菜单 `PAL3 / PAL3A → Battle Editor` 打开独立窗口，无需进入 Play Mode 或载入剧情。在画布中央选择数据目录，然后点击“创建随机战斗 → 开始测试”。开始测试后即播放，顶部主操作随状态变为暂停、继续或重新测试。切换作品使用现有 `Switch Variant`。

随机示例随机选择战场、最多 4 位不同队友及 1–6 位同等级敌人。队友等级比敌方高 3 级（最高 100 级），按各自等级计算成长和装备属性并补满精气神。随机示例和新增队友默认开放各自的全部特技、仙术，熟练度为 4，方便测试；精气神消耗、装备、变身等使用条件继续生效，载入已有预设时保留其技能配置。战斗音乐留空时按原版剧情场景映射自动选择；共享战场缺少唯一剧情来源时使用有诊断说明的编辑器默认曲目，可填写原始曲号覆盖，或填 `NONE` 静音。音乐在 1×播放时输出，暂停、步进及倍速保持静音。

可配置场景、阵位、模型、等级、装备、技能、状态及局内物品／金钱／蛊。属性可选显式有效值、基础值加装备，或原版等级成长；当前精气神按最终派生上限校验。预设保存为 `BattleScenarioAsset`，支持 Undo/Redo；编辑草稿与运行快照隔离。右侧“编辑配置”按场景、队伍、物品分组，高级字段折叠收纳；修改、撤销或还原草稿会暂停当前测试，点击“应用并重新开始”建立新快照。文件菜单提供新建、打开、保存、另存与诊断导出；随机创建作为未命名新配置，不覆盖已打开的预设。

独立实战覆盖两槽时间条、普攻行列范围、五灵、仙术／特技／物品、装备被动、异常状态、阵法与援护、合击、魔剑技、尸块和专用角色／Boss 机制；外传包含摄灵五灵轮、两种变身及累计损失选项。局内结算包含经验、升级、法术解锁、熟练度、金钱及物品，并配有结果演出。双方可手动操作或使用按发布程序实现的 AI；日志保留请求值、实际变化及随机轨迹。支持暂停、倍速、逻辑单步、下一命中／行动结束、输入重放和 JSON 报告，结果不写回剧情进度或正式背包。

新配置默认使用“实时光照 + Toon”，也可在“编辑配置 → 场景 → 音乐、渲染与相机”切换为“原版光影”。原版保留烘焙光影、顶点色及原版角色光照，不创建 Unity 实时灯；Toon 使用实时方向光。普通预览按显示器实际像素密度渲染；启用原版镜头拖影时保留其原生 256×256 缓冲，可关闭拖影检查清晰画面。

“诊断工具”集中单步、重放、动作预览、原始资源表、日志与画布设置。受击数字默认保留已核实的原版蓝色 `#3232FF`，画布设置可改为黄色或白色；恢复、气、神及 MISS 颜色保持原版。

战斗视图使用原版界面资源，按横屏布局适配 21:9、4:3、2:1、1:1 和安全区，并提供可选的 4:3 预览；“触屏操作”沿用同一套原生界面，适度放大菜单、支持列表滚动／点选与触控命中。目标直接在场景中选择：鼠标悬停时身体闪动并显示原版头顶指针，单击生效；触屏轻触一次即可对有效目标施放，点到无效目标或空白处会回到上一层。AI 与援护也直接点选角色，触屏第一次轻触预览，再点同一角色确认。AI 页先编辑草稿，确认按原版计算时间条成本，取消保留当前设置；调试指令面板独立保留。

Battle Editor 的战斗界面支持手柄左摇杆／方向键八向命令盘、A 确认、B 返回、LB／RB 切换分类或额外控件／AI 角色、X 回到普攻焦点（援护页清除援护）、Y 重复、Start 开始／暂停／恢复；结果页也可确认继续。方向盘保留原版九宫位置，魔剑、五灵轮与变身等独立控件通过肩键访问。键盘使用方向键／WASD、Q／E／Z／C 斜向、Enter 确认、Esc 返回、Tab 切换、F／R 普攻／重复。鼠标、键盘、手柄及触屏共用语义输入；原生 Editor 输入流有虚拟手柄回归，实体硬件另行验收。

当前采用 20ms 参考步长，按本地两作发布程序核实数值、随机顺序与身体／特效／相机／音频行为；无效快照、缺失资源和不可直接施放的原始描述记录会显示诊断。纯 C# 战斗领域及界面 Presenter 分别独立于 Unity，窗口缩放和投影不参与战斗结算。验证分为无图形逻辑／数据、后台 GPU 离屏、真实桌面 UI 三层；默认后台检查不创建测试窗口，桌面焦点／IMGUI 像素检查需单独执行。最新验证范围与未执行项目见工作区 `docs/battle-system-validation.md`；实体设备与完整原版逐帧视听一致性仍需单独验收。

纯 C# 内核见 [Battle core](Assets/Scripts/Pal3.Battle/README.md)，Unity 编辑器适配与所有权边界见 [Battle Editor composition](Assets/Scripts/Editor/BattleEditor/README.md)。

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
两作首次运行时默认使用「卡通与实时光影」。可在「游戏设置 → 画风」切换为 URP 原版画风，重启后生效。两档都使用仓库内的 shader，运行时从玩家本地读取原版游戏资源。

「游戏设置 → 画面与特效设置」提供接触阴影、角色光照、增强夜景火光和火光投影四个独立开关，在卡通画风中即时生效。接触阴影和火光投影在桌面设备默认开启，手持设备可按需开启。夜景火光使用暖色灯光与冷色环境底色。火光投影也适用于暗色迷宫，桌面默认四盏，控制台可通过 `Settings.ToonFireShadowBudget` 设置 0–4 盏，阴影图集随预算扩容。泛光可以独立开关，默认仅桌面原版画风开启。

上方截图来自旧版卡通渲染。两作已支持的剧情与场景特效由原版数据驱动，运行时读取本地 CPK 的特效组、任务、粒子、贴图和模型，使用自有 URP shader 与 Unity 原生渲染组件。Unity 菜单 `PAL3 / PAL3A → Effect Editor` 可选择作品、播放、检查对象、修改组任务和旧粒子曲线；覆盖资产分别保存到 `Assets/Resources/Effects/PAL3Overrides.asset` 或 `PAL3AOverrides.asset` 后供游戏读取。资源浏览器的特效视图可直接打开 Effect Editor；组 ID `-1` 支持 PAL3／PAL3A 两作传送点。

PAL3 已覆盖当前清单中的 56 个剧情／场景根组，PAL3A 已覆盖 93 个，均由原版数据驱动。传送点按原版圆盘、双光锥及场景参数配色播放。SPar 关键帧、路径、六槽控制器和材质混合独立实现，原始 SPar 字段在编辑器中只读。战斗专用类型由独立战斗会话提供上下文；缺失资源或不支持的输入会给出诊断。

实现所需的代码、Shader 和构建资源均随本仓库提供。运行时读取玩家本地的原版游戏数据；原版数据不随项目分发。

## 按键以及操作
  - 鼠标键盘：鼠标左键点击操作人物行走方向（键盘方向键也可以），AD控制镜头旋转，空格与周边附近的物品或者NPC交互（也是跳跃键），M键打开大地图，U键或ESC键打开剧情选择菜单，Tab键切换角色（迷宫中）。
  - 手柄：左摇杆控制人物行走，右摇杆控制镜头旋转，A键交互，菜单和选择按钮对应大地图和剧情选择菜单，LB/RB切换上一个/下一个角色（迷宫中）。
  - 触屏：仅在有触摸屏的手持设备上才会启用，虚拟摇杆控制行走，交互键互动。

## 项目进度以及路线图
仙剑奇侠传三以及仙剑奇侠传三外传的剧情部分已经全部完成，两部游戏都可以完整的从头玩到尾体验一遍剧情，也可以使用游戏内提供的剧情选择菜单跳转至预设好的剧情时间点。两部游戏中的迷宫机关也基本全部实现，剧情中出现的大部分特效也已经重新实现。独立 Battle Editor 已提供两作战斗的配置、运行、原版规则与表现及回放测试入口；剧情中的正式接战、背包、完整装备管理和进度写回仍待接入。小游戏、主菜单及其他界面的后续实现与移动端适配另行推进。

## 如何贡献？
修改自有 C# 代码后，使用 `global.json` 指定的 .NET SDK，在工程根目录执行：

```sh
dotnet run --project tools/CodeStyle --
dotnet run --project tools/CodeStyle -- --check
```

格式工具以纯 .NET 实现，无需 Python、Unity 或游戏数据。它使用 Roslyn、`.editorconfig` 和固定版本的 StyleCop 布局规则，检查空格、缩进、函数／成员间空行及多条语句分行。默认只应用改动行及删除边界上的格式修正，保留其他行的既有格式；显式路径或 `--all` 可检查整个文件。工具分别检查 PAL3／PAL3A 条件分支；StyleCop 仅用于开发工具，不进入 Unity 工程。PR 的 `Code style` 工作流先运行 `dotnet test tools/CodeStyle.Tests`，再执行相同格式检查。更多命令见[工具说明](tools/CodeStyle/README.md)。格式检查通过后仍需运行相关 Unity 测试；ShaderLab／HLSL 由人工整理并验证。

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

