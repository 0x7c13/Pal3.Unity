// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public enum BattleHudIntentKind
    {
        Navigate, Confirm, Back, Normal, Repeat, PointerDown, PointerMove, PointerUp, PointerCancel, CyclePrevious, CycleNext, Primary
    }

    public enum BattleHudDirection
    {
        Up, Down, Left, Right, UpRight, DownRight, DownLeft, UpLeft, Neutral
    }

    public readonly struct BattleHudIntent
    {
        public BattleHudIntentKind Kind
        {
            get;
        }

        public BattleHudDirection Direction
        {
            get;
        }

        public int PointerId
        {
            get;
        }

        public string ControlId
        {
            get;
        }

        public BattleHudIntent(BattleHudIntentKind kind, string controlId = null, int pointerId = 0, BattleHudDirection direction = BattleHudDirection.Down)
        {
            Kind = kind;
            ControlId = controlId;
            PointerId = pointerId;
            Direction = direction;
        }
    }

    public readonly struct BattleHudFocus
    {
        public string Id
        {
            get;
        }

        public string Label
        {
            get;
        }

        public bool Enabled
        {
            get;
        }

        public int Column
        {
            get;
        }

        public int Row
        {
            get;
        }

        public BattleHudFocus(string id, string label, bool enabled = true, int column = 0, int row = 0)
        {
            Id = id;
            Label = label;
            Enabled = enabled;
            Column = column;
            Row = row;
        }
    }

    public sealed partial class BattleHudPresenter
    {
        private string _focusId;
        private int? _pointerId;
        private string _pressedControl;
        private int? _pressedTargetGeneration;

        private bool PressedTargetReplaced => _pressedTargetGeneration.HasValue &&
            _session.MonsterGeneration(_pressedControl[7..]) != _pressedTargetGeneration.Value;

        public string FormationActor
        {
            get; private set;
        }

        public string AiActor
        {
            get; private set;
        }

        public string FocusedId
        {
            get
            {
                IReadOnlyList<BattleHudFocus> items = FocusItems();
                if (items.Any(item => item.Id == _focusId))
                    return _focusId;
                string first = items.FirstOrDefault(item => item.Enabled).Id;
                return first?.StartsWith("category:", StringComparison.Ordinal) == true ? "category:" + Category : first;
            }
        }

        public int? CapturedPointerId => _pointerId;

        // The control actually activated by the latest input. A cancelled or
        // stray release never produces activation feedback.
        public string ActivatedControl
        {
            get; private set;
        }

        public IReadOnlyList<BattleHudFocus> FocusItems()
        {
            if (_session == null || Controller == null)
                return Array.Empty<BattleHudFocus>();
            BattleActorSnapshot actor = _session.Actors.First(value => value.Id == Controller);
            if (Page == BattleHudPage.Commands)
            {
                BattleDefinitions definitions = _session.Definitions;
                // Offer the same entries as the lists below. PAL3 FillSkillList
                // (cbrolestate.cpp) skips ally skills that have not been mastered.
                var learned = actor.Skills.Where(skill => actor.Side == BattleSide.Enemy || skill.Mastered);
                bool seal = _session.HasActiveState(Controller, BattleState.Seal);
                bool forbidden = _session.HasActiveState(Controller, BattleState.Forbidden);
                var items = new List<BattleHudFocus>
                {
                    new("attack", "击", Reason(BattleAction.Attack) == null, 1, 1),
                    new("skills", "技", _session.FormActionDenial(Controller, BattleAction.Skill) == null && definitions != null && learned.Any(skill => definitions.Skills.TryGetValue(skill.Id, out var ability) && !IsMagicMenu(ability)), 1, 0),
                    new("magic", "术", !seal && _session.FormActionDenial(Controller, BattleAction.Skill, magic: true) == null && definitions != null && learned.Any(skill => definitions.Skills.TryGetValue(skill.Id, out var ability) && IsMagicMenu(ability)), 0, 1),
                    new("items", "物", definitions != null && actor.Side == BattleSide.Ally && !forbidden && _session.FormActionDenial(Controller, BattleAction.Item) == null, 1, 2),
                    new("defend", "防", Reason(BattleAction.Defend) == null, 2, 1),
                    new("formation", "阵", _session.CanBeginFormation(Controller) == null, 0, 2),
                    new("flee", "逃", Reason(BattleAction.Flee) == null, 2, 2),
                    new("ai", "AI", definitions != null && !_session.IsPal3ATransformed(Controller), 0, 0),
                    new("protection", "护", _session.CanSetProtection(Controller, null) == null, 2, 0)
                };
                if (actor.MagicSword is { SkillId: > 0 } sword && sword.Energy > 199.9f)
                    items.Add(new BattleHudFocus("sword", "魔剑技", Reason(BattleAction.Skill, sword.SkillId) == null, 0, 3));
                if (definitions?.Variant == "PAL3A" && actor.NativeIndex == 0)
                {
                    if (actor.ActorId == 101)
                        items.Add(new BattleHudFocus("transform", "炎兽变身", Reason(BattleAction.Transform) == null, 6, 0));
                    items.AddRange(Enumerable.Range(264, 5).Select(id => new BattleHudFocus("wheel:" + id, definitions.Skills.TryGetValue(id, out BattleAbilityDefinition ability) ? ability.Name : "五灵轮" + id, WheelReason(id) == null, id is 264 or 265 ? 5 : 6, id switch { 265 => 0, 264 or 266 => 1, 267 => 2, _ => 3 })));
                }
                return items;
            }
            if (Page is BattleHudPage.Skills or BattleHudPage.Magic or BattleHudPage.Items)
            {
                IEnumerable<BattleHudFocus> rows = Abilities().Select(row => new BattleHudFocus("ability:" + row.Definition.Id, row.Definition.Name, row.Denied == null));
                if (Page is BattleHudPage.Magic or BattleHudPage.Items)
                {
                    string[] categories = Page == BattleHudPage.Magic ? new[] { "水", "火", "雷", "风", "土", "双系" } : new[] { "恢复", "攻击", "仙宝" };
                    rows = rows.Concat(categories.Select((name, index) => new BattleHudFocus("category:" + index, name, CategoryEnabled(index))));
                }
                return rows.ToArray();
            }
            if (Page == BattleHudPage.Targets)
                return Targets.Select(target => new BattleHudFocus("target:" + target.Id, target.Id)).ToArray();
            if (Page == BattleHudPage.Formation)
            {
                var items = _session.Actors.Where(value => value.Side == BattleSide.Ally).Select(value =>
                    new BattleHudFocus("actor:" + value.Id, value.Id, _session.CanSwapFormation(value.Id, value.Slot) == null && FormationActor == null)).ToList();
                items.AddRange(Enum.GetValues(typeof(BattleSlot)).Cast<BattleSlot>().Select(slot =>
                    new BattleHudFocus("slot:" + (int)slot, slot switch
                    {
                        BattleSlot.Water => "水",
                        BattleSlot.Fire => "火",
                        BattleSlot.Wind => "风",
                        BattleSlot.Thunder => "雷",
                        BattleSlot.Earth => "土",
                        _ => "中"
                    }, FormationActor != null && _session.CanSwapFormation(FormationActor, slot) == null)));
                items.Add(new BattleHudFocus("end", "确认阵型"));
                return items;
            }
            if (Page == BattleHudPage.Protection)
                return _session.Actors.Where(value => value.Side == BattleSide.Ally && _session.CanSetProtection(Controller, value.Id) == null)
                    .Select(value => new BattleHudFocus("protect:" + value.Id, value.Id)).ToArray();
            if (Page == BattleHudPage.Ai)
                return _session.Actors.Select(value => new BattleHudFocus("ai-actor:" + value.Id, value.Id + (value.Id == Controller && value.Side == BattleSide.Ally ? "（操作者）" : "（工具／免费）")))
                    .Concat(Enumerable.Range(0, 6).Select(bit => new BattleHudFocus("flag:" + bit, new[] { "普攻", "仙术", "特技", "物品", "全力／万全", "自动" }[bit])))
                    .Append(new BattleHudFocus("end", "确认AI")).ToArray();
            return Array.Empty<BattleHudFocus>();
        }

        public bool SetFocus(string id)
        {
            if (!FocusItems().Any(item => item.Id == id && item.Enabled))
                return false;
            _focusId = id;
            if (Page == BattleHudPage.Targets && id.StartsWith("target:", StringComparison.Ordinal))
                HoverTarget(id[7..]);
            return true;
        }

        public bool Handle(BattleHudIntent input)
        {
            ActivatedControl = null;
            if (PressedTargetReplaced)
                CancelPointer();
            if (Controller == null || _session.WaitingActor != Controller)
                return false;
            if (input.Kind == BattleHudIntentKind.PointerCancel)
            {
                if (_pointerId != input.PointerId)
                    return false;
                CancelPointer();
                return true;
            }
            if (input.Kind == BattleHudIntentKind.PointerDown)
            {
                if (_pointerId.HasValue || !SetFocus(input.ControlId))
                    return false;
                _pointerId = input.PointerId;
                _pressedControl = input.ControlId;
                if (Page == BattleHudPage.Targets)
                    _pressedTargetGeneration = _session.MonsterGeneration(input.ControlId[7..]);
                if (Page == BattleHudPage.Formation && input.ControlId.StartsWith("actor:", StringComparison.Ordinal))
                    FormationActor = input.ControlId[6..];
                return true;
            }
            if (input.Kind is BattleHudIntentKind.PointerMove or BattleHudIntentKind.PointerUp)
            {
                if (_pointerId != input.PointerId)
                    return false;
                if (input.ControlId != null)
                    SetFocus(input.ControlId);
                if (input.Kind == BattleHudIntentKind.PointerMove)
                    return true;
                string pressed = _pressedControl;
                CancelPointer();
                if (Page == BattleHudPage.Formation && pressed?.StartsWith("actor:", StringComparison.Ordinal) == true)
                    return input.ControlId?.StartsWith("slot:", StringComparison.Ordinal) == true &&
                        int.TryParse(input.ControlId[5..], out int slot) && MoveFormation(pressed[6..], (BattleSlot)slot);
                return pressed == input.ControlId && ExecuteFocus(input.ControlId);
            }
            CancelPointer();
            switch (input.Kind)
            {
                case BattleHudIntentKind.Navigate:
                    return Navigate(input.Direction);
                case BattleHudIntentKind.Confirm:
                    return ExecuteFocus(input.ControlId ?? FocusedId);
                case BattleHudIntentKind.CyclePrevious:
                    return CycleSection(-1);
                case BattleHudIntentKind.CycleNext:
                    return CycleSection(1);
                case BattleHudIntentKind.Primary:
                    return Page == BattleHudPage.Protection ? SetProtection(null) && CommitProtection() : Page == BattleHudPage.Commands && SetFocus("attack");
                case BattleHudIntentKind.Back:
                    Back();
                    return true;
                case BattleHudIntentKind.Normal:
                    return RequestAi(false);
                case BattleHudIntentKind.Repeat:
                    return RequestAi(true);
                default:
                    return false;
            }
        }

        private readonly Dictionary<string, (float X, float Y)> _focusLayout = new(StringComparer.Ordinal);

        public void SetFocusLayout(IEnumerable<(string Id, float X, float Y)> positions)
        {
            _focusLayout.Clear();
            foreach (var position in positions)
                _focusLayout[position.Id] = (position.X, position.Y);
        }

        public static (int X, int Y) DirectionVector(BattleHudDirection direction) => direction switch
        {
            BattleHudDirection.Up => (0, -1),
            BattleHudDirection.Down => (0, 1),
            BattleHudDirection.Left => (-1, 0),
            BattleHudDirection.Right => (1, 0),
            BattleHudDirection.UpRight => (1, -1),
            BattleHudDirection.DownRight => (1, 1),
            BattleHudDirection.DownLeft => (-1, 1),
            BattleHudDirection.UpLeft => (-1, -1),
            _ => (0, 0)
        };

        private bool FocusNavigation(string id)
        {
            // Disabled list entries remain inspectable, but ExecuteFocus still
            // checks their enabled state before any command can be issued.
            if (!FocusItems().Any(item => item.Id == id))
                return false;
            _focusId = id;
            if (Page == BattleHudPage.Targets && id.StartsWith("target:", StringComparison.Ordinal))
                HoverTarget(id[7..]);
            InspectAbilityRow(id);
            return true;
        }

        private bool CycleSection(int step)
        {
            if (Page is BattleHudPage.Magic or BattleHudPage.Items)
            {
                int count = Page == BattleHudPage.Magic ? 6 : 3;
                int next = (Category + count + step) % count;
                if (!CategoryEnabled(next))
                    next = (next + count + step) % count;
                SetCategory(next);
                return true;
            }
            if (Page == BattleHudPage.Ai)
            {
                var actors = _session.Actors.ToArray();
                int index = Math.Max(0, Array.FindIndex(actors, actor => actor.Id == AiActor));
                AiActor = actors[(index + step + actors.Length) % actors.Length].Id;
                _focusId = "flag:0";
                return true;
            }
            var items = FocusItems().Where(item => item.Enabled).ToArray();
            if (items.Length == 0)
                return false;
            int at = Math.Max(0, Array.FindIndex(items, item => item.Id == FocusedId));
            return SetFocus(items[(at + step + items.Length) % items.Length].Id);
        }

        private bool Navigate(BattleHudDirection direction)
        {
            if (direction == BattleHudDirection.Neutral)
                return Page == BattleHudPage.Commands && FocusNavigation("attack");
            var (dx, dy) = DirectionVector(direction);
            if (Page is BattleHudPage.Magic or BattleHudPage.Items && dy == 0)
                return CycleSection(dx);
            var items = FocusItems().Where(item => Page is BattleHudPage.Skills or BattleHudPage.Magic or BattleHudPage.Items || item.Enabled).ToArray();
            if (Page is BattleHudPage.Magic or BattleHudPage.Items)
                items = items.Where(item => item.Id.StartsWith("ability:", StringComparison.Ordinal)).ToArray();
            if (Page == BattleHudPage.Ai)
                items = items.Where(item => !item.Id.StartsWith("ai-actor:", StringComparison.Ordinal)).ToArray();
            if (items.Length == 0)
                return false;
            if (Page == BattleHudPage.Commands)
            {
                var radial = FocusItems().FirstOrDefault(item => item.Column == 1 + dx && item.Row == 1 + dy);
                return radial.Id != null && FocusNavigation(radial.Id);
            }
            int index = Array.FindIndex(items, item => item.Id == FocusedId);
            if (index < 0)
                return FocusNavigation(items[dy < 0 || dy == 0 && dx < 0 ? items.Length - 1 : 0].Id);
            if (Page is BattleHudPage.Targets or BattleHudPage.Formation or BattleHudPage.Ai or BattleHudPage.Protection &&
                _focusLayout.TryGetValue(items[index].Id, out var origin))
            {
                var next = items.Where(item => item.Id != items[index].Id && _focusLayout.ContainsKey(item.Id))
                    .Select(item => (Item: item, X: _focusLayout[item.Id].X - origin.X, Y: _focusLayout[item.Id].Y - origin.Y))
                    .Where(item => item.X * dx + item.Y * dy > .001f)
                    .OrderBy(item => (Math.Abs(item.X * dy - item.Y * dx) * 2 + Math.Sqrt(item.X * item.X + item.Y * item.Y)))
                    .FirstOrDefault();
                if (next.Item.Id != null)
                    return FocusNavigation(next.Item.Id);
            }
            int step = dy < 0 || dy == 0 && dx < 0 ? -1 : 1;
            return FocusNavigation(items[(index + step + items.Length) % items.Length].Id);
        }

        private bool ExecuteFocus(string id)
        {
            if (!SetFocus(id))
                return false;
            ActivatedControl = id;
            Message = null;
            if (id.StartsWith("category:", StringComparison.Ordinal))
            {
                SetCategory(int.Parse(id[9..]));
                return true;
            }
            if (id.StartsWith("ability:", StringComparison.Ordinal))
                return ChooseAbility(int.Parse(id[8..]));
            if (id.StartsWith("target:", StringComparison.Ordinal))
                return SelectTarget(id[7..]);
            if (id.StartsWith("actor:", StringComparison.Ordinal))
            {
                FormationActor = id[6..];
                _focusId = FocusItems().FirstOrDefault(item => item.Id.StartsWith("slot:", StringComparison.Ordinal) && item.Enabled).Id;
                return true;
            }
            if (id.StartsWith("slot:", StringComparison.Ordinal))
                return FormationActor != null && MoveFormation(FormationActor, (BattleSlot)int.Parse(id[5..]));
            if (id.StartsWith("protect:", StringComparison.Ordinal))
                return SetProtection(id[8..]) && CommitProtection();
            if (id.StartsWith("wheel:", StringComparison.Ordinal))
                return ChooseWheel(int.Parse(id[6..]));
            if (id.StartsWith("ai-actor:", StringComparison.Ordinal))
            {
                AiActor = id[9..];
                _focusId = "flag:0";
                return true;
            }
            if (id.StartsWith("flag:", StringComparison.Ordinal))
            {
                return ToggleAiDraft(int.Parse(id[5..]));
            }
            switch (id)
            {
                case "attack":
                    return ChooseAction(BattleAction.Attack);
                case "skills":
                    Open(BattleHudPage.Skills);
                    return true;
                case "magic":
                    Open(BattleHudPage.Magic);
                    return true;
                case "items":
                    Open(BattleHudPage.Items);
                    return true;
                case "defend":
                    return ChooseAction(BattleAction.Defend);
                case "flee":
                    return ChooseAction(BattleAction.Flee);
                case "formation":
                    return BeginFormation();
                case "ai":
                    AiActor = Controller;
                    Open(BattleHudPage.Ai);
                    return true;
                case "protection":
                    Open(BattleHudPage.Protection);
                    return true;
                case "transform":
                    return ChooseAction(BattleAction.Transform);
                case "end" when Page == BattleHudPage.Ai:
                    return CommitAiDraft();
                case "end" when Page == BattleHudPage.Formation:
                    return CommitFormation();
                case "sword":
                    return ChooseAction(BattleAction.Skill, _session.MagicSwordAbilityId(Controller));
                default:
                    return false;
            }
        }

        private void ResetFocus()
        {
            _focusId = null;
            CancelPointer();
        }

        private void CancelPointer()
        {
            // PointerDown selects an actor only for this drag. Keyboard actor
            // selection has no pressed control and keeps its two-stage Back.
            if (_pressedControl?.StartsWith("actor:", StringComparison.Ordinal) == true)
            {
                FormationActor = null;
                _focusId = null;
            }
            _pointerId = null;
            _pressedControl = null;
            _pressedTargetGeneration = null;
        }

        private void ClearInput()
        {
            ResetFocus();
            FormationActor = AiActor = ProtectionSelection = null;
            _aiDraft.Clear();
        }
    }
}
