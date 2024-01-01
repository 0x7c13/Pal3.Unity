// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.GameSystems.Rest
{
    using System;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Utilities;
    using Scene;
    using Script;
    using State;

    public sealed class HotelManager : IDisposable,
        ICommandExecutor<UIShowHotelMenuCommand>
    {
        private readonly IUserVariableStore<ushort, int> _userVariableStore;
        private readonly ScriptManager _scriptManager;
        private readonly SceneManager _sceneManager;

        public HotelManager(IUserVariableStore<ushort, int> userVariableStore,
            ScriptManager scriptManager,
            SceneManager sceneManager)
        {
            _userVariableStore = Requires.IsNotNull(userVariableStore, nameof(userVariableStore));
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
            Pal3.Instance.Execute(new GameStateChangeRequest(GameState.Cutscene));
            Pal3.Instance.Execute(new CameraFadeInCommand());
            _scriptManager.AddScript(afterRestTalkScript);
        }

        public void Execute(UIShowHotelMenuCommand command)
        {
            #if PAL3
            if (command.HotelScriptName.Equals("DealScript\\rest\\q01rest.txt", StringComparison.OrdinalIgnoreCase) &&
                _userVariableStore.Get(ScriptConstants.MainStoryVariableId) == 11101)
            {
                _userVariableStore.Set(ScriptConstants.MainStoryVariableId, 11200);
                Rest("Q01", "xn01", (uint)command.AfterRestScriptId);
            }
            else if (command.HotelScriptName.Equals("DealScript\\rest\\q13rest.txt", StringComparison.OrdinalIgnoreCase) &&
                     _userVariableStore.Get(ScriptConstants.MainStoryVariableId) == 120301)
            {
                _userVariableStore.Set(ScriptConstants.MainStoryVariableId, 120302);
                Rest("Q13", "n06", (uint)command.AfterRestScriptId);
                Pal3.Instance.Execute(new CameraSetDefaultTransformCommand(2));
            }
            else
            {
                // TODO: Remove this
                Pal3.Instance.Execute(new UIDisplayNoteCommand("住店功能暂未实现，仅剧情需要的情况下才可交互。"));
            }
            #elif PAL3A
            if (command.HotelScriptName.Equals("DealScript\\rest\\q10rest.txt", StringComparison.OrdinalIgnoreCase) &&
                _userVariableStore.Get(ScriptConstants.MainStoryVariableId) == 140300)
            {
                Rest("q10", "n02y", 2007);
            }
            else
            {
                // TODO: Remove this
                Pal3.Instance.Execute(new UIDisplayNoteCommand("住店功能暂未实现，仅剧情需要的情况下才可交互。"));
            }
            #endif
        }
    }
}