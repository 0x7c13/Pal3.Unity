// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.GameSystems.Team
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Gdb;
    using Data;

    // Mutable progression is separate from the shared, cached GDB records.
    public sealed class PlayerProgressManager : IDisposable,
        #if PAL3A
        ICommandExecutor<PlayerSetActorLevelCommand>,
        #endif
        ICommandExecutor<PlayerSetProgressCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private readonly IDictionary<int, CombatActorInfo> _actors;
        private readonly IDictionary<int, GameItemInfo> _items;
        private readonly TeamManager _team;
        private readonly Dictionary<int, (int level, int weapon)> _progress = new();

        public PlayerProgressManager(GameResourceProvider resources, TeamManager team)
            : this(resources.GetCombatActorInfos(), resources.GetGameItemInfos(), team)
        {
        }

        internal PlayerProgressManager(IDictionary<int, CombatActorInfo> actors,
            IDictionary<int, GameItemInfo> items, TeamManager team)
        {
            _actors = actors;
            _items = items;
            _team = team;
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose() => CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        private (int level, int weapon) Get(int actorId)
        {
            if (_progress.TryGetValue(actorId, out var progress))
                return progress;
            if (!ActorConstants.MainActorCombatActorIdMap.TryGetValue((PlayerActorId)actorId, out int combatId))
                throw new ArgumentOutOfRangeException(nameof(actorId));
            int weapon = 0;
            if (ActorConstants.MainActorWeaponMap.TryGetValue(ActorConstants.MainActorNameMap[(PlayerActorId)actorId], out string model))
                weapon = _items.Where(p => p.Value.Type == ItemType.Weapon &&
                    p.Value.ApplicableActors.Contains((PlayerActorId)actorId) &&
                    string.Equals(p.Value.ModelName, model, StringComparison.OrdinalIgnoreCase))
                    .Select(p => p.Key).OrderBy(id => id).FirstOrDefault();
            return (Math.Max(1, _actors[combatId].Level), weapon);
        }

        public int GetLevel(int actorId) => Get(actorId).level;
        public int GetWeaponType(int actorId) => _items.TryGetValue(Get(actorId).weapon, out var item) ? (int)item.WeaponType : 0;
        public IEnumerable<ICommand> GetSaveCommands() => _progress.OrderBy(p => p.Key)
            .Select(p => (ICommand)new PlayerSetProgressCommand(p.Key, p.Value.level, p.Value.weapon));
        public void Execute(PlayerSetProgressCommand command)
        {
            if (!ActorConstants.MainActorCombatActorIdMap.ContainsKey((PlayerActorId)command.ActorId))
                return;
            if (command.WeaponItemId != 0 && (!_items.TryGetValue(command.WeaponItemId, out var item) ||
                item.Type != ItemType.Weapon || !item.ApplicableActors.Contains((PlayerActorId)command.ActorId)))
                throw new ArgumentException("Weapon is not usable by this actor");
            _progress[command.ActorId] = (Math.Max(1, command.Level), command.WeaponItemId);
        }
        #if PAL3A
        public void Execute(PlayerSetActorLevelCommand command)
        {
            if (!ActorConstants.MainActorCombatActorIdMap.ContainsKey((PlayerActorId)command.ActorId))
                return;
            int[] otherLevels = _team.GetActorsInTeam().Where(id => (int)id != command.ActorId &&
                ActorConstants.MainActorCombatActorIdMap.ContainsKey(id)).Select(id => GetLevel((int)id)).ToArray();
            if (otherLevels.Length == 0)
                return;
            var progress = Get(command.ActorId);
            int target = (int)(otherLevels.Sum(x => (long)x) / otherLevels.Length);
            _progress[command.ActorId] = (Math.Max(progress.level, target), progress.weapon);
        }
        #endif

        public void Execute(ResetGameStateCommand command) => _progress.Clear();
    }
}
