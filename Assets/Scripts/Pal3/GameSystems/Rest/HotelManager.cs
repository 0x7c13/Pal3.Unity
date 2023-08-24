// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.GameSystems.Rest
{
    using System;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.Utils;
    using MetaData;
    using Scene;
    using Script;
    using State;

    public sealed class HotelManager : IDisposable,
        ICommandExecutor<UIShowHotelMenuCommand>
    {
        private readonly ScriptManager _scriptManager;
        private readonly SceneManager _sceneManager;

        public HotelManager(ScriptManager scriptManager, SceneManager sceneManager)
        {
            _scriptManager = Requires.IsNotNull(scriptManager, nameof(scriptManager));
            _sceneManager = Requires.IsNotNull(sceneManager, nameof(sceneManager));
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        private void Rest(string sceneCityName, string sceneName, uint afterRestTalkScript)
        {
            _sceneManager.LoadScene(sceneCityName, sceneName);
            CommandDispatcher<ICommand>.Instance.Dispatch(new GameStateChangeRequest(GameState.Cutscene));
            CommandDispatcher<ICommand>.Instance.Dispatch(new CameraFadeInCommand());
            _scriptManager.AddScript(afterRestTalkScript);
        }

        public void Execute(UIShowHotelMenuCommand command)
        {
            #if PAL3
            if (command.HotelScriptName.Equals("DealScript\\rest\\q01rest.txt", StringComparison.OrdinalIgnoreCase) &&
                _scriptManager.GetGlobalVariable(ScriptConstants.MainStoryVariableName) == 11101)
            {
                _scriptManager.SetGlobalVariable(ScriptConstants.MainStoryVariableName, 11200);
                Rest("Q01", "xn01", (uint)command.AfterRestScriptId);
            }
            else if (command.HotelScriptName.Equals("DealScript\\rest\\q13rest.txt", StringComparison.OrdinalIgnoreCase) &&
                     _scriptManager.GetGlobalVariable(ScriptConstants.MainStoryVariableName) == 120301)
            {
                _scriptManager.SetGlobalVariable(ScriptConstants.MainStoryVariableName, 120302);
                Rest("Q13", "n06", (uint)command.AfterRestScriptId);
                CommandDispatcher<ICommand>.Instance.Dispatch(new CameraSetDefaultTransformCommand(2));
            }
            else
            {
                // TODO: Remove this
                CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand("住店功能暂未实现，仅剧情需要的情况下才可交互。"));
            }
            #elif PAL3A
            if (command.HotelScriptName.Equals("DealScript\\rest\\q10rest.txt", StringComparison.OrdinalIgnoreCase) &&
                _scriptManager.GetGlobalVariable(ScriptConstants.MainStoryVariableName) == 140300)
            {
                Rest("q10", "n02y", 2007);
            }
            else
            {
                // TODO: Remove this
                CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand("住店功能暂未实现，仅剧情需要的情况下才可交互。"));
            }
            #endif
        }
    }
}