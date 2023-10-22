// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Script
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.DataReader.Scn;
    using Core.Utilities;
    using Engine.Logging;
    using Engine.Services;
    using GameSystems.Dialogue;
    using GameSystems.Favor;
    using GameSystems.MiniGames;
    using Scene;
    using State;

    public sealed class UserVariableManager : IDisposable,
        ICommandExecutor<ScriptVarSetValueCommand>,
        ICommandExecutor<ScriptVarSetRandomValueCommand>,
        ICommandExecutor<ScriptVarAddValueCommand>,
        ICommandExecutor<ScriptVarDistractAnotherVarCommand>,
        ICommandExecutor<ScriptVarSetDialogueSelectionResultCommand>,
        ICommandExecutor<ScriptVarSetLimitTimeDialogueSelectionResultCommand>,
        ICommandExecutor<ScriptVarSetMazeSwitchStatusCommand>,
        ICommandExecutor<ScriptVarSetMoneyCommand>,
        ICommandExecutor<ScriptVarSetActorFavorCommand>,
        ICommandExecutor<ScriptVarSetMostFavorableActorIdCommand>,
        ICommandExecutor<ScriptVarSetLeastFavorableActorIdCommand>,
        ICommandExecutor<ScriptVarSetCombatResultCommand>,
        #if PAL3
        ICommandExecutor<ScriptVarSetAppraisalsResultCommand>,
        #elif PAL3A
        ICommandExecutor<ScriptVarSetWheelOfTheFiveElementsUsageCountCommand>,
        ICommandExecutor<ScriptVarSetObjectUsageStatusCommand>,
        #endif
        ICommandExecutor<ResetGameStateCommand>
    {
        private readonly Dictionary<ushort, int> _variables = new ();

        public UserVariableManager()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void SetVariableValue(ushort variable, int value)
        {
            _variables[variable] = value;
        }

        public int GetVariableValue(ushort variable)
        {
            return _variables.TryGetValue(variable, out int value) ? value : 0;
        }

        public Dictionary<ushort, int> GetGlobalVariables()
        {
            return _variables.Where(_ => (_.Key >= ScriptConstants.MainStoryVariableId))
                .ToDictionary(_ => _.Key, _ => _.Value);
        }

        public void Execute(ScriptVarSetValueCommand command)
        {
            EngineLogger.LogWarning($"Set var {command.Variable} with value: {command.Value}");
            SetVariableValue(command.Variable, command.Value);
        }

        public void Execute(ScriptVarSetRandomValueCommand command)
        {
            SetVariableValue(command.Variable, RandomGenerator.Range(0, command.MaxExclusiveValue));
        }

        public void Execute(ScriptVarAddValueCommand command)
        {
            SetVariableValue(command.Variable, GetVariableValue(command.Variable) + command.Value);
        }

        public void Execute(ScriptVarDistractAnotherVarCommand command)
        {
            int result = GetVariableValue(command.VariableA) - GetVariableValue(command.VariableB);
            SetVariableValue(command.VariableA, result >= 0 ? result : -result);
        }

        #if PAL3
        // TODO: Impl
        public void Execute(ScriptVarSetAppraisalsResultCommand command)
        {
            bool result = ServiceLocator.Instance.Get<AppraisalsMiniGame>().GetResult();
            SetVariableValue(command.Variable, result ? 1: 0);
        }
        #endif

        public void Execute(ScriptVarSetDialogueSelectionResultCommand command)
        {
            int selection = ServiceLocator.Instance.Get<DialogueManager>().GetDialogueSelectionButtonIndex();
            SetVariableValue(command.Variable, selection);
        }

        public void Execute(ScriptVarSetLimitTimeDialogueSelectionResultCommand command)
        {
            DialogueManager dialogueManager = ServiceLocator.Instance.Get<DialogueManager>();
            bool playerReactedInTime = dialogueManager.PlayerReactedInTimeForLimitTimeDialogue();
            SetVariableValue(command.Variable, playerReactedInTime ? 1 : 0);
        }

        public void Execute(ScriptVarSetMazeSwitchStatusCommand command)
        {
            string currentCity = ServiceLocator.Instance.Get<SceneManager>().GetCurrentScene().GetSceneInfo().CityName;

            if (ServiceLocator.Instance.Get<SceneStateManager>()
                    .TryGetSceneObjectStateOverride(currentCity,
                        command.SceneName,
                        command.ObjectId,
                        out SceneObjectStateOverride state) && state.SwitchState.HasValue)
            {
                SetVariableValue(command.Variable, state.SwitchState.Value == 1 ? 1 : 0);
            }
            else SetVariableValue(command.Variable, 0); // Default to off
        }

        public void Execute(ScriptVarSetMoneyCommand command)
        {
            // TODO: Remove this and uncomment the following line
            int totalMoney = 777777;
            // var totalMoney = ServiceLocator.Instance.Get<InventoryManager>().GetTotalMoney();
            SetVariableValue(command.Variable, totalMoney);
        }

        public void Execute(ScriptVarSetActorFavorCommand command)
        {
            int favor = ServiceLocator.Instance.Get<FavorManager>().GetFavorByActor(command.ActorId);
            SetVariableValue(command.Variable, favor);
        }

        public void Execute(ScriptVarSetMostFavorableActorIdCommand command)
        {
            int mostFavorableActorId = ServiceLocator.Instance.Get<FavorManager>().GetMostFavorableActorId();
            SetVariableValue(command.Variable, mostFavorableActorId);
        }

        public void Execute(ScriptVarSetLeastFavorableActorIdCommand command)
        {
            int leastFavorableActorId = ServiceLocator.Instance.Get<FavorManager>().GetLeastFavorableActorId();
            SetVariableValue(command.Variable, leastFavorableActorId);
        }

        #if PAL3A
        // TODO: Impl
        public void Execute(ScriptVarSetWheelOfTheFiveElementsUsageCountCommand command)
        {
            float rand = RandomGenerator.Range(0f, 1f);
            int usageCount = rand > 0.35f ? 360 : 0;
            SetVariableValue(command.Variable, usageCount);
        }

        public void Execute(ScriptVarSetObjectUsageStatusCommand command)
        {
            Scene scene = ServiceLocator.Instance.Get<SceneManager>().GetCurrentScene();
            ScnSceneInfo sceneInfo = scene.GetSceneInfo();
            SceneStateManager sceneStateManager = ServiceLocator.Instance.Get<SceneStateManager>();

            if (sceneStateManager.TryGetSceneObjectStateOverride(sceneInfo.CityName,
                    command.SceneName,
                    command.ObjectId,
                    out SceneObjectStateOverride state) && state.TimesCount.HasValue)
            {
                // Set variable to 1 if it's fully used, otherwise 0
                SetVariableValue(command.Variable, state.TimesCount.Value == 0 ? 1 : 0);
            }
            else SetVariableValue(command.Variable, 0); // Default to not used
        }
        #endif

        // TODO: Impl
        public void Execute(ScriptVarSetCombatResultCommand command)
        {
            bool won = RandomGenerator.Range(0f, 1f) > 0.35f;
            #if PAL3
            Pal3.Instance.Execute(won
                ? new UIDisplayNoteCommand("你战胜了重楼")
                : new UIDisplayNoteCommand("你输给了重楼"));
            #elif PAL3A
            Pal3.Instance.Execute(won
                ? new UIDisplayNoteCommand("你战胜了景小楼")
                : new UIDisplayNoteCommand("你输给了景小楼"));
            #endif
            SetVariableValue(command.Variable, won ? 1 : 0);
        }

        public void Execute(ResetGameStateCommand command)
        {
            _variables.Clear();
        }
    }
}