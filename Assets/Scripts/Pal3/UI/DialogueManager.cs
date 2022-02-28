// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.UI
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.Utils;
    using Data;
    using Input;
    using MetaData;
    using Scene;
    using Script.Waiter;
    using State;
    using TMPro;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.InputSystem;
    using UnityEngine.UI;

    public class DialogueManager : MonoBehaviour,
        ICommandExecutor<DialogueRenderActorAvatarCommand>,
        ICommandExecutor<DialogueRenderTextCommand>,
        ICommandExecutor<DialogueAddSelectionsCommand>,
        ICommandExecutor<DialogueRenderTextWithTimeLimitCommand>
    {
        private const float LIMIT_TIME_DIALOGUE_PLAYER_MAX_REACTION_TIME = 4f;
        private const string INFORMATION_TEXT_COLOR_HEX = "#ffff05";

        private GameResourceProvider _resourceProvider;
        private GameStateManager _gameStateManager;
        private SceneManager _sceneManager;
        private InputManager _inputManager;
        private PlayerInputActions _inputActions;
        private WaitUntilCanceled _skipDialogueWaiter;

        private EventSystem _eventSystem;
        private Canvas _dialogueCanvas;
        private Canvas _dialogueSelectionButtonsCanvas;
        private GameObject _dialogueSelectionButtonPrefab;
        private Image _dialogueBackgroundImage;

        private Image _avatarImageLeft;
        private Image _avatarImageRight;
        private TextMeshProUGUI _dialogueTextLeft;
        private TextMeshProUGUI _dialogueTextRight;
        private TextMeshProUGUI _dialogueTextDefault;

        private Texture2D _avatarTexture;
        private bool _isRightAligned;
        private bool _skipDialogueRequested;
        private bool _isRenderingDialogue;
        private bool _isAvatarPresented;

        private int _lastSelectedButtonIndex;
        private readonly List<GameObject> _selectionButtons = new();
        private double _totalTimeUsedBeforeSkippingTheLastDialogue;

        public void Init(GameResourceProvider resourceProvider,
            GameStateManager gameStateManager,
            SceneManager sceneManager,
            InputManager inputManager,
            EventSystem eventSystem,
            Canvas dialogueCanvas,
            Image dialogueBackgroundImage,
            Image avatarImageLeft,
            Image avatarImageRight,
            TextMeshProUGUI textLeft,
            TextMeshProUGUI textRight,
            TextMeshProUGUI textDefault,
            Canvas dialogueSelectionButtonsCanvas,
            GameObject dialogueSelectionButtonPrefab)
        {
            _dialogueCanvas = dialogueCanvas;
            _gameStateManager = gameStateManager;
            _dialogueBackgroundImage = dialogueBackgroundImage;

            _avatarImageLeft = avatarImageLeft;
            _avatarImageRight = avatarImageRight;

            _dialogueTextLeft = textLeft;
            _dialogueTextRight = textRight;
            _dialogueTextDefault = textDefault;

            _dialogueSelectionButtonsCanvas = dialogueSelectionButtonsCanvas;
            _dialogueSelectionButtonPrefab = dialogueSelectionButtonPrefab;

            _avatarImageLeft.preserveAspect = true;
            _avatarImageRight.preserveAspect = true;

            // Disable dialogue background blur effect on handheld devices
            if (Utility.IsHandheldDevice())
            {
                _dialogueBackgroundImage.material = null;
                _dialogueBackgroundImage.color = new Color(0f, 0f, 0f, 0.65f);
            }

            ResetUI();

            _eventSystem = eventSystem;
            _resourceProvider = resourceProvider;
            _sceneManager = sceneManager;
            _inputManager = inputManager;
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
            return _totalTimeUsedBeforeSkippingTheLastDialogue < LIMIT_TIME_DIALOGUE_PLAYER_MAX_REACTION_TIME;
        }

        IEnumerator TypeSentence(TextMeshProUGUI textUI, string sentence, float waitSecondsBeforeRenderingChar)
        {
            textUI.text = string.Empty;

            yield return new WaitForSeconds(waitSecondsBeforeRenderingChar);

            var richText = string.Empty;
            foreach (var letter in sentence.ToCharArray())
            {
                if (richText.Length > 0)
                {
                    if (letter.Equals('>') && richText.Contains('>'))
                    {
                        textUI.text += richText + letter;
                        richText = string.Empty;
                        yield return new WaitForSeconds(waitSecondsBeforeRenderingChar);
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

                if (_isRenderingDialogue == false) yield break;
                textUI.text += letter;
                yield return new WaitForSeconds(waitSecondsBeforeRenderingChar);
            }
        }

        private TextMeshProUGUI GetRenderingTextUI()
        {
            if (!_isAvatarPresented) return _dialogueTextDefault;
            else return _isRightAligned ? _dialogueTextRight : _dialogueTextLeft;
        }

        private IEnumerator RenderDialogueTextWithAnimation(string text,
            float waitSecondsBeforeRenderingChar = 0.05f)
        {
            TextMeshProUGUI dialogueTextUI = GetRenderingTextUI();

            if (waitSecondsBeforeRenderingChar < Mathf.Epsilon)
            {
                dialogueTextUI.text = text;
            }
            else
            {
                _isRenderingDialogue = true;
                yield return null;
                yield return TypeSentence(dialogueTextUI, text, waitSecondsBeforeRenderingChar);
            }

            _isRenderingDialogue = false;
        }

        /// <summary>
        /// Break long dialogue into pieces
        /// Basically separate a dialogue into two pieces if there are more
        /// than three new line chars found in the dialogue text.
        /// </summary>
        /// <param name="text"></param>
        /// <returns>One or two sub dialogues</returns>
        private IEnumerable<string> GetSubDialogues(string text)
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

        public IEnumerator RenderDialogueAndWait(string text, bool trackReactionTime)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(new DialogueRenderingStartedNotification());
            // TODO: when trackReactionTime set to true, we should also show alert on UI
            // to let player know

            var timer = new Stopwatch();
            timer.Start();
            _dialogueBackgroundImage.enabled = true;
            _dialogueCanvas.enabled = true;
            _skipDialogueRequested = false;

            foreach (var dialogue in GetSubDialogues(text))
            {
                var renderDialogue = RenderDialogueTextWithAnimation(dialogue);

                StartCoroutine(renderDialogue);

                yield return SkipDialogueRequested();

                if (_isRenderingDialogue)
                {
                    _isRenderingDialogue = false;
                    StopCoroutine(renderDialogue);
                    TextMeshProUGUI dialogueTextUI = GetRenderingTextUI();
                    dialogueTextUI.text = dialogue;
                    yield return SkipDialogueRequested();
                }
            }

            timer.Stop();
            if (trackReactionTime)
            {
                _totalTimeUsedBeforeSkippingTheLastDialogue = timer.Elapsed.TotalSeconds;
            }
            ResetUI();
            _skipDialogueWaiter?.CancelWait();
        }

        private void ResetUI()
        {
            _dialogueCanvas.enabled = false;

            _dialogueTextLeft.text = string.Empty;
            _dialogueTextRight.text = string.Empty;
            _dialogueTextDefault.text = string.Empty;

            _dialogueBackgroundImage.enabled = false;

            _avatarImageLeft.color = new Color(0f, 0f, 0f, 0f);
            _avatarImageRight.color = new Color(0f, 0f, 0f, 0f);

            _dialogueSelectionButtonsCanvas.enabled = false;

            foreach (var button in _selectionButtons)
            {
                button.GetComponentInChildren<Button>().onClick.RemoveAllListeners();
                Destroy(button);
            }
            _selectionButtons.Clear();

            _isAvatarPresented = false;
        }

        IEnumerator SkipDialogueRequested()
        {
            yield return new WaitUntil(() => _skipDialogueRequested);
            _skipDialogueRequested = false;
        }

        private string GetDisplayText(string text)
        {
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

        private void SkipDialoguePerformed(InputAction.CallbackContext obj)
        {
            if (_dialogueSelectionButtonsCanvas.enabled) return;
            _skipDialogueRequested = true;
        }

        public void Execute(DialogueRenderTextCommand command)
        {
            _skipDialogueWaiter?.CancelWait();
            _skipDialogueWaiter = new WaitUntilCanceled(this);
            StartCoroutine(RenderDialogueAndWait(GetDisplayText(command.DialogueText), false));
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerWaitRequest(_skipDialogueWaiter));
        }

        public void Execute(DialogueRenderTextWithTimeLimitCommand command)
        {
            _skipDialogueWaiter?.CancelWait();
            _skipDialogueWaiter = new WaitUntilCanceled(this);
            StartCoroutine(RenderDialogueAndWait(GetDisplayText(command.DialogueText), true));
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerWaitRequest(_skipDialogueWaiter));
        }

        public void Execute(DialogueRenderActorAvatarCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID) return;

            var actorName = _sceneManager.GetCurrentScene().GetActor((byte)command.ActorId).Info.Name;
            var avatarSprite = _resourceProvider.GetActorAvatarSprite(actorName, command.AvatarTextureName);

            _isRightAligned = command.RightAligned == 1;

            if (_isRightAligned)
            {
                _avatarImageRight.color = Color.white;
                _avatarImageRight.sprite = avatarSprite;
            }
            else
            {
                _avatarImageLeft.color = Color.white;
                _avatarImageLeft.sprite = avatarSprite;
            }

            _isAvatarPresented = true;
        }

        private string GetSelectionDisplayText(object selection)
        {
            var selectionString = (string)selection;
            if (selectionString.EndsWith("；") ||
                selectionString.EndsWith("。"))
            {
                return selectionString[2..^1];
            }
            else
            {
                return selectionString[2..];
            }
        }

        public void Execute(DialogueAddSelectionsCommand command)
        {
            _gameStateManager.GoToState(GameState.UI);

            var canvasTransform = _dialogueSelectionButtonsCanvas.transform;
            for (var i = 0; i < command.Selections.Count; i++)
            {
                var selectionButton = Instantiate(_dialogueSelectionButtonPrefab, canvasTransform);
                var buttonTextUI = selectionButton.GetComponentInChildren<TextMeshProUGUI>();
                buttonTextUI.text = GetSelectionDisplayText(command.Selections[i]);
                var buttonIndex = i;
                selectionButton.GetComponentInChildren<Button>().onClick
                    .AddListener(delegate { SelectionButtonClicked(buttonIndex);});
                _selectionButtons.Add(selectionButton);
            }

            // Setup button navigation
            for (var i = 0; i < command.Selections.Count; i++)
            {
                var button = _selectionButtons[i].GetComponentInChildren<Button>();
                var buttonNavigation = button.navigation;
                buttonNavigation.mode = Navigation.Mode.Explicit;

                if (i == 0)
                {
                    buttonNavigation.selectOnDown = _selectionButtons[i + 1].GetComponentInChildren<Button>();
                }
                else if (i == command.Selections.Count - 1)
                {
                    buttonNavigation.selectOnUp = _selectionButtons[i - 1].GetComponentInChildren<Button>();
                }
                else
                {
                    buttonNavigation.selectOnUp = _selectionButtons[i - 1].GetComponentInChildren<Button>();
                    buttonNavigation.selectOnDown = _selectionButtons[i + 1].GetComponentInChildren<Button>();
                }

                button.navigation = buttonNavigation;
            }

            var firstButton = _selectionButtons.First().GetComponentInChildren<Button>();
            _eventSystem.firstSelectedGameObject = firstButton.gameObject;

            var lastActiveInputDevice = _inputManager.GetLastActiveInputDevice();
            if (lastActiveInputDevice == Keyboard.current ||
                lastActiveInputDevice == Gamepad.current)
            {
                firstButton.Select();
            }

            _dialogueCanvas.enabled = true;
            _dialogueSelectionButtonsCanvas.enabled = true;

            _skipDialogueWaiter?.CancelWait();
            _skipDialogueWaiter = new WaitUntilCanceled(this);
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerWaitRequest(_skipDialogueWaiter));
        }

        private void SelectionButtonClicked(int index)
        {
            _lastSelectedButtonIndex = index;
            _skipDialogueWaiter?.CancelWait();
            ResetUI();
            _gameStateManager.GoToPreviousState();
        }
    }
}