// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

#if PAL3A
namespace Pal3.Game.GameSystems.MiniGames
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Data;
    using Engine.Core.Abstraction;
    using Engine.Extensions;
    using Script.Waiter;
    using State;
    using TMPro;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    // The original two buttons (messages 104/105) return 1/2, not a random success value.
    public sealed class FortuneTellingMiniGame : IDisposable,
        ICommandExecutor<MiniGameStartFortuneTellingCommand>,
        ICommandExecutor<ScriptVarSetFortuneTellingResultCommand>,
        ICommandExecutor<ResetGameStateCommand>,
        ICommandExecutor<SceneLeavingCurrentSceneNotification>
    {
        private static readonly string[] Symbols = { "DE", "JING", "NAI", "WEI", "XIE", "YOU", "YUE" };
        private readonly GameResourceProvider _resources;
        private readonly GameStateManager _states;
        private readonly Script.IUserVariableStore<ushort, int> _variables;
        private readonly TMP_FontAsset _font;
        private readonly EventSystem _events;
        private readonly List<ITexture2D> _textures = new();
        private readonly Guid _locker = Guid.NewGuid();
        private GameObject _canvas;
        private GameObject _previousSelection;
        private GameState _previousState;
        private WaitUntilCanceled _waiter;
        private int _result;
        public bool IsOpen => _canvas != null;
        public int Result => _result;

        public FortuneTellingMiniGame(GameResourceProvider resources, GameStateManager states,
            Script.IUserVariableStore<ushort, int> variables,
            TMP_FontAsset font, EventSystem events)
        {
            _resources = resources;
            _states = states;
            _variables = variables;
            _font = font;
            _events = events;
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            Close(_result, false);
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Execute(MiniGameStartFortuneTellingCommand command)
        {
            if (IsOpen)
                throw new InvalidOperationException("A fortune selection is already open");
            _result = 0;
            _previousState = _states.GetCurrentState();
            _previousSelection = _events.currentSelectedGameObject;
            _waiter = new WaitUntilCanceled();
            Pal3.Instance.Execute(new ScriptRunnerAddWaiterRequest(_waiter));
            _states.AddGamePlayStateLocker(_locker);
            try
            {
                _canvas = new GameObject("FortuneTelling", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var canvas = _canvas.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                var scaler = _canvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(800, 600);
                scaler.matchWidthOrHeight = 0.5f;
                var background = Rect("Background", _canvas.transform, new Vector2(800, 600), Vector2.zero);
                background.anchorMin = Vector2.zero;
                background.anchorMax = Vector2.one;
                background.sizeDelta = Vector2.zero;
                var shade = background.gameObject.AddComponent<Image>();
                shade.color = new Color(0.08f, 0.04f, 0.12f, 0.96f);
                Label("请选择你的回应", background, new Vector2(0, 160), 30);
                var choices = Rect("Choices", background, new Vector2(0, 230), new Vector2(0, -10));
                choices.anchorMin = new Vector2(0.08f, 0.5f);
                choices.anchorMax = new Vector2(0.92f, 0.5f);
                var layout = choices.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 40;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                Button first = Choice(choices, command.Symbol1, 1);
                Button second = Choice(choices, command.Symbol2, 2);
                var firstNav = first.navigation;
                firstNav.mode = Navigation.Mode.Explicit;
                firstNav.selectOnRight = second;
                first.navigation = firstNav;
                var secondNav = second.navigation;
                secondNav.mode = Navigation.Mode.Explicit;
                secondNav.selectOnLeft = first;
                second.navigation = secondNav;
                _states.TryGoToState(GameState.UI);
                _events.SetSelectedGameObject(first.gameObject);
            }
            catch { Close(_result, true); throw; }
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private void Label(string text, Transform parent, Vector2 position, int size)
        {
            var label = Rect("Label", parent, new Vector2(700, 60), position).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = _font;
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }

        private Button Choice(Transform parent, int symbol, int result)
        {
            symbol = Mathf.Clamp(symbol, 0, 6);
            var rect = Rect("Choice" + result, parent, new Vector2(230, 230), Vector2.zero);
            var size = rect.gameObject.AddComponent<LayoutElement>();
            size.minWidth = 0;
            size.preferredWidth = size.preferredHeight = 230;
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.35f, 0.2f, 0.3f, 1);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = UI.UITheme.GetButtonColors();
            var provider = _resources.CreateTextureResourceProvider(FileConstants.UISceneFolderVirtualPath,
                useCache: false, usage: TextureUsage.Sprite);
            string textureName = "XZ_" + Symbols[symbol] + "0.tga";
            var texture = provider.GetTexture(textureName) ?? throw new FileNotFoundException(textureName);
            _textures.Add(texture);
            var frame = Rect("SymbolFrame", rect, Vector2.zero, Vector2.zero);
            frame.anchorMin = new Vector2(0.1f, 0.1f);
            frame.anchorMax = new Vector2(0.9f, 0.9f);
            var icon = Rect("Symbol", frame, Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
            var aspect = icon.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectRatio = 1;
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            icon.texture = texture.NativeObject as Texture;
            icon.raycastTarget = false;
            button.onClick.AddListener(() => Close(result, true));
            return button;
        }

        private void Close(int result, bool restoreState)
        {
            if (_waiter == null && _canvas == null)
                return;
            _result = result;
            if (_canvas != null)
            {
                _canvas.SetActive(false);
                UnityEngine.Object.Destroy(_canvas);
                _canvas = null;
            }
            foreach (var texture in _textures)
                texture.Destroy();
            _textures.Clear();
            _waiter?.CancelWait();
            _waiter = null;
            _states.RemoveGamePlayStateLocker(_locker);
            if (restoreState)
            {
                if (_events != null)
                    _events.SetSelectedGameObject(_previousSelection);
                if (!_states.TryGoToState(_previousState))
                    _states.TryGoToState(GameState.Cutscene);
            }
        }

        public void Execute(ScriptVarSetFortuneTellingResultCommand command) => _variables.Set(command.Variable, _result);
        public void Execute(ResetGameStateCommand command)
        {
            Close(_result, false);
            _result = 0;
        }

        public void Execute(SceneLeavingCurrentSceneNotification command) => Close(_result, false);
    }
}
#endif
