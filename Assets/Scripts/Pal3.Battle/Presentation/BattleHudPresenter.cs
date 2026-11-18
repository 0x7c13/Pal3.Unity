// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle;

    public enum BattleHudPage
    {
        Commands, Skills, Magic, Items, Targets, Formation, Protection, Ai
    }

    public sealed class BattleHudAbility
    {
        public BattleAbilityDefinition Definition
        {
            get; internal set;
        }

        public string Denied
        {
            get; internal set;
        }

        public int Count
        {
            get; internal set;
        }

        public int Uses
        {
            get; internal set;
        }

        public int Level
        {
            get; internal set;
        }
    }

    // Selection is local to the view. Reading this presenter never submits a
    // command, advances time, pays a resource, or chooses a random recipient.
    public sealed partial class BattleHudPresenter
    {
        private readonly Dictionary<string, int> _magicCategories = new(StringComparer.Ordinal);
        private BattleSession _session;
        private BattleHudPage _returnPage;
        private BattleAction _action;
        private int _ability;

        public string Controller
        {
            get; private set;
        }

        public BattleHudPage Page
        {
            get; private set;
        }

        public int Category
        {
            get; private set;
        }

        public string Target
        {
            get; private set;
        }

        public string ProtectionSelection
        {
            get; private set;
        }

        public string Message
        {
            get; private set;
        }

        public BattleCommand Command => new(Controller, _action, Target, _ability);

        public BattleTargetRange Range => _session?.GetActionTargetRange(Command) ?? BattleTargetRange.None;

        public IReadOnlyList<BattleActorSnapshot> Targets => _session?.GetTargetCandidates(Command) ?? Array.Empty<BattleActorSnapshot>();

        public IReadOnlyList<BattleActorSnapshot> Affected => _session?.GetPotentialAffectedTargets(Command) ?? Array.Empty<BattleActorSnapshot>();

        public IReadOnlyList<BattleCostEstimate> Costs => _session?.GetCostEstimate(Command) ?? Array.Empty<BattleCostEstimate>();

        public void Synchronize(BattleSession session)
        {
            string waiting = session?.Status == BattleSessionStatus.Running ? session.WaitingActor : null;
            if (_session != session || Controller != waiting)
            {
                if (_session != session)
                    _magicCategories.Clear();
                _session = session;
                Controller = waiting;
                Reset();
            }
            if (session?.ActiveFormationController != null)
            {
                if (Page != BattleHudPage.Formation)
                {
                    Reset();
                    Page = BattleHudPage.Formation;
                }
            }
            else if (Page == BattleHudPage.Formation)
                Reset();
            if (AbilityMenuBlocked())
                Reset();
            if (Page == BattleHudPage.Targets)
            {
                if (PressedTargetReplaced)
                    CancelPointer();
                IReadOnlyList<BattleActorSnapshot> targets = Targets;
                if (targets.Count == 0)
                    Back();
                else if (!targets.Any(actor => actor.Id == Target))
                {
                    FocusDefaultTarget();
                }
            }
        }

        private bool AbilityMenuBlocked()
        {
            BattleState state;
            if (Page == BattleHudPage.Items || Page == BattleHudPage.Targets && _action == BattleAction.Item)
                state = BattleState.Forbidden;
            else if (Page == BattleHudPage.Magic || Page == BattleHudPage.Targets && _action == BattleAction.Skill &&
                _session?.Definitions?.Skills.TryGetValue(_ability, out var ability) == true && IsMagicMenu(ability))
                state = BattleState.Seal;
            else
                return false;
            return _session?.HasActiveState(Controller, state) == true;
        }

        private int _blockedMagicCategory = -1;

        public void Reset()
        {
            _blockedMagicCategory = -1;
            Page = BattleHudPage.Commands;
            Category = 0;
            Target = null;
            Message = null;
            _action = BattleAction.Attack;
            _ability = 0;
            ClearInput();
        }

        public void Open(BattleHudPage page)
        {
            if (Controller == null)
                return;
            if (page == BattleHudPage.Protection)
                ProtectionSelection = _session.ProtectionTarget;
            if (page == BattleHudPage.Ai)
                BeginAiDraft();
            else if (Page == BattleHudPage.Ai)
                _aiDraft.Clear();
            Page = page;
            Category = page == BattleHudPage.Magic ? _magicCategories.GetValueOrDefault(Controller) : 0;
            Target = null;
            Message = null;
            ResetFocus();
            if (page == BattleHudPage.Ai)
                _focusId = "flag:0";
            RefreshMagicCategoryGate();
        }

        public void SetCategory(int category)
        {
            if (!CategoryEnabled(category))
                return;
            Category = Math.Max(0, category);
            RememberMagicCategory();
            Message = null;
            ResetFocus();
        }

        private bool CategoryEnabled(int category) => Page != BattleHudPage.Magic || category != _blockedMagicCategory;

        private bool IsPal3AMagicPage => Page == BattleHudPage.Magic && _session?.Definitions?.Variant == "PAL3A";

        private void RememberMagicCategory()
        {
            if (Page == BattleHudPage.Magic && Controller != null)
                _magicCategories[Controller] = Category;
        }

        // PAL3A configures tab availability when entering the magic menu
        // (0x4D09AA–0x4D0A5E). Individual spells use the live field when checked.
        private void RefreshMagicCategoryGate()
        {
            _blockedMagicCategory = -1;
            if (IsPal3AMagicPage && _session.FiveSpiritForce == 2 && _session.FiveSpiritElement != 0 &&
                _session.Actors.Any(actor => actor.Id == Controller && actor.Side == BattleSide.Ally))
            {
                _blockedMagicCategory = BattleMagicSlots.Group(1 << (_session.FiveSpiritElement - 1));
                if (Category == _blockedMagicCategory)
                    Category = (_blockedMagicCategory + 1) % 5;
            }
            RememberMagicCategory();
        }

        public bool InspectAbilityRow(string id) => IsPal3AMagicPage && Controller != null && _session.FiveSpiritElement != 0 &&
            id?.StartsWith("ability:", StringComparison.Ordinal) == true && int.TryParse(id[8..], out int abilityId) &&
            AbilityDefinitions(_session.Actors.First(actor => actor.Id == Controller), Array.Empty<BattleItemSnapshot>())
                .Any(ability => ability.Id == abilityId) &&
            _session.InspectMagicMenuAbility(Controller, abilityId);

        public IReadOnlyList<BattleHudAbility> Abilities()
        {
            if (_session?.Definitions == null || Controller == null)
                return Array.Empty<BattleHudAbility>();
            BattleActorSnapshot actor = _session.Actors.First(value => value.Id == Controller);
            bool item = Page == BattleHudPage.Items;
            IReadOnlyList<BattleItemSnapshot> inventory = item ? _session.InventoryEntries : Array.Empty<BattleItemSnapshot>();
            var counts = item ? inventory.ToDictionary(entry => entry.Id, entry => entry.Count) : null;
            return AbilityDefinitions(actor, inventory).Select(ability => new BattleHudAbility
            {
                Definition = ability,
                Denied = Reason(item ? BattleAction.Item : BattleAction.Skill, ability.Id),
                Count = item ? counts[ability.Id] : 0,
                Uses = actor.Skills.FirstOrDefault(skill => skill.Id == ability.Id)?.Uses ?? 0,
                Level = actor.Skills.FirstOrDefault(skill => skill.Id == ability.Id)?.Level ?? 0
            }).ToArray();
        }

        private IEnumerable<BattleAbilityDefinition> AbilityDefinitions(BattleActorSnapshot actor, IReadOnlyList<BattleItemSnapshot> inventory)
        {
            bool item = Page == BattleHudPage.Items;
            IEnumerable<int> ids = item ? inventory.Where(entry => entry.Count > 0).Select(entry => entry.Id) :
                actor.Skills.Where(skill => actor.Side == BattleSide.Enemy || skill.Mastered).Select(skill => skill.Id);
            if (!item && Page == BattleHudPage.Magic && actor.Side == BattleSide.Ally)
                ids = BattleMagicSlots.Capture(_session.Definitions.Variant, _session.Definitions,
                    _session.Definitions.Actors[Controller].Identity, actor.Skills)
                    .Where(slot => slot.Group == Category && actor.Skills.Any(skill => skill.Id == slot.SkillId && skill.Mastered))
                    .Select(slot => slot.SkillId);
            IReadOnlyDictionary<int, BattleAbilityDefinition> table = item ? _session.Definitions.Items : _session.Definitions.Skills;
            return ids.Distinct().Where(table.ContainsKey).Select(id => table[id]).Where(ability =>
                item ? ability.ItemType == Category + 1 : Page == BattleHudPage.Skills ? !IsMagicMenu(ability) : IsMagicMenu(ability) &&
                    (actor.Side == BattleSide.Ally || MagicCategory(ability.ElementMask) == Category));
        }

        public static bool IsMagicMenu(BattleAbilityDefinition ability) => ability.SkillTypeId is 1 or 2 or 3 ||
            ability.SkillTypeId == -1 && ability.IsMagic;

        public static int MagicCategory(int mask) => Math.Min(5, BattleMagicSlots.Group(mask));

        public string Reason(BattleAction action, int ability = 0)
        {
            if (_session == null || Controller == null)
                return "等待角色就绪";
            var query = new BattleCommand(Controller, action, abilityId: ability);
            string target = _session.GetTargetCandidates(query).FirstOrDefault()?.Id;
            return _session.CanSubmit(new BattleCommand(Controller, action, target, ability));
        }

        public bool ChooseAbility(int ability)
        {
            if (Page is not (BattleHudPage.Skills or BattleHudPage.Magic or BattleHudPage.Items))
                return false;
            return ChooseAction(Page == BattleHudPage.Items ? BattleAction.Item : BattleAction.Skill, ability);
        }

        public bool ChooseAction(BattleAction action, int ability = 0)
        {
            Message = Reason(action, ability);
            if (Message != null)
                return false;
            _returnPage = Page;
            _action = action;
            _ability = ability;
            Target = null;
            if (action is BattleAction.Defend or BattleAction.Flee or BattleAction.Transform ||
                action == BattleAction.Skill && ability != 0 && ability == _session.MagicSwordAbilityId(Controller))
                return Submit();
            Page = BattleHudPage.Targets;
            FocusDefaultTarget();
            return true;
        }

        private void FocusDefaultTarget()
        {
            ResetFocus();
            Target = _session.GetDefaultTarget(Command);
            _focusId = Target == null ? null : "target:" + Target;
        }

        public bool HoverTarget(string target)
        {
            if (Page != BattleHudPage.Targets || !Targets.Any(actor => actor.Id == target))
                return false;
            Target = target;
            _focusId = "target:" + target;
            return true;
        }

        public bool SelectTarget(string target)
        {
            if (!HoverTarget(target))
                return false;
            return Submit();
        }

        public bool Submit()
        {
            if (_session == null || Controller == null || _session.WaitingActor != Controller)
                return false;
            if (Page == BattleHudPage.Targets && Target == null)
                return false;
            BattleTargetRange range = Range;
            BattleCommand command = (_action is BattleAction.Skill or BattleAction.Item) && (range is BattleTargetRange.OwnAll or BattleTargetRange.OpponentAll) ?
                new BattleCommand(Controller, _action, abilityId: _ability) : Command;
            Message = _session.CanSubmit(command);
            if (Message != null)
                return false;
            _session.Submit(command);
            Synchronize(_session);
            return true;
        }

        public bool BeginFormation()
        {
            if (Controller == null || !_session.BeginFormation(Controller))
                return false;
            Synchronize(_session);
            return true;
        }

        public bool CommitFormation()
        {
            if (Page != BattleHudPage.Formation || Controller == null || _session.WaitingActor != Controller)
                return false;
            bool ended = _session.EndFormation(Controller);
            Reset();
            Synchronize(_session);
            return ended;
        }

        public bool MoveFormation(string actor, BattleSlot slot)
        {
            Message = _session?.CanSwapFormation(actor, slot);
            bool moved = Message == null && _session?.SwapFormation(actor, slot) == true;
            if (moved)
            {
                FormationActor = null;
                ResetFocus();
            }
            return moved;
        }

        public bool SetProtection(string target)
        {
            if (Page != BattleHudPage.Protection || Controller == null)
                return false;
            Message = _session.CanSetProtection(Controller, target);
            if (Message != null)
                return false;
            ProtectionSelection = ProtectionSelection == target ? null : target;
            return true;
        }

        public bool CommitProtection()
        {
            if (Controller == null || Page != BattleHudPage.Protection || _session.WaitingActor != Controller)
                return false;
            Message = _session.CanSetProtection(Controller, ProtectionSelection);
            if (Message != null)
                return false;
            if (_session.ProtectionTarget != ProtectionSelection && !_session.SetProtection(Controller, ProtectionSelection))
                return false;
            Page = BattleHudPage.Commands;
            ResetFocus();
            Synchronize(_session);
            return true;
        }

        public string WheelReason(int skillId) => Controller == null ? "等待角色就绪" : _session.CanSubmitElementWheel(Controller, skillId);

        public bool ChooseWheel(int skillId)
        {
            Message = WheelReason(skillId);
            if (Message != null || !_session.SubmitElementWheel(Controller, skillId))
                return false;
            Synchronize(_session);
            return true;
        }

        public bool RequestAi(bool repeat)
        {
            if (Controller == null || Page == BattleHudPage.Formation)
                return false;
            bool submitted = repeat ? _session.RequestAiRepeat(Controller) : _session.RequestAiNormal(Controller);
            Synchronize(_session);
            return submitted;
        }

        public bool Back()
        {
            if (Page == BattleHudPage.Formation && FormationActor != null)
            {
                FormationActor = null;
                ResetFocus();
                return false;
            }
            _aiDraft.Clear();
            if (Page == BattleHudPage.Formation)
                return CommitFormation();
            Page = Page == BattleHudPage.Targets ? _returnPage : BattleHudPage.Commands;
            Target = null;
            Message = null;
            ResetFocus();
            if (Page is BattleHudPage.Magic or BattleHudPage.Skills or BattleHudPage.Items)
                _focusId = "ability:" + _ability;
            RefreshMagicCategoryGate();
            return false;
        }
    }
}
