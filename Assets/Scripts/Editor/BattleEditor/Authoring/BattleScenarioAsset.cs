// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using Battle;
    using UnityEngine;

    [CreateAssetMenu(menuName = BattleData.Variant + "/Battle scenario", fileName = "BattleScenario")]
    public sealed class BattleScenarioAsset : ScriptableObject, ISerializationCallbackReceiver
    {
        [NonSerialized] public BattleScenario Scenario = new() { Variant = BattleData.Variant };
        [SerializeField, HideInInspector] private string _scenarioJson = "";

        public void OnBeforeSerialize() => _scenarioJson = Scenario == null ? "" : BattleScenarioJson.Serialize(Scenario);

        public void OnAfterDeserialize() => Scenario = string.IsNullOrEmpty(_scenarioJson) ? new BattleScenario { Variant = BattleData.Variant } : BattleScenarioJson.Deserialize(_scenarioJson);
    }
}
