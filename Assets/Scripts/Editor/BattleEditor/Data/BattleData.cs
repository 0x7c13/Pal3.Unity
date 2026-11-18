// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Security.Cryptography;
    using System.Text;
    using Battle;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader;
    using Core.DataReader.Gdb;
    using Core.DataReader.Ini;
    using Core.DataReader.Txt;
    using Core.Primitives;
    using Engine.Extensions;
    using ResourceBrowser;
    using UnityEngine;

    // Read-only original-data adapter. No runtime service registrations or original-data writes.
    public sealed partial class BattleData : IDisposable
    {
#if PAL3A
        public const string Variant = "PAL3A";
#else
        public const string Variant = "PAL3";
#endif

        public ResourceCatalog Resources
        {
            get;
        }

        public GdbFile Database
        {
            get;
        }

        public CombatConfigFile Layout
        {
            get;
        }

        public CombatCameraConfigFile Cameras
        {
            get;
        }

        public CombatScnFile SceneMap
        {
            get;
        }

        public string[] Scenes
        {
            get;
        }

        public string Fingerprint
        {
            get;
        }

        private readonly Dictionary<string, ActorAction[]> _actions = new(StringComparer.OrdinalIgnoreCase);

        public BattleData(string root, int codepage, CancellationToken cancellation = default)
        {
            Resources = ResourceCatalog.OpenBaseData(root, codepage, cancellation);
            try
            {
                string database = $"basedata.cpk\\cbdata\\{Variant}_Softstar.gdb";
                string detected = Resources.Find("basedata.cpk\\cbdata\\PAL3A_Softstar.gdb") != null ? "PAL3A" : "PAL3";
                if (detected != Variant || Resources.Find(database) == null)
                    throw new InvalidDataException($"This is not {Variant} data. Use Switch Variant before loading the other game.");
                cancellation.ThrowIfCancellationRequested();
                Database = Read(database, new GdbFileReader());
                cancellation.ThrowIfCancellationRequested();
                Layout = Read("basedata.cpk\\datascript\\combat.ini", new CombatConfigFileReader());
                Cameras = Read("basedata.cpk\\datascript\\cbCam.ini", new CombatCameraConfigFileReader());
                SceneMap = Read("basedata.cpk\\datascript\\combatScn.txt", new CombatScnFileReader());
                Scenes = Resources.Entries.Where(entry => entry.Extension == ".pol" &&
                        entry.Path.StartsWith("basedata.cpk\\cbscene\\", StringComparison.OrdinalIgnoreCase) &&
                        Path.GetFileNameWithoutExtension(entry.Name).Equals(entry.Path.Split('\\')[^2], StringComparison.OrdinalIgnoreCase))
                    .Select(entry => entry.Path.Split('\\')[^2]).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(name => name, StringComparer.Ordinal).ToArray();
                using var hash = SHA256.Create();
                using var content = new MemoryStream();
                foreach (string path in new[] { database, "basedata.cpk\\datascript\\combat.ini", "basedata.cpk\\datascript\\cbCam.ini", "basedata.cpk\\datascript\\combatScn.txt" })
                {
                    cancellation.ThrowIfCancellationRequested();
                    byte[] bytes = Bytes(path);
                    content.Write(bytes, 0, bytes.Length);
                }
                Fingerprint = BitConverter.ToString(hash.ComputeHash(content.ToArray())).Replace("-", "").ToLowerInvariant();
            }
            catch
            {
                Resources.Dispose();
                throw;
            }
        }

        public byte[] Bytes(string path)
        {
            ResourceEntry entry = Resources.Find(path) ?? throw new FileNotFoundException("Missing battle dependency: " + path);
            return Resources.Read(entry);
        }

        public T Read<T>(string path, IFileReader<T> reader) => reader.Read(Bytes(path), Resources.Codepage);

        public string Model(BattleParticipant actor)
        {
            if (!string.IsNullOrWhiteSpace(actor.Model))
                return actor.Model;
            CombatActorInfo info = Database.CombatActorInfos[actor.ActorId];
            return info.Type == CombatActorType.MainActor || string.IsNullOrEmpty(info.ModelId) || info.ModelId.Equals("Null", StringComparison.OrdinalIgnoreCase)
                ? actor.ActorId.ToString() : info.ModelId;
        }

        public static int PlayerIdentity(int actorId) => BattleLoadoutRules.PlayerIdentity(Variant, actorId);

        public ActorAction[] Actions(BattleParticipant actor)
        {
            string model = Model(actor);
            if (_actions.TryGetValue(model, out ActorAction[] cached))
                return cached;
            string path = FileConstants.GetActorFolderVirtualPath(model) + model + ".ini";
            byte[] bytes = Bytes(path);
            if (bytes.Length < 6 || Encoding.ASCII.GetString(bytes, 0, 6) != ";#MV3#")
                throw new NotSupportedException("Battle preview requires an MV3 action configuration: " + path);
            ActorAction[] actions = new Mv3ConfigFileReader().Read(bytes, Resources.Codepage).ActorActions;
            _actions[model] = actions;
            return actions;
        }

        public bool TryActionPath(BattleParticipant actor, string action, out string path)
        {
            ActorAction[] actions = Actions(actor);
            int index = Array.FindIndex(actions, item => item.ActionName.Equals(action, StringComparison.OrdinalIgnoreCase));
            path = index < 0 ? null : FileConstants.GetActorFolderVirtualPath(Model(actor)) + actions[index].ActionFileName;
            return path != null && Resources.Find(path) != null;
        }

        public string ActionPath(BattleParticipant actor, string action)
        {
            ActorAction[] actions = Actions(actor);
            int index = Array.FindIndex(actions, item => item.ActionName.Equals(action, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
                throw new InvalidOperationException($"Actor {actor.ActorId} has no action {action}.");
            return FileConstants.GetActorFolderVirtualPath(Model(actor)) + actions[index].ActionFileName;
        }

        public string IdleAction(BattleParticipant actor)
        {
            string[] preferred = Database.CombatActorInfos[actor.ActorId].Type == CombatActorType.MainActor
                ? new[] { ActorConstants.ActionToNameMap[ActorActionType.PreAttack], "z1" } : new[] { "z1", "z2" };
            return preferred.FirstOrDefault(name => Actions(actor).Any(action => action.ActionName.Equals(name, StringComparison.OrdinalIgnoreCase)))
                ?? Actions(actor).First().ActionName;
        }

        public string AttackAction(BattleParticipant actor)
        {
            if (Database.CombatActorInfos[actor.ActorId].Type != CombatActorType.MainActor)
                return ActorConstants.ActionToNameMap[ActorActionType.NpcAttack];
            int identity = PlayerIdentity(actor.ActorId);
            int weapon = actor.Equipment is { Length: > 0 } && actor.Equipment[0] != 0 ? (int)Database.GameItemInfos[actor.Equipment[0]].WeaponType : 0;
            bool second = Variant == "PAL3" ? identity == 2 && weapon == 7 || identity == 3 && weapon == 4 || identity == 4 && weapon == 2
                : identity == 1 && weapon == 4 || identity == 2 && weapon == 6 || identity == 4 && weapon == 9;
            return ActorConstants.ActionToNameMap[second ? ActorActionType.Attack2 : ActorActionType.Attack1];
        }

        public Vector3 Position(BattleParticipant actor, int sceneElement) => actor.OverridePosition
            ? new Vector3(actor.Position.X, actor.Position.Y, actor.Position.Z)
            : FormationPosition(actor.Side, actor.Slot, sceneElement);

        internal Vector3 FormationPosition(BattleSide side, BattleSlot slot, int sceneElement)
        {
            if (side == BattleSide.Enemy)
                return Layout.ActorGameBoxPositions[5 + (int)slot].ToUnityPosition();
            if (slot == BattleSlot.Center)
            {
                var center = Layout.AllyFormationConfig.CenterGameBoxPosition;
                return new GameBoxVector3(center.X, 0, center.Z).ToUnityPosition();
            }
            // combat.ini places Thunder before Wind in the neutral allied lineup.
            BattleSlot initial = BattleTargetRules.InitialAllySlot(slot, sceneElement);
            int role = initial switch
            {
                BattleSlot.Thunder => 2,
                BattleSlot.Wind => 3,
                _ => (int)initial,
            };
            return Layout.ActorGameBoxPositions[role].ToUnityPosition();
        }

        public BattleParticipant CreateParticipant(int actorId, BattleSide side, BattleSlot slot)
        {
            CombatActorInfo info = Database.CombatActorInfos[actorId];
            var actor = new BattleParticipant
            {
                ActorId = actorId,
                Side = side,
                Slot = slot,
                Level = Math.Clamp(info.Level, 1, 100),
                Experience = Math.Max(0, info.Experience),
                Attributes = Enumerable.Range(0, 12).Select(index => Math.Max(0, info.AttributeValues[(ActorAttributeType)index])).ToArray(),
                Skills = info.SkillIds.Select((id, index) => (id, index)).Where(entry => entry.id != 0).GroupBy(entry => entry.id)
                    .Select(group => new BattleSkill
                    {
                        Id = (int)group.Key,
                        Level = info.Type != CombatActorType.MainActor && group.First().index == 3 ? 4 :
                            group.First().index < info.SkillLevels.Length ? Math.Max(1, (int)info.SkillLevels[group.First().index]) : 1
                    }).ToList(),
            };
            actor.Vitals = actor.Attributes.Take(3).ToArray();
            string model = Model(actor);
            if (ActorConstants.MainActorWeaponMap.TryGetValue(model, out string weapon))
                actor.Equipment[0] = Database.GameItemInfos.Where(pair => pair.Value.Type == ItemType.Weapon && pair.Value.ModelName == weapon &&
                    pair.Value.ApplicableActors.Contains((PlayerActorId)PlayerIdentity(actorId)))
                    .Select(pair => pair.Key).OrderBy(id => id).FirstOrDefault();
            if (actor.Equipment[0] == 0 && info.Type == CombatActorType.MainActor)
                actor.Equipment[0] = Database.GameItemInfos.Where(pair => pair.Value.Type == ItemType.Weapon &&
                    pair.Value.ApplicableActors.Contains((PlayerActorId)PlayerIdentity(actorId)) &&
                    (int)pair.Value.WeaponType > 0 && pair.Key != BattleMagicSwordRules.WeaponId)
                    .Select(pair => pair.Key).OrderBy(id => id).FirstOrDefault();
            if (actor.Equipment[0] == BattleMagicSwordRules.WeaponId)
            {
                // A newly added participant has no magic-sword cultivation snapshot yet.
                GameItemInfo original = Database.GameItemInfos[actor.Equipment[0]];
                actor.Equipment[0] = Database.GameItemInfos.Where(pair => pair.Value.Type == ItemType.Weapon &&
                    pair.Value.WeaponType == original.WeaponType && (int)pair.Value.ItemSpecialType == 0 &&
                    pair.Value.ApplicableActors.Intersect(original.ApplicableActors).Any())
                    .Select(pair => pair.Key).OrderBy(id => id).FirstOrDefault();
            }
            return actor;
        }

        public BattleParticipant CreateTestParticipant(int actorId, BattleSide side, BattleSlot slot) =>
            BattleTestLoadoutBuilder.Create(this, actorId, side, slot);

        public string CombatRosterDenial(int actorId, BattleSide side)
        {
            if (!Database.CombatActorInfos.TryGetValue(actorId, out CombatActorInfo info))
                return $"角色 {actorId}：原版数据中不存在。";
            bool main = info.Type == CombatActorType.MainActor;
            if (main != (side == BattleSide.Ally))
                return $"角色 {actorId}（{info.Name}）：战斗仅支持主角加入我方、怪物加入敌方；其他组合可用于动作预览。";
            if (main && PlayerIdentity(actorId) < 0)
                return $"角色 {actorId}（{info.Name}）：此主角形态尚不支持独立开战，请选择标准主角；仍可用于动作预览。";
            return null;
        }

        public List<string> Validate(BattleScenario scenario, bool forBattle = false)
        {
            List<string> errors = scenario.Validate();
            if (scenario.Variant != Variant)
                errors.Add($"Scenario is {scenario.Variant}; current Unity Variant is {Variant}.");
            if (scenario.DataFingerprint != Fingerprint)
                errors.Add("Scenario data fingerprint differs. Explicitly rebind the scenario to this data before previewing.");
            if (!Scenes.Contains(scenario.Scene, StringComparer.OrdinalIgnoreCase))
                errors.Add("Combat scene is missing: " + scenario.Scene);
            if (scenario.Participants == null)
                return errors;
            var allyIdentities = new HashSet<int>();
            foreach (BattleParticipant actor in scenario.Participants.Where(actor => actor != null))
            {
                if (!Database.CombatActorInfos.ContainsKey(actor.ActorId))
                {
                    errors.Add("Unknown actor: " + actor.ActorId);
                    continue;
                }
                if (forBattle)
                {
                    if (CombatRosterDenial(actor.ActorId, actor.Side) is string reason)
                        errors.Add(reason);
                    else if (actor.Side == BattleSide.Ally && !allyIdentities.Add(PlayerIdentity(actor.ActorId)))
                        errors.Add($"角色 {actor.ActorId}：同一主角只能参加一次战斗。");
                }
                try
                {
                    string idle = IdleAction(actor);
                    if (!TryActionPath(actor, idle, out string idlePath))
                        errors.Add($"Actor {actor.ActorId}: missing initial action {idle} ({idlePath}).");
                    // Original INIs include optional, absent actions. GotoAction
                    // can fail while the current animation continues; validate
                    // actual requested dependencies when compiling their task.
                }
                catch (Exception ex)
                {
                    errors.Add(ex.Message);
                }
                if (actor.Skills != null)
                    foreach (BattleSkill skill in actor.Skills.Where(skill => skill != null))
                        if (!Database.SkillInfos.ContainsKey(skill.Id))
                            errors.Add("Unknown skill: " + skill.Id);
                if (actor.Equipment != null)
                    foreach (int item in actor.Equipment.Where(id => id != 0))
                        if (!Database.GameItemInfos.ContainsKey(item))
                            errors.Add("Unknown equipment: " + item);
            }
            if (scenario.Items != null)
                foreach (BattleItem item in scenario.Items.Where(item => item != null))
                    if (!Database.GameItemInfos.ContainsKey(item.Id))
                        errors.Add("Unknown item: " + item.Id);
            return errors;
        }

        public BattleSetup Capture(BattleScenario scenario, bool forBattle = false)
        {
            if (scenario == null)
                throw new ArgumentNullException(nameof(scenario));
            List<string> errors = Validate(scenario, forBattle);
            if (errors.Count > 0)
                throw new ArgumentException(string.Join("\n", errors), nameof(scenario));
            BattleScenario prepared = scenario.Copy();
            prepared.Participants = scenario.Participants.Select(actor => BattleLoadoutRules.Prepare(Variant, actor)).ToList();
            return new BattleSetup(prepared);
        }

        public void Dispose()
        {
            DisposeAnimationMetadata();
            Resources.Dispose();
        }
    }
}
