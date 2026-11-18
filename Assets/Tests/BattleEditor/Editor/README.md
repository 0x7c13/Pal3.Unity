# Battle Editor checks

The test assemblies follow their dependencies:

- `Pal3.Core.Tests`: binary readers and original resource records.
- `Pal3.Battle.Tests` (`Tests/Battle/Editor`): engine-independent rules, simulation, scheduling and replay.
- `Pal3.Battle.Presentation.Tests` (`Tests/Battle/Presentation/Editor`): engine-independent feedback and projection.
- `Pal3.BattleEditor.Tests`: Unity resource adapters, authoring, HUD and integration.

Run the affected assemblies once per game using the existing Switch Variant
menu, and restore PAL3 after validation. The namespaces of existing battle tests
are retained so saved test filters remain valid. Integration fixtures reference
the domain test assembly for the shared synthetic scenario/catalogue.

Default automated validation uses isolated batch projects, a positive test filter
for windowless cases, and either `-nographics` or GPU RenderTexture rendering.
Do not run all editor-window tests unattended: native focus, file-picker and
window routing checks may show windows and need a separately requested desktop
session. Workbench IMGUI can also be checked with the workspace's windowless
Panel driver, without adding private Unity APIs to product code.

Portable cases cover initial-state isolation, malformed inputs, native texture
failure cleanup, preset persistence, schema guards, saving and Undo, and a real
assembly reload with an unsaved editor draft. The domain and semantic presentation assemblies have no Unity references and
can also be compiled independently; authoring and resource integration use Unity.

`LocalGameData` cases use `PAL3_TEST_DATA_ROOT` or `../Game Data/<variant>` relative
to the project. Without local data they skip. They check both wrong-game loading
directions, immutable database inputs, captured preview isolation, explicit
attack targets and seeking, failed second-mesh initialization with recovery, and
20 start/sample/stop cycles in both rendering modes. Failure recovery checks native
Mesh, Material and Texture counts as well as scene ownership.

Rendering samples are written to `../workspace/Implementation/BattleEditor/QA/`.
Original game files, captures and local presets stay outside committed fixtures.
The preceding checks cover stage 1 scene/animation authoring. Stage 2 combat
checks are described below.


第二阶段新增 `BattleSessionTests`：两作原函数独立数值预期、初始条与回合计数、命中标记、暴击／闪避／防御、受击重启与取消剩余标记、死亡／取消、故障与输入重放、30/60/144Hz一致性、同速移动双方自然结束。领域程序集不引用Unity。

`BattleIntegrationTests` 使用真实两作CPK/GDB/MV3；覆盖一对一／多对多、两画风自然结束与渲染、重放、反复重开释放、失败保留旧会话、原始work时间、PAL3A分段除数与笛／弩原地时间线、备用武器拒绝，以及存在其他待输入角色时的下一命中推进。通过 `PAL3_TEST_DATA_ROOT` 指定当前作品原数据目录，`PAL3_BATTLE_QA_ROOT` 可选保存截图。未配置环境变量时使用工作区默认 `Game Data/<Variant>`。

完整工作区验证脚本位于 `workspace/Implementation/BattleStage2/validate.py`（工作区根目录下）。结果与审阅记录保存在同目录 `QA/`；仓库不包含原始资料或测试截图。当前50Hz更新只是明确的兼容参考；完整机制与手动鼠标体验验收仍按工作区分阶段计划记录。


第三阶段新增 `BattleMechanicsTests`，可脱离Unity运行：原版成本前置与减耗顺序、双槽蓄力／中断、物品开局消耗、治疗／复活、状态互斥与潜伏、镜避界、行列、逃跑、熟练／技能解锁、经验／升级／奖励及重放；暴击和群体死亡目标的随机顺序使用发布EXE独立预期。`BattleAbilitiesIntegrationTests` 使用真实风咒301、止血草6201、还魂香6211及怪物第四技能，验证原始类型、阈值、生效时间、扣费时点和固定熟练度。资源报告写入的目录／文件／悬空符号链接另由Resource Browser测试拒绝。

第三阶段完整验证入口为工作区 `workspace/Implementation/BattleStage3/validate.py`；原指令向量、反汇编及独立审查位于 `workspace/Analysis/Battle-Stage3/`，验证汇总与审查修复记录在 `workspace/Implementation/BattleStage3/QA/`。纯数值／分支向量不能代替后续完整AI、导演与原版主循环的动态对照。

随机示例与音乐回归见 `BattleRandomExampleTests`：128 个 authoring seeds 覆盖两作随机场景／阵容、等级及原版实际属性、正确累计经验、可启动会话；固定种子可重现选材；真实 MP3 自动解码与播放、动画时钟、绑定战斗复用音源，以及缺少剧情来源和音频失败诊断保持可见。
