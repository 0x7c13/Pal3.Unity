// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using Battle;
    using UnityEditor;
    using UnityEngine;

    public sealed partial class BattleEditorWindow
    {
        private void DrawAiControls(BattleSession combat, BattleActorSnapshot actor)
        {
            if (combat.Definitions == null)
                return;
            GUILayout.Label("原版 AI 配置", EditorStyles.boldLabel);
            int flags = combat.GetAiProfile(actor.Id);
            string[] labels = { "普攻", "仙术", "特技", "物品", "万全", "自动" };
            int changed = flags;
            using (new EditorGUI.DisabledScope(combat.Status != BattleSessionStatus.Running))
            using (new EditorGUILayout.HorizontalScope())
                for (int bit = 0; bit < labels.Length; bit++)
                {
                    int mask = 1 << bit;
                    bool enabled = GUILayout.Toggle((flags & mask) != 0, labels[bit]);
                    changed = enabled ? changed | mask : changed & ~mask;
                }
            if (changed != flags)
                Try(() => combat.SetAiProfile(actor.Id, changed));
            if (actor.Side != BattleSide.Ally)
                return;
            using (new EditorGUI.DisabledScope(combat.Status != BattleSessionStatus.Running || combat.WaitingActor != actor.Id))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (DrawerButton("F · 自动普攻"))
                    Try(() =>
                    {
                        combat.RequestAiNormal(actor.Id);
                        _session.SyncBattle();
                    });
                if (DrawerButton("R · 重复上次指令"))
                    Try(() =>
                    {
                        combat.RequestAiRepeat(actor.Id);
                        _session.SyncBattle();
                    });
            }
        }
    }
}
