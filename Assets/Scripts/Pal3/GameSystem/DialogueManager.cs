// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.GameSystem
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.Animation;
    using Core.Utils;
    using Data;
    using Input;
    using MetaData;
    using Scene;
    using Script.Waiter;
    using State;
    using TMPro;
    using UI;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.InputSystem;
    using UnityEngine.InputSystem.DualShock;
    using UnityEngine.UI;

    public sealed class DialogueManager : MonoBehaviour,
        ICommandExecutor<DialogueRenderActorAvatarCommand>,
        ICommandExecutor<DialogueRenderTextCommand>,
        ICommandExecutor<DialogueAddSelectionsCommand>,
        ICommandExecutor<DialogueRenderTextWithTimeLimitCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private const float LIMIT_TIME_DIALOGUE_PLAYER_MAX_REACTION_TIME_IN_SECONDS = 4f;
        private const float DIALOGUE_SHOW_HIDE_ANIMATION_DURATION = 0.1f;
        private const float DIALOGUE_SHOW_HIDE_ANIMATION_Y_OFFSET = -30f;

        private const string INFORMATION_TEXT_COLOR_HEX = "#ffff05";

        private GameResourceProvider _resourceProvider;
        private GameStateManager _gameStateManager;
        private SceneManager _sceneManager;
        private InputManager _inputManager;
        private PlayerInputActions _inputActions;

        private EventSystem _eventSystem;
        private CanvasGroup _dialogueCanvasGroup;
        private Canvas _dialogueSelectionButtonsCanvas;
        private GameObject _dialogueSelectionButtonPrefab;
        private Image _dialogueBackgroundImage;

        private Image _avatarImageLeft;
        private Image _avatarImageRight;
        private TextMeshProUGUI _dialogueTextLeft;
        private TextMeshProUGUI _dialogueTextRight;
        private TextMeshProUGUI _dialogueTextDefault;

        private Texture2D _avatarTexture;
        private bool _isDialoguePresenting;
        private bool _isSkipDialogueRequested;
        private bool _isDialogueRenderingAnimationInProgress;

        private int _lastSelectedButtonIndex;
        private readonly List<GameObject> _selectionButtons = new();
        private double _totalTimeUsedBeforeSkippingTheLastDialogue;

        private DialogueRenderActorAvatarCommand _lastAvatarCommand;
        private readonly Queue<IEnumerator> _dialogueRenderQueue = new();

        public void Init(GameResourceProvider resourceProvider,
            GameStateManager gameStateManager,
            SceneManager sceneManager,
            InputManager inputManager,
            EventSystem eventSystem,
            CanvasGroup dialogueCanvasGroup,
            Image dialogueBackgroundImage,
            Image avatarImageLeft,
            Image avatarImageRight,
            TextMeshProUGUI textLeft,
            TextMeshProUGUI textRight,
            TextMeshProUGUI textDefault,
            Canvas dialogueSelectionButtonsCanvas,
            GameObject dialogueSelectionButtonPrefab)
        {
            _resourceProvider = Requires.IsNotNull(resourceProvider, nameof(resourceProvider));
            _gameStateManager = Requires.IsNotNull(gameStateManager, nameof(gameStateManager));
            _sceneManager = Requires.IsNotNull(sceneManager, nameof(sceneManager));
            _inputManager = Requires.IsNotNull(inputManager, nameof(inputManager));
            _eventSystem = Requires.IsNotNull(eventSystem, nameof(eventSystem));

            _dialogueCanvasGroup = Requires.IsNotNull(dialogueCanvasGroup, nameof(dialogueCanvasGroup));
            _dialogueBackgroundImage = Requires.IsNotNull(dialogueBackgroundImage, nameof(dialogueBackgroundImage));

            _avatarImageLeft = Requires.IsNotNull(avatarImageLeft, nameof(avatarImageLeft));
            _avatarImageRight = Requires.IsNotNull(avatarImageRight, nameof(avatarImageRight));

            _dialogueTextLeft = Requires.IsNotNull(textLeft, nameof(textLeft));
            _dialogueTextRight = Requires.IsNotNull(textRight, nameof(textRight));
            _dialogueTextDefault = Requires.IsNotNull(textDefault, nameof(textDefault));

            _dialogueSelectionButtonsCanvas = Requires.IsNotNull(dialogueSelectionButtonsCanvas, nameof(dialogueSelectionButtonsCanvas));
            _dialogueSelectionButtonPrefab = Requires.IsNotNull(dialogueSelectionButtonPrefab, nameof(dialogueSelectionButtonPrefab));

            _avatarImageLeft.preserveAspect = true;
            _avatarImageRight.preserveAspect = true;

            ResetUI();

            _inputActions = inputManager.GetPlayerInputActions();
            _inputActions.Cutscene.Continue.performed += SkipDialoguePerformed;
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
            _inputActions.Cutscene.Continue.performed -= SkipDialoguePerformed;
        }

        public int GetDialogueSelectionButtonIndex()
        {
            return _lastSelectedButtonIndex;
        }

        public bool PlayerReactedInTimeForLimitTimeDialogue()
        {
            return _totalTimeUsedBeforeSkippingTheLastDialogue < LIMIT_TIME_DIALOGUE_PLAYER_MAX_REACTION_TIME_IN_SECONDS;
        }

        IEnumerator TypeSentenceAsync(TextMeshProUGUI textUI, string sentence, float waitSecondsBeforeRenderingChar)
        {
            var charTypingAnimationDelay = new WaitForSeconds(waitSecondsBeforeRenderingChar);

            textUI.text = string.Empty;

            yield return charTypingAnimationDelay;

            var richText = string.Empty;
            foreach (var letter in sentence.ToCharArray())
            {
                if (richText.Length > 0)
                {
                    if (letter.Equals('>') && richText.Contains('>'))
                    {
                        textUI.text += richText + letter;
                        richText = string.Empty;
                        yield return charTypingAnimationDelay;
                        continue;
                    }

                    richText += letter;
                    continue;
                }
                else if (letter.Equals('<'))
                {
                    richText += letter;
                    continue;
                }

                if (_isDialogueRenderingAnimationInProgress == false) yield break;
                textUI.text += letter;
                yield return charTypingAnimationDelay;
            }
        }

        private void Update()
        {
            if (!_isDialoguePresenting && _dialogueRenderQueue.Count > 0)
            {
                _isDialoguePresenting = true;
                StartCoroutine(_dialogueRenderQueue.Dequeue());
            }
        }

        private TextMeshProUGUI GetRenderingTextUI(bool isAvatarPresented, bool isRightAligned)
        {
            if (!isAvatarPresented) return _dialogueTextDefault;
            else return isRightAligned ? _dialogueTextRight : _dialogueTextLeft;
        }

        private IEnumerator RenderDialogueTextWithAnimationAsync(TextMeshProUGUI dialogueTextUI,
            string text,
            float waitSecondsBeforeRenderingChar = 0.04f)
        {
            if (waitSecondsBeforeRenderingChar < Mathf.Epsilon)
            {
                dialogueTextUI.text = text;
            }
            else
            {
                _isDialogueRenderingAnimationInProgress = true;
                yield return null;
                yield return TypeSentenceAsync(dialogueTextUI, text, waitSecondsBeforeRenderingChar);
            }

            _isDialogueRenderingAnimationInProgress = false;
        }

        /// <summary>
        /// Break long dialogue into pieces
        /// Basically separate a dialogue into two pieces if there are more
        /// than three new line chars found in the dialogue text.
        /// </summary>
        /// <param name="text"></param>
        /// <returns>One or two sub dialogues</returns>
        private IEnumerable<string> GetSubDialoguesAsync(string text)
        {
            if (text.Contains('\n'))
            {
                var indexOfSecondNewLineChar = text.IndexOf('\n', text.IndexOf('\n') + 1);
                if (indexOfSecondNewLineChar != -1)
                {
                    var indexOfThirdNewLineChar = text.IndexOf('\n', indexOfSecondNewLineChar + 1);
                    if (indexOfThirdNewLineChar != -1 && indexOfThirdNewLineChar != text.Length)
                    {
                        var firstPart = text.Substring(0, indexOfThirdNewLineChar);
                        var secondPart = text.Substring(indexOfThirdNewLineChar, text.Length - indexOfThirdNewLineChar);
                        yield return firstPart;
                        yield return text.Substring(0, text.IndexOf('\n')) + secondPart;
                        yield break;
                    }
                }
            }

            yield return text;
        }

        public IEnumerator RenderDialogueAndWaitAsync(string text,
            bool trackReactionTime,
            WaitUntilCanceled waitUntilCanceled,
            DialogueRenderActorAvatarCommand avatarCommand = null)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(new DialogueRenderingStartedNotification());

            bool isRightAligned = true;
            bool isAvatarPresented = false;

            if (avatarCommand != null &&
                _sceneManager.GetCurrentScene().GetActor(avatarCommand.ActorId) is { } actor &&
                _resourceProvider.GetActorAvatarSprite(actor.Info.Name, avatarCommand.AvatarTextureName) is { } avatarSprite)
            {
                isRightAligned = avatarCommand.RightAligned == 1;

                if (isRightAligned)
                {
                    _avatarImageRight.color = Color.white;
                    _avatarImageRight.sprite = avatarSprite;
                }
                else
                {
                    _avatarImageLeft.color = Color.white;
                    _avatarImageLeft.sprite = avatarSprite;
                }

                isAvatarPresented = true;
            }

            TextMeshProUGUI dialogueTextUI = GetRenderingTextUI(isAvatarPresented, isRightAligned);

            // TODO: when trackReactionTime set to true, we should also show alert on UI
            // to let player know

            var timer = new Stopwatch();
            timer.Start();
            _dialogueBackgroundImage.enabled = true;
            _dialogueCanvasGroup.alpha = 0f;
            _dialogueCanvasGroup.enabled = true;
            _isSkipDialogueRequested = false;

            yield return StartDialogueAnimationAsync(true);
            _isSkipDialogueRequested = false; // Ignore skip request during dialogue rendering animation

            foreach (var dialogue in GetSubDialoguesAsync(text))
            {
                IEnumerator renderDialogue = RenderDialogueTextWithAnimationAsync(dialogueTextUI, dialogue);

                StartCoroutine(renderDialogue);

                yield return SkipDialogueRequestedAsync();

                if (_isDialogueRenderingAnimationInProgress)
                {
                    _isDialogueRenderingAnimationInProgress = false;
                    StopCoroutine(renderDialogue);
                    dialogueTextUI.text = dialogue;
                    yield return SkipDialogueRequestedAsync();
                }
            }

            timer.Stop();
            if (trackReactionTime)
            {
                _totalTimeUsedBeforeSkippingTheLastDialogue = timer.Elapsed.TotalSeconds;
            }

            yield return StartDialogueAnimationAsync(false);

            ResetUI();
            waitUntilCanceled.CancelWait();
            _isDialoguePresenting = false;
        }

        private IEnumerator StartDialogueAnimationAsync(bool showDialogue)
        {
            const float yOffset = DIALOGUE_SHOW_HIDE_ANIMATION_Y_OFFSET;
            Transform dialogueCanvasGroupTransform = _dialogueCanvasGroup.transform;
            Vector3 finalPosition = dialogueCanvasGroupTransform.position;
            Vector3 startPosition = finalPosition + new Vector3(0f, yOffset, 0);

            float startValue = showDialogue ? 0f : 1f;
            float endValue = showDialogue ? 1f : 0f;

            if (showDialogue)
            {
                dialogueCanvasGroupTransform.position = startPosition;
            }

            yield return AnimationHelper.EnumerateValueAsync(startValue, endValue, DIALOGUE_SHOW_HIDE_ANIMATION_DURATION,
                AnimationCurveType.Linear, value =>
                {
                    _dialogueCanvasGroup.transform.position = finalPosition + new Vector3(0f, yOffset * (1 - value), 0);
                    _dialogueCanvasGroup.alpha = value;
                });

            _dialogueCanvasGroup.transform.position = finalPosition; // Always set to final position
            _dialogueCanvasGroup.alpha = showDialogue ? 1f : 0f;
        }

        private void ResetUI()
        {
            _dialogueCanvasGroup.alpha = 0f;
            _dialogueCanvasGroup.enabled = false;

            _dialogueTextLeft.text = string.Empty;
            _dialogueTextRight.text = string.Empty;
            _dialogueTextDefault.text = string.Empty;

            _dialogueBackgroundImage.enabled = false;

            _avatarImageLeft.color = new Color(0f, 0f, 0f, 0f);
            _avatarImageRight.color = new Color(0f, 0f, 0f, 0f);
            _avatarImageLeft.sprite = null;
            _avatarImageRight.sprite = null;

            _dialogueSelectionButtonsCanvas.enabled = false;

            foreach (GameObject button in _selectionButtons)
            {
                button.GetComponentInChildren<Button>().onClick.RemoveAllListeners();
                Destroy(button);
            }
            _selectionButtons.Clear();
        }

        IEnumerator SkipDialogueRequestedAsync()
        {
            yield return new WaitUntil(() => _isSkipDialogueRequested);
            _isSkipDialogueRequested = false;
        }

        private string GetDisplayText(string text)
        {
            // Easter egg + dev notes
            #if PAL3
            if (text.Equals("韩用：\\n知道吗？\\i使用鼠标滚轮可以旋转镜头\\r，这样就不怕人物被挡住了。"))
            #elif PAL3A
            if (text.Equals("蜀中第一丐：\\n我蜀中第一丐四海为家，过的桥比你走的路还多，有什么不明白的尽管问～～\\n" +
                                "今天心情好，就再多透露点秘密给你吧，假如在城镇、迷宫中人物被挡住了，可以试试\\i用鼠标滚轮旋转镜头\\r，会有不错的效果哦。" +
                                "另外，画面\\i左下角\\r那东西叫作\\i“司南”\\r，只要点一下，就能看到地图，认清方向了。"))
            #endif
            {
                text = $"知道吗？您现在玩的{GameConstants.AppNameCNFull}是由\\i柒才\\r使用C#/Unity开发的复刻版，免费开源且支持全平台。" +
                       "如果您是花钱得到的，那么恭喜您成为盗版游戏的受害者。" +
                       $"当前游戏还在开发中，包括战斗在内的很多功能尚未实现，请耐心等待，也欢迎加入本游戏QQ群与作者联系：\\i252315306\\r，" +
                       "或者您也可以在B站关注Up主\\i@柒才\\r！";
            }

            var formattedText = text.Replace("\\n", "\n");

            return ReplaceStringWithPatternForEachChar(formattedText,
                "\\i", "\\r",
                $"<color={INFORMATION_TEXT_COLOR_HEX}>", "</color>");
        }

        private string ReplaceStringWithPatternForEachChar(string str,
            string startPattern,
            string endPattern,
            string charStartPattern,
            string charEndPattern)
        {
            var newStr = string.Empty;

            var currentIndex = 0;
            var startOfInformation = str.IndexOf(startPattern, StringComparison.Ordinal);
            while (startOfInformation != -1)
            {
                var endOfInformation = str.IndexOf(endPattern, startOfInformation, StringComparison.Ordinal);

                newStr += str.Substring(currentIndex, startOfInformation - currentIndex);

                foreach (var ch in str.Substring(
                             startOfInformation + startPattern.Length,
                             endOfInformation - startOfInformation - startPattern.Length))
                {
                    newStr += $"{charStartPattern}{ch}{charEndPattern}";
                }

                currentIndex = endOfInformation + endPattern.Length;
                startOfInformation = str.IndexOf(
                    startPattern, currentIndex, StringComparison.Ordinal);
            }

            newStr += str.Substring(currentIndex, str.Length - currentIndex);

            return newStr;
        }

        private void SkipDialoguePerformed(InputAction.CallbackContext _)
        {
            if (_dialogueSelectionButtonsCanvas.enabled) return;
            _isSkipDialogueRequested = true;
        }

        public void Execute(DialogueRenderTextCommand command)
        {
            var skipDialogueWaiter = new WaitUntilCanceled();
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(skipDialogueWaiter));
            DialogueRenderActorAvatarCommand avatarCommand = _lastAvatarCommand;
            _dialogueRenderQueue.Enqueue(RenderDialogueAndWaitAsync(
                GetDisplayText(command.DialogueText), false, skipDialogueWaiter, avatarCommand));
            _lastAvatarCommand = null;
        }

        public void Execute(DialogueRenderTextWithTimeLimitCommand command)
        {
            var skipDialogueWaiter = new WaitUntilCanceled();
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(skipDialogueWaiter));
            DialogueRenderActorAvatarCommand avatarCommand = _lastAvatarCommand;
            _dialogueRenderQueue.Enqueue(RenderDialogueAndWaitAsync(
                GetDisplayText(command.DialogueText), true, skipDialogueWaiter, avatarCommand));
            _lastAvatarCommand = null;
        }

        public void Execute(DialogueRenderActorAvatarCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID) return;
            _lastAvatarCommand = command;
        }

        private string GetSelectionDisplayText(object selection)
        {
            var selectionString = (string)selection;

            if (selectionString.EndsWith("；") || selectionString.EndsWith("。")) selectionString = selectionString[..^1];

            if (selectionString.Contains('.'))
            {
                var numberStr = selectionString[..selectionString.IndexOf('.')];
                if (int.TryParse(numberStr, out _))
                {
                    return selectionString[(selectionString.IndexOf('.') + 1)..];
                }
            }

            if (selectionString.Contains('、'))
            {
                var numberStr = selectionString[..selectionString.IndexOf('、')];
                if (int.TryParse(numberStr, out _))
                {
                    return selectionString[(selectionString.IndexOf('、') + 1)..];
                }
            }

            // I don't think there will be more than 20 options, so let's start with 20
            for (var i = 20; i >= 0; i--)
            {
                var intStr = i.ToString();
                if (selectionString.StartsWith(intStr) && !string.Equals(selectionString, intStr))
                {
                    return selectionString[intStr.Length..];
                }
            }

            return selectionString;
        }

        public void Execute(DialogueAddSelectionsCommand command)
        {
            _gameStateManager.GoToState(GameState.MenuShowing);

            var waiter = new WaitUntilCanceled();
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(waiter));

            Transform canvasTransform = _dialogueSelectionButtonsCanvas.transform;
            for (var i = 0; i < command.Selections.Count; i++)
            {
                GameObject selectionButton = Instantiate(_dialogueSelectionButtonPrefab, canvasTransform);
                var buttonTextUI = selectionButton.GetComponentInChildren<TextMeshProUGUI>();
                buttonTextUI.text = GetSelectionDisplayText(command.Selections[i]);
                var buttonIndex = i;
                var button = selectionButton.GetComponentInChildren<Button>();
                button.colors = UITheme.GetButtonColors();
                button.onClick
                    .AddListener(delegate
                    {
                        SelectionButtonClicked(buttonIndex);
                        waiter.CancelWait();
                        _gameStateManager.GoToPreviousState();
                    });
                _selectionButtons.Add(selectionButton);
            }

            // Setup button navigation
            void ConfigureButtonNavigation(Button button, int index, int count)
            {
                Navigation buttonNavigation = button.navigation;
                buttonNavigation.mode = Navigation.Mode.Explicit;

                int upIndex = index == 0 ? count - 1 : index - 1;
                int downIndex = index == count - 1 ? 0 : index + 1;

                buttonNavigation.selectOnUp = _selectionButtons[upIndex].GetComponentInChildren<Button>();
                buttonNavigation.selectOnDown = _selectionButtons[downIndex].GetComponentInChildren<Button>();

                button.navigation = buttonNavigation;
            }

            for (var i = 0; i < command.Selections.Count; i++)
            {
                var button = _selectionButtons[i].GetComponentInChildren<Button>();
                ConfigureButtonNavigation(button, i, command.Selections.Count);
            }

            var firstButton = _selectionButtons.First().GetComponentInChildren<Button>();

            InputDevice lastActiveInputDevice = _inputManager.GetLastActiveInputDevice();
            if (lastActiveInputDevice == Keyboard.current ||
                lastActiveInputDevice == Gamepad.current ||
                lastActiveInputDevice == DualShockGamepad.current)
            {
                _eventSystem.firstSelectedGameObject = firstButton.gameObject;
                firstButton.Select();
            }
            else
            {
                _eventSystem.firstSelectedGameObject = null;
            }

            _dialogueCanvasGroup.alpha = 1f;
            _dialogueCanvasGroup.enabled = true;
            _dialogueSelectionButtonsCanvas.enabled = true;
        }

        private void SelectionButtonClicked(int index)
        {
            _lastSelectedButtonIndex = index;
            ResetUI();
        }

        public void Execute(ResetGameStateCommand command)
        {
            _lastAvatarCommand = null;
            _totalTimeUsedBeforeSkippingTheLastDialogue = 0f;
            _dialogueRenderQueue.Clear();
            ResetUI();
        }
    }
}