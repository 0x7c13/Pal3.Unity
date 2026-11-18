// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Battle;
    using Battle.Presentation;
    using UnityEditor;
    using UnityEngine;

    public sealed partial class BattleHudView
    {
        private readonly BattleHudPresenter _presenter = new();
        private BattleSession _battle;
        private BattleData _data;
        private BattlePreview _preview;
        private BattleHudLayout _layout;
        private readonly Dictionary<string, Texture2D> _spriteTextures = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _sequenceSprites = new(StringComparer.OrdinalIgnoreCase);
        private Action _changed;
        private Action<string> _selectActor;
        private Vector2 _listScroll;
        private bool _inputEnabled;
        private Vector2 _pointer;
        private BattleHudViewport _viewport;
        private Vector2 _pageOffset;
        private float _pageScale = 1;
        private System.Collections.Generic.IReadOnlyList<BattleHudFocus> _renderFocus = Array.Empty<BattleHudFocus>();
        private string _renderFocusedId;
        private bool _hasInputFocus = true;
        private BattleHudPage _layoutPage;
        private int _layoutCategory;
        private string _scrollFocus;

        public BattleHudPresenter Presenter => _presenter;

        public void Release()
        {
            CancelInput();
            ReleasePointers();
            ClearHitControls();
            _spritePixels.Clear();
            _spriteTextures.Clear();
            _sequenceSprites.Clear();
            _presenter.Synchronize(null);
            _presenter.SetFocusLayout(Array.Empty<(string, float, float)>());
            _battle = null;
            _data = null;
            _preview = null;
            _selectionOwner = null;
            _gamepadPointingContext = null;
            _changed = _repaint = null;
            _selectActor = null;
            _renderFocus = Array.Empty<BattleHudFocus>();
            _renderFocusedId = _scrollFocus = null;
            _gamepadPointingDirection = null;
            _inputEnabled = _navigationInput = false;
            _listScroll = Vector2.zero;
        }

        public Vector2 GuiScreenOrigin
        {
            get; private set;
        }

        public Rect LastViewport
        {
            get; private set;
        }

        private static GUIStyle Small => BattleHudCanvas.LabelStyle(11, Color.white);

        public void BindBattle(BattleSession combat, BattleData data, BattlePreview preview, Action changed, Action<string> select, Action repaint = null)
        {
            if (_battle != combat || _data != data || _preview != preview)
            {
                ClearHitControls();
                _spritePixels.Clear();
                _spriteTextures.Clear();
                _sequenceSprites.Clear();
                _layout = new BattleHudLayout(combat.Variant);
            }
            _battle = combat;
            _data = data;
            _preview = preview;
            _changed = changed;
            _selectActor = select;
            _repaint = repaint ?? changed;
            Synchronize(combat);
        }

        public void Draw(BattleSession combat, BattleData data, BattlePreview preview, Rect content, Action changed, Action<string> select, Vector4 safeInsets = default, Action repaint = null, BattleHudInteractionProfile interaction = default)
        {
            if (content.width < 16 || content.height < 16)
                return;
            PrepareGuiInput();
            GuiScreenOrigin = GUIUtility.GUIToScreenPoint(Vector2.zero);
            LastViewport = content;
            _viewport = new BattleHudViewport(content, safeInsets);
            if (_interaction.Touch != interaction.Touch)
            {
                CancelInput();
                ClearHitControls();
            }
            _interaction = interaction;
            BindBattle(combat, data, preview, changed, select, repaint);
            bool inputEnabled = combat.Status == BattleSessionStatus.Running && !combat.IsModalPresentation && GUI.enabled && _hasInputFocus;
            if (_inputEnabled && !inputEnabled)
                CancelInput();
            _inputEnabled = inputEnabled;
            if (combat.IsModalPresentation || combat.Status != BattleSessionStatus.Running)
            {
                HandleGuiInput(Event.current, content);
                ClearHitControls();
                return;
            }
            _lastPointer ??= Event.current.mousePosition;
            if (Event.current.type == EventType.ScrollWheel && content.Contains(Event.current.mousePosition) && _navigationInput)
            {
                PointerTookControl?.Invoke();
                _navigationInput = false;
            }
            HandleGuiInput(Event.current, content);
            if (Event.current.type == EventType.Repaint)
                ClearHitControls();
            if (_layoutPage != _presenter.Page || _layoutCategory != _presenter.Category)
            {
                if (_layoutPage != _presenter.Page)
                {
                    _touchTargetArmed = null;
                    _touchTargetConfirm = false;
                    _pointerTarget = null;
                }
                _layoutPage = _presenter.Page;
                _layoutCategory = _presenter.Category;
                _listScroll = Vector2.zero;
                _touchPanel = default;
                _touchScrollLimit = 0;
                _scrollFocus = null;
                ResetTouchGesture();
            }
            _renderFocus = _presenter.FocusItems();
            _renderFocusedId = _presenter.FocusedId;
            _pointer = _navigationInput ? new Vector2(float.NegativeInfinity, float.NegativeInfinity) : _viewport.ToCanvas(Event.current.mousePosition);
            using (new BattleHudCanvas(_viewport))
            {
                DrawWheelControls();
                DrawSceneField();
                IReadOnlyList<BattleActorSnapshot> actors = combat.Actors;
                DrawStateCards(actors);
                DrawSequence(actors);
            }
            if (_presenter.Controller != null && _presenter.Page != BattleHudPage.Targets)
                DrawCommandPage();
            using (new BattleHudCanvas(_viewport))
            {
                if (Event.current.type == EventType.Repaint)
                    PublishFocusLayout();
                if (_presenter.Message != null)
                    GUI.Label(new Rect(210, _viewport.CanvasSize.y - 148, Math.Max(100, _viewport.CanvasSize.x - 220), 36), BattleHudDenialText.Format(_presenter.Message, detailed: true), BattleHudCanvas.LabelStyle(13, Color.white, wrap: true));
            }
        }

        private void DrawCommandPage()
        {
            _pageScale = _interaction.ScaleFor(_viewport);
            _pageOffset = CommandPageOrigin(_pageScale);
            _pointer = (_pointer - _pageOffset) / _pageScale;
            float scale = _viewport.Scale * _pageScale;
            var area = new Rect(_viewport.ToViewport(_pageOffset), new Vector2(_viewport.SafeRect.width, 600 * scale));
            try
            {
                using (new BattleHudCanvas(area, scale))
                {
                    switch (_presenter.Page)
                    {
                        case BattleHudPage.Commands:
                            DrawCommands();
                            break;
                        case BattleHudPage.Skills:
                        case BattleHudPage.Magic:
                        case BattleHudPage.Items:
                            DrawAbilities();
                            break;
                        case BattleHudPage.Formation:
                            DrawFormation();
                            break;
                        case BattleHudPage.Protection:
                            // Protection is selected directly on scene actors.
                            break;
                        case BattleHudPage.Ai:
                            DrawAi();
                            break;
                    }
                }
            }
            finally
            {
                _pointer = _pointer * _pageScale + _pageOffset;
                _pageOffset = Vector2.zero;
                _pageScale = 1;
            }
        }

        private BattleSession _selectionOwner;
        private (BattleSession Battle, string Controller, BattleHudPage Page)? _gamepadPointingContext;

        private void Synchronize(BattleSession combat)
        {
            string previousController = _presenter.Controller;
            BattleHudPage previousPage = _presenter.Page;
            string previousTarget = _presenter.Target;
            int? previousPointer = _presenter.CapturedPointerId;
            _presenter.Synchronize(combat);
            bool controllerChanged = _selectionOwner != combat || previousController != _presenter.Controller;
            if (controllerChanged)
            {
                _selectionOwner = combat;
                if (_presenter.Controller != null)
                    PlayMenuSound("wa003");
                _listScroll = Vector2.zero;
            }
            if (controllerChanged || previousPage != _presenter.Page || previousTarget != _presenter.Target ||
                previousPointer != _presenter.CapturedPointerId)
            {
                if (previousPointer is int pointer)
                    _retiredPointers.Add(pointer);
                CancelInput();
                _pointerTarget = null;
            }
            var pointingContext = (combat, _presenter.Controller, _presenter.Page);
            if (_gamepadPointingContext != pointingContext)
            {
                _gamepadPointingContext = pointingContext;
                ApplyGamepadPointing();
            }
        }

        // The native UI uses width/textureWidth UVs, cropping padded textures.
        // Stretching the complete power-of-two image shrinks portraits and bars.
        private Texture2D SpriteTexture(string name)
        {
            if (!_spriteTextures.TryGetValue(name, out Texture2D texture))
            {
                texture = _preview.View.GetTexture("basedata.cpk\\ui\\combat\\" + name.Replace('/', '\\') + ".tga");
                _spriteTextures.Add(name, texture);
            }
            return texture;
        }

        private void Sprite(string name, Rect rect)
        {
            Texture2D texture = SpriteTexture(name);
            if (texture == null)
                return;
            rect.width = Math.Min(rect.width, texture.width);
            rect.height = Math.Min(rect.height, texture.height);
            GUI.DrawTextureWithTexCoords(rect, texture, BattleHudCanvas.SpriteUv(texture, rect.size), true);
        }

        // Controls only draw and publish their visible hit rectangles. The
        // input dispatcher is the sole path into the shared presenter.
        private void SpriteButton(string name, Rect rect, string tooltip = null, bool selected = false, bool disabledImage = true)
        {
            bool hover = rect.Contains(_pointer);
            string control = ControlId(name);
            bool enabled = control == null || _renderFocus.Any(item => item.Id == control && item.Enabled);
            RegisterControl(control, rect, SpriteTexture(name + "0"));
            int frame = !enabled && disabledImage ? 3 : selected || hover || control == _renderFocusedId ? 1 : 0;
            Color before = GUI.color;
            if (!enabled && !disabledImage)
                GUI.color = new Color(1, 1, 1, .4f);
            if (_navigationInput && control == _renderFocusedId)
                GUI.color *= new Color(1, 1, 1, .7f + .3f * Mathf.Sin((float)UnityEditor.EditorApplication.timeSinceStartup * 7.8f));
            Sprite(name + frame, rect);
            GUI.color = before;
            GUI.Label(rect, new GUIContent("", tooltip));
        }

        private void TextButton(Rect rect, string text, string controlId)
        {
            bool enabled = _renderFocus.Any(item => item.Id == controlId && item.Enabled);
            RegisterControl(controlId, rect);
            Color previous = GUI.color;
            if (!enabled || !_inputEnabled)
                GUI.color = new Color(1, 1, 1, .4f);
            GUIStyle style = new GUIStyle(EditorStyles.miniButton);
            if (_navigationInput && controlId == _renderFocusedId)
                style.normal = style.onNormal;
            GUI.Label(rect, text, style);
            GUI.color = previous;
        }

        public string NavigationFocusedControl => _navigationInput ? _presenter.FocusedId : null;

        private string ActorName(BattleActorSnapshot actor) => _data.Database.CombatActorInfos.TryGetValue(actor.ActorId, out var info) ? info.Name : actor.ActorId.ToString();
    }
}
