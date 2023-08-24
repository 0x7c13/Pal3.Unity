// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Actor
{
    using Core.Contracts;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Core.Navigation;
    using Data;
    using MetaData;

    public sealed class Actor : ActorBase
    {
        public ScnNpcInfo Info { get; }

        private uint? _overwrittenScriptId;
        private uint? _overwrittenMoveSpeed;

        public Actor(GameResourceProvider resourceProvider, ScnNpcInfo npcInfo) :
            base(resourceProvider, npcInfo.Id, npcInfo.Name)
        {
            Info = npcInfo;
        }

        public uint GetScriptId()
        {
            return _overwrittenScriptId ?? Info.ScriptId;
        }

        public void ChangeScriptId(uint scriptId)
        {
            _overwrittenScriptId = scriptId;
        }

        public bool IsScriptIdChanged()
        {
            return _overwrittenScriptId != null;
        }

        public float GetMoveSpeed(MovementMode movementMode)
        {
            if (_overwrittenMoveSpeed != null)
            {
                return _overwrittenMoveSpeed.Value / GameBoxInterpreter.GameBoxUnitToUnityUnit;
            }

            if (Info.GameBoxMoveSpeed > 0)
            {
                return Info.GameBoxMoveSpeed / GameBoxInterpreter.GameBoxUnitToUnityUnit;
            }

            if (Info.Type == ActorType.MainActor)
            {
                return movementMode == MovementMode.Run
                    ? ActorConstants.PlayerActorRunSpeed
                    : ActorConstants.PlayerActorWalkSpeed;
            }
            else
            {
                return movementMode == MovementMode.Run
                    ? ActorConstants.NpcActorRunSpeed
                    : ActorConstants.NpcActorWalkSpeed;
            }
        }

        public void ChangeMoveSpeed(uint moveSpeed)
        {
            _overwrittenMoveSpeed = moveSpeed;
        }

        public void ResetMoveSpeed()
        {
            _overwrittenMoveSpeed = null;
        }

        public string GetInitAction()
        {
            return !string.IsNullOrEmpty(Info.InitAction) && HasAction(Info.InitAction) ?
                Info.InitAction :
                GetIdleAction();
        }

        public float GetInteractionMaxDistance()
        {
            return Info.Type switch
            {
                ActorType.Soldier => 4f,
                ActorType.MainActor => 4f,
                ActorType.StoryNpc => 4f,
                ActorType.HotelManager => 6f,
                ActorType.Dealer => 6f,
                _ => 4f
            };
        }
    }
}