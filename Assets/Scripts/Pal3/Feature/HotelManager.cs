// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Feature
{
    using System;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using MetaData;
    using Scene;
    using Script;
    using State;
    using UnityEngine;

    public sealed class HotelManager : MonoBehaviour,
        ICommandExecutor<UIShowHotelMenuCommand>
    {
        private ScriptManager _scriptManager;
        private SceneManager _sceneManager;

        public void Init(ScriptManager scriptManager, SceneManager sceneManager)
        {
            _scriptManager = scriptManager ?? throw new ArgumentNullException(nameof(scriptManager));
            _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Rest(string sceneFileName, string sceneName, uint afterRestTalkScript)
        {
            _sceneManager.LoadScene(sceneFileName, sceneName);
            CommandDispatcher<ICommand>.Instance.Dispatch(new GameStateChangeRequest(GameState.Cutscene));
            CommandDispatcher<ICommand>.Instance.Dispatch(new CameraFadeInCommand());
            _scriptManager.AddScript(afterRestTalkScript);
        }

        public void Execute(UIShowHotelMenuCommand command)
        {
            var vars = _scriptManager.GetGlobalVariables();

            #if PAL3
            if (command.HotelScriptName.Equals("DealScript\\rest\\q01rest.txt",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (vars[ScriptConstants.MainStoryVariableName] == 11101)
                {
                    vars[ScriptConstants.MainStoryVariableName] = 11200;
                    Rest("Q01", "xn01", (uint)command.AfterRestScriptId);
                }
            }
            else if (command.HotelScriptName.Equals("DealScript\\rest\\q13rest.txt",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (vars[ScriptConstants.MainStoryVariableName] == 120301)
                {
                    vars[ScriptConstants.MainStoryVariableName] = 120302;
                    Rest("Q13", "n06", (uint)command.AfterRestScriptId);
                    CommandDispatcher<ICommand>.Instance.Dispatch(new CameraSetDefaultTransformCommand(2));
                }
            }
            #elif PAL3A
            if (command.HotelScriptName.Equals("DealScript\\rest\\q10rest.txt",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (vars[ScriptConstants.MainStoryVariableName] == 140300)
                {
                    Rest("q10", "n02y", 2007);
                }
            }
            #endif
        }
    }
}