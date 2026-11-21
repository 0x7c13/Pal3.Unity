// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using Battle;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    // Unity's inline class serialization turns absent optional snapshots into
    // new objects. Keep the engine-independent authoring contract as JSON so
    // null sword state survives assets, Undo, domain reload and reports.
    public static class BattleScenarioJson
    {
        private static readonly JsonSerializerSettings Settings = new()
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            TypeNameHandling = TypeNameHandling.None
        };

        private const string FormatProperty = "$battleScenarioFormat";

        public static string Serialize(BattleScenario scenario)
        {
            if (scenario == null)
                return "null";
            JObject document = JObject.FromObject(scenario, JsonSerializer.Create(Settings));
            document.AddFirst(new JProperty(FormatProperty, 1));
            return document.ToString(Formatting.None);
        }

        public static BattleScenario Deserialize(string json) => Decode(JObject.Parse(json));

        private static BattleScenario Decode(JObject document)
        {
            BattleScenario scenario = document.ToObject<BattleScenario>(JsonSerializer.Create(Settings));
            if (document.GetValue(nameof(BattleScenario.Participants), StringComparison.OrdinalIgnoreCase) is JArray participants)
                for (int index = 0; index < participants.Count; index++)
                    if (participants[index] is JObject actor &&
                        actor.GetValue(nameof(BattleParticipant.InstanceId), StringComparison.OrdinalIgnoreCase) == null)
                        scenario.Participants[index].InstanceId = null;
            return scenario;
        }

        // Only the old window codec needs repair. New authored invalid values
        // remain visible to validation instead of being silently normalized.
        public static BattleScenario RestoreDraft(string json)
        {
            JObject document = JObject.Parse(json);
            BattleScenario scenario = Decode(document);
            bool canonical = document[FormatProperty] != null ||
                document.SelectToken("Camera.Position.IsFinite") != null;
            if (!canonical && document["Participants"] is JArray participants)
                foreach (JToken participant in participants)
                    canonical |= participant.SelectToken("Position.IsFinite") != null;
            if (!canonical && scenario.Participants != null)
                foreach (BattleParticipant actor in scenario.Participants)
                    if (actor != null && (scenario.Variant != "PAL3" || actor.Equipment is not { Length: > 0 } ||
                        actor.Equipment[0] != BattleMagicSwordRules.WeaponId ||
                        actor.UsesNativeDefaultWeapon && BattleLoadoutRules.PlayerIdentity("PAL3", actor.ActorId) != 0))
                        actor.MagicSword = null;
            return scenario;
        }
    }
}
