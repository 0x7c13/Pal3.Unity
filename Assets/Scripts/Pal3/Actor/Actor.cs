// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Actor
{
    using Core.DataReader.Scn;
    using Data;
    using MetaData;
    using UnityEngine;

    public class Actor : ActorBase
    {
        public ScnNpcInfo Info { get; }

        public bool IsActive { get; set; }

        public Actor(GameResourceProvider resourceProvider, ScnNpcInfo npcInfo) : base(resourceProvider, npcInfo.Name)
        {
            Info = npcInfo;
        }

        public new void ChangeName(string name)
        {
            Info.Name = name;
            base.ChangeName(name);
        }

        public bool IsMainActor()
        {
            return Info.Name.StartsWith("1"); // 101-110 are all main actor models
        }

        public bool IsMonsterActor()
        {
            return !Info.Name.StartsWith("1") && !Info.Name.StartsWith("2");
        }

        public string GetInitAction()
        {
            return !string.IsNullOrEmpty(Info.InitAction) && HasAction(Info.InitAction) ?
                Info.InitAction :
                GetIdleAction();
        }

        public string GetIdleAction()
        {
            if (IsMainActor())
            {
                return ActorConstants.ActionToNameMap[ActorActionType.Stand];
            }

            if (IsMonsterActor() && HasAction(ActorConstants.MonsterIdleAction))
            {
                return ActorConstants.MonsterIdleAction;
            }

            if (HasAction(ActorConstants.ActionToNameMap[ActorActionType.NpcStand1]))
            {
                return ActorConstants.ActionToNameMap[ActorActionType.NpcStand1];
            }

            if (HasAction(ActorConstants.ActionToNameMap[ActorActionType.NpcStand2]))
            {
                return ActorConstants.ActionToNameMap[ActorActionType.NpcStand2];
            }

            Debug.LogError($"No default idle animation found for {Info.Id}_{Info.Name}");
            return ActorConstants.ActionToNameMap[ActorActionType.NpcStand1];
        }

        public string GetMovementAction(MovementMode mode)
        {
            if (IsMainActor())
            {
                return mode switch
                {
                    MovementMode.Walk => ActorConstants.ActionToNameMap[ActorActionType.Walk],
                    MovementMode.Run => HasAction(ActorConstants.ActionToNameMap[ActorActionType.Run])
                        ? ActorConstants.ActionToNameMap[ActorActionType.Run]
                        : ActorConstants.ActionToNameMap[ActorActionType.Walk],
                    MovementMode.StepBack => ActorConstants.ActionToNameMap[ActorActionType.StepBack],
                    _ => ActorConstants.ActionToNameMap[ActorActionType.Walk]
                };
            }

            if (IsMonsterActor() && HasAction(ActorConstants.MonsterWalkAction))
            {
                return ActorConstants.MonsterWalkAction;
            }

            return mode switch
            {
                MovementMode.Walk => ActorConstants.ActionToNameMap[ActorActionType.NpcWalk],
                MovementMode.Run => ActorConstants.ActionToNameMap[ActorActionType.NpcRun],
                _ => ActorConstants.ActionToNameMap[ActorActionType.NpcWalk]
            };
        }

        // TODO: Get weapon based on inventory context
        public string GetWeaponName()
        {
            return ActorConstants.MainActorWeaponMap.TryGetValue(Info.Name, out var value) ?
                value : null;
        }

        public float GetInteractionMaxDistance()
        {
            return Info.Kind switch
            {
                ScnActorKind.Soldier => 4f,
                ScnActorKind.MainActor => 4f,
                ScnActorKind.StoryNpc => 4f,
                ScnActorKind.HotelManager => 6f,
                ScnActorKind.Dealer => 6f,
                _ => 4f
            };
        }
    }
}