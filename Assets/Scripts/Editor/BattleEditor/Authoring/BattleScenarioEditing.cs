// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Linq;
    using Battle;

    // Invalid imports remain inspectable; restoring the required authoring graph
    // is explicit and undoable, rather than silently changing data while drawing.
    public static class BattleScenarioEditing
    {
        public static bool NeedsRepair(BattleScenario scenario) => scenario == null ||
            scenario.Participants == null || scenario.Items == null || scenario.TreasureUses == null ||
            scenario.Camera == null || scenario.Pal3A?.SpiritWheel?.Charges is not { Length: 5 } ||
            scenario.Participants.Any(actor => actor == null || actor.Skills == null || actor.Statuses == null ||
                actor.Attributes is not { Length: 12 } || actor.Vitals is not { Length: 3 } || actor.Equipment is not { Length: 6 } ||
                actor.Skills.Any(skill => skill == null) || actor.Statuses.Any(state => state == null)) ||
            scenario.Items.Any(item => item == null) || scenario.TreasureUses.Any(item => item == null);

        public static BattleScenario Repair(BattleScenario scenario)
        {
            scenario ??= new BattleScenario();
            scenario.Camera ??= new BattleCamera();
            scenario.Pal3A ??= new BattlePal3AState();
            scenario.Pal3A.SpiritWheel ??= new BattleSpiritWheel();
            scenario.Pal3A.SpiritWheel.Charges = Resize(scenario.Pal3A.SpiritWheel.Charges, 5);
            scenario.Participants ??= new();
            scenario.Items ??= new();
            scenario.TreasureUses ??= new();
            scenario.Participants.RemoveAll(actor => actor == null);
            scenario.Items.RemoveAll(item => item == null);
            scenario.TreasureUses.RemoveAll(item => item == null);
            foreach (BattleParticipant actor in scenario.Participants)
            {
                actor.Skills ??= new();
                actor.Statuses ??= new();
                actor.Skills.RemoveAll(skill => skill == null);
                actor.Statuses.RemoveAll(state => state == null);
                actor.Attributes = Resize(actor.Attributes, 12);
                actor.Vitals = Resize(actor.Vitals, 3);
                actor.Equipment = Resize(actor.Equipment, 6);
            }
            return scenario;
        }

        private static int[] Resize(int[] values, int length)
        {
            if (values?.Length == length)
                return values;
            var repaired = new int[length];
            if (values != null)
                Array.Copy(values, repaired, Math.Min(values.Length, length));
            return repaired;
        }
    }
}
