// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.GamePlay
{
    using System;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataReader.Scn;
    using MetaData;
    using UnityEngine.InputSystem;

    public partial class PlayerGamePlayManager :
        ICommandExecutor<SwitchPlayerActorRequest>
    {
        public void Execute(SwitchPlayerActorRequest command)
        {
            if (_sceneManager.GetCurrentScene().GetSceneInfo().SceneType == ScnSceneType.Maze)
            {
                SwitchPlayerActorInCurrentTeam(true);
            }
        }

        private void SwitchToNextPlayerActorPerformed(InputAction.CallbackContext _)
        {
            SwitchPlayerActorInCurrentTeam(true);
        }

        private void SwitchToPreviousPlayerActorPerformed(InputAction.CallbackContext _)
        {
            SwitchPlayerActorInCurrentTeam(false);
        }

        private void SwitchPlayerActorInCurrentTeam(bool next)
        {
            if (!_playerActorManager.IsPlayerActorControlEnabled() ||
                !_playerActorManager.IsPlayerInputEnabled() ||
                _sceneManager.GetCurrentScene().GetSceneInfo().SceneType != ScnSceneType.Maze)
            {
                return;
            }

            var actorsInTeam = _teamManager.GetActorsInTeam();
            if (actorsInTeam.Count <= 1) return; // Makes no sense to change player actor if there is only one actor in the team

            var playerActorIdLength = Enum.GetNames(typeof(PlayerActorId)).Length;
            int targetPlayerActorId = _playerActor.Info.Id;

            do
            {
                targetPlayerActorId = (targetPlayerActorId + playerActorIdLength + (next ? +1 : -1)) % playerActorIdLength;
            } while (!actorsInTeam.Contains((PlayerActorId) targetPlayerActorId));

            CommandDispatcher<ICommand>.Instance.Dispatch(new ActorEnablePlayerControlCommand(targetPlayerActorId));
        }
    }
}