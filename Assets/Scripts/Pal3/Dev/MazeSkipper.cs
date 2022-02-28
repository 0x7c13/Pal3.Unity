// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Dev
{
    using System.Collections.Generic;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataReader.Scn;
    using Scene;
    using State;
    using UnityEngine;
    using UnityEngine.UI;

    public class MazeSkipper :
        ICommandExecutor<ScenePostLoadingNotification>,
        ICommandExecutor<GameStateChangedNotification>
    {
        private readonly GameStateManager _gameStateManager;
        private readonly SceneManager _sceneManager;
        private readonly CanvasGroup _mazeSkipperCanvasGroup;
        private readonly Button _mazeEntranceButton;
        private readonly Button _mazeExitButton;

        // _0 for entrance, _1 for exit
        private readonly Dictionary<string, IList<ICommand>> _skipperCommands = new()
        {
            { "m01_1_0", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 242, 41)
            }},
            { "m01_1_1", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 193, 260)
            }},
            { "m02_1_0", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 397, 50)
            }},
            { "m02_1_1", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 408, 275)
            }},
            { "m03_1_0", new List<ICommand>()
            {
                new ActorSetNavLayerCommand(-1, 0),
                new ActorSetTilePositionCommand(-1, 15, 34)
            }},
            { "m03_1_1", new List<ICommand>()
            {
                new ActorSetNavLayerCommand(-1, 0),
                new ActorSetTilePositionCommand(-1, 354, 372)
            }},
            { "m06_2_0", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 184, 203)
            }},
            { "m06_2_1", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 149, 29)
            }},
            { "m06_4_0", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 9, 45)
            }},
            { "m06_4_1", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 233, 31)
            }},
            { "m08_1a_0", new List<ICommand>()
            {
                new ActorSetNavLayerCommand(-1, 0),
                new ActorSetTilePositionCommand(-1, 100, 88)
            }},
            { "m08_1a_1", new List<ICommand>()
            {
                new ActorSetNavLayerCommand(-1, 1),
                new ActorSetTilePositionCommand(-1, 22, 53)
            }},
            { "m08_2_0", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 51, 83)
            }},
            { "m08_2_1", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 170, 28)
            }},
            { "m09_1_0", new List<ICommand>()
            {
                new ActorSetNavLayerCommand(-1, 1),
                new ActorSetTilePositionCommand(-1, 27, 222)
            }},
            { "m09_1_1", new List<ICommand>()
            {
                new ActorSetNavLayerCommand(-1, 0),
                new ActorSetTilePositionCommand(-1, 155, 117)
            }},
            { "m10_1_0", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 40, 58)
            }},
            { "m10_1_1", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 232, 36)
            }},
            { "m10_2_0", new List<ICommand>()
            {
                new ActorSetNavLayerCommand(-1, 1),
                new ActorSetTilePositionCommand(-1, 24, 18)
            }},
            { "m10_2_1", new List<ICommand>()
            {
                new ActorSetNavLayerCommand(-1, 1),
                new ActorSetTilePositionCommand(-1, 240, 190)
            }},
            { "m10_3_0", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 197, 18)
            }},
            { "m10_3_1", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 35, 243)
            }},
            { "m11_1_0", new List<ICommand>()
            {
                new ActorSetNavLayerCommand(-1, 1),
                new ActorSetTilePositionCommand(-1, 266, 398)
            }},
            { "m11_1_1", new List<ICommand>()
            {
                new ActorSetNavLayerCommand(-1, 1),
                new ActorSetTilePositionCommand(-1, 361, 471)
            }},
            { "m11_2_0", new List<ICommand>()
            {
                new ActorSetNavLayerCommand(-1, 0),
                new ActorSetTilePositionCommand(-1, 180, 358)
            }},
            { "m11_2_1", new List<ICommand>()
            {
                new ActorSetNavLayerCommand(-1, 0),
                new ActorSetTilePositionCommand(-1, 195, 106)
            }},
            { "m15_2_0", new List<ICommand>()
            {
                new ActorSetNavLayerCommand(-1, 0),
                new ActorSetTilePositionCommand(-1, 227, 169)
            }},
            { "m15_2_1", new List<ICommand>()
            {
                new ActorSetNavLayerCommand(-1, 0),
                new ActorSetTilePositionCommand(-1, 18, 171)
            }},
            { "m15_c_0", new List<ICommand>()
            {
                new ActorSetNavLayerCommand(-1, 1),
                new ActorSetTilePositionCommand(-1, 16, 109)
            }},
            { "m15_c_1", new List<ICommand>()
            {
                new SceneLoadCommand("M15", "A"),
                new ActorSetNavLayerCommand(-1, 1),
                new ActorSetTilePositionCommand(-1, 2, 15)
            }},
            { "m16_1_0", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 242, 28)
            }},
            { "m16_1_1", new List<ICommand>()
            {
                new SceneLoadCommand("M16", "6"),
                new ActorSetTilePositionCommand(-1, 28, 62)
            }},
            { "m17_2_0", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 81, 92)
            }},
            { "m17_2_1", new List<ICommand>()
            {
                new SceneLoadCommand("M17", "10"),
                new ActorSetTilePositionCommand(-1, 53, 41)
            }},
            { "m18_1_0", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 48, 54)
            }},
            { "m18_1_1", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 313, 264)
            }},
            { "m19_1_0", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 43, 19)
            }},
            { "m19_1_1", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 210, 280)
            }},
            { "m19_2_0", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 206, 206)
            }},
            { "m19_2_1", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 264, 162)
            }},
            { "m19_3_0", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 185, 200)
            }},
            { "m19_3_1", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 37, 8)
            }},
            { "m20_1_0", new List<ICommand>()
            {
                new ActorSetNavLayerCommand(-1, 0),
                new ActorSetTilePositionCommand(-1, 40, 32)
            }},
            { "m20_1_1", new List<ICommand>()
            {
                new ActorSetNavLayerCommand(-1, 0),
                new ActorSetTilePositionCommand(-1, 207, 255)
            }},
            { "m20_2_0", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 73, 39)
            }},
            { "m20_2_1", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 14, 69)
            }},
            { "m20_3_0", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 33, 21)
            }},
            { "m20_3_1", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 80, 79)
            }},
            { "m20_4_0", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 83, 16)
            }},
            { "m20_4_1", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 16, 52)
            }},
            { "m21_1_0", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 23, 71)
            }},
            { "m21_1_1", new List<ICommand>()
            {
                new SceneLoadCommand("M21", "7"),
                new ActorSetTilePositionCommand(-1, 157, 105)
            }},
            { "m22_3_0", new List<ICommand>()
            {
                new ActorSetNavLayerCommand(-1, 0),
                new ActorSetTilePositionCommand(-1, 239, 78)
            }},
            { "m22_3_1", new List<ICommand>()
            {
                new ActorSetNavLayerCommand(-1, 0),
                new ActorSetTilePositionCommand(-1, 63, 14)
            }},
            { "m22_4_0", new List<ICommand>()
            {
                new ActorSetNavLayerCommand(-1, 0),
                new ActorSetTilePositionCommand(-1, 301, 33)
            }},
            { "m22_4_1", new List<ICommand>()
            {
                new ActorSetNavLayerCommand(-1, 0),
                new ActorSetTilePositionCommand(-1, 26, 204)
            }},
            { "m23_2_0", new List<ICommand>()
            {
                new ActorSetNavLayerCommand(-1, 0),
                new ActorSetTilePositionCommand(-1, 157, 70)
            }},
            { "m23_2_1", new List<ICommand>()
            {
                new ActorSetNavLayerCommand(-1, 0),
                new ActorSetTilePositionCommand(-1, 244, 207)
            }},
            { "m23_3_0", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 178, 121)
            }},
            { "m23_3_1", new List<ICommand>()
            {
                new SceneLoadCommand("M23", "4"),
                new ActorSetTilePositionCommand(-1, 87, 7)
            }},
            { "m23_5_0", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 112, 13)
            }},
            { "m23_5_1", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 122, 176)
            }},
            { "m24_1_0", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 53, 303)
            }},
            { "m24_1_1", new List<ICommand>()
            {
                new SceneLoadCommand("M24", "4"),
                new ActorSetTilePositionCommand(-1, 82, 39)
            }},
            { "m24_4_0", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 86, 36)
            }},
            { "m24_4_1", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 144, 13)
            }},
            { "m24_6_0", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 12, 18)
            }},
            { "m24_6_1", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 156, 166)
            }},
            { "m25_1_0", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 25, 107)
            }},
            { "m25_1_1", new List<ICommand>()
            {
                new ActorSetTilePositionCommand(-1, 258, 148)
            }},
            { "m25_2_0", new List<ICommand>()
            {
                new ActorSetNavLayerCommand(-1, 0),
                new ActorSetTilePositionCommand(-1, 21, 112)
            }},
            { "m25_2_1", new List<ICommand>()
            {
                new ActorSetNavLayerCommand(-1, 1),
                new ActorSetTilePositionCommand(-1, 281, 87)
            }},
        };

        public MazeSkipper(GameStateManager gameStateManager,
            SceneManager sceneManager,
            CanvasGroup mazeSkipperCanvasGroup,
            Button mazeEntranceButton,
            Button mazeExitButton)
        {
            _gameStateManager = gameStateManager;
            _sceneManager = sceneManager;
            _mazeSkipperCanvasGroup = mazeSkipperCanvasGroup;
            _mazeEntranceButton = mazeEntranceButton;
            _mazeExitButton = mazeExitButton;

            _mazeSkipperCanvasGroup.alpha = 0f;
            _mazeSkipperCanvasGroup.interactable = false;

            _mazeEntranceButton.onClick
                .AddListener(EntranceButtonClicked);
            _mazeExitButton.onClick
                .AddListener(ExitButtonClicked);

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void EntranceButtonClicked()
        {
            if (_gameStateManager.GetCurrentState() != GameState.Gameplay) return;
            if (_sceneManager.GetCurrentScene() is not { } currentScene) return;
            foreach (var command in _skipperCommands[GetEntranceCommandHashKey(currentScene.GetSceneInfo())])
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(command);
            }
        }

        private void ExitButtonClicked()
        {
            if (_gameStateManager.GetCurrentState() != GameState.Gameplay) return;
            if (_sceneManager.GetCurrentScene() is not { } currentScene) return;
            foreach (var command in _skipperCommands[GetExitCommandHashKey(currentScene.GetSceneInfo())])
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(command);
            }
        }

        public void Dispose()
        {
            _mazeEntranceButton.onClick
                .RemoveListener(EntranceButtonClicked);
            _mazeExitButton.onClick
                .RemoveListener(ExitButtonClicked);

            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        private string GetEntranceCommandHashKey(ScnSceneInfo sceneInfo)
        {
            return $"{sceneInfo.CityName}_{sceneInfo.Name}_0".ToLower();
        }

        private string GetExitCommandHashKey(ScnSceneInfo sceneInfo)
        {
            return $"{sceneInfo.CityName}_{sceneInfo.Name}_1".ToLower();
        }

        public void Execute(GameStateChangedNotification command)
        {
            if (command.NewState != GameState.Gameplay)
            {
                _mazeSkipperCanvasGroup.alpha = 0f;
                _mazeSkipperCanvasGroup.interactable = false;
                return;
            }

            if (_sceneManager.GetCurrentScene() is not { } currentScene) return;

            var currentSceneInfo = currentScene.GetSceneInfo();
            if (command.NewState == GameState.Gameplay &&
                _skipperCommands.ContainsKey(GetEntranceCommandHashKey(currentSceneInfo)) &&
                _skipperCommands.ContainsKey(GetExitCommandHashKey(currentSceneInfo)))
            {
                _mazeSkipperCanvasGroup.alpha = 1f;
                _mazeSkipperCanvasGroup.interactable = true;
            }
        }

        public void Execute(ScenePostLoadingNotification command)
        {
            if (_gameStateManager.GetCurrentState() == GameState.Gameplay &&
                _skipperCommands.ContainsKey(GetEntranceCommandHashKey(command.SceneInfo)) &&
                _skipperCommands.ContainsKey(GetExitCommandHashKey(command.SceneInfo)))
            {
                _mazeSkipperCanvasGroup.alpha = 1f;
                _mazeSkipperCanvasGroup.interactable = true;
            }
            else
            {
                _mazeSkipperCanvasGroup.alpha = 0f;
                _mazeSkipperCanvasGroup.interactable = false;
            }
        }
    }
}