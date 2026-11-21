// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle;
    using Battle.Presentation;
    using Core.DataReader.Cvd;
    using Game.Rendering.Native;
    using Game.Rendering.Renderer;
    using ResourceBrowser;
    using UnityEngine;
    using UnityEditor;

    public sealed partial class BattleHudView
    {
        private ResourceModelPreview _pointerOwner;
        private readonly Dictionary<string, (GameObject Object, CvdModelRenderer Renderer)> _pointers = new(StringComparer.Ordinal);
        private CvdFile _pointerModel;

        public static IReadOnlyList<string> PointerActors(BattleSession combat, BattleHudPresenter presenter)
        {
            if (combat == null || combat.IsModalPresentation || combat.Status != BattleSessionStatus.Running || presenter.Controller == null)
                return Array.Empty<string>();
            string focus = presenter.FocusedId;
            string selectedActor = presenter.Page == BattleHudPage.Ai ? (focus?.StartsWith("ai-actor:", StringComparison.Ordinal) == true ? focus[9..] : presenter.AiActor) :
                presenter.Page == BattleHudPage.Protection ? (focus?.StartsWith("protect:", StringComparison.Ordinal) == true ? focus[8..] : presenter.ProtectionSelection) : presenter.Controller;
            IEnumerable<BattleActorSnapshot> selected = presenter.Page == BattleHudPage.Targets ? presenter.Affected :
                presenter.Page != BattleHudPage.Formation ? combat.Actors.Where(actor => actor.Id == selectedActor) : Array.Empty<BattleActorSnapshot>();
            return selected.Select(actor => actor.Id).ToArray();
        }

        private void ReleasePointers()
        {
            if (_pointerOwner?.Root != null)
                foreach (var pointer in _pointers.Values)
                    if (pointer.Object != null)
                        _pointerOwner.ReleaseCompositeObject(pointer.Object);
            _pointers.Clear();
            _pointerOwner = null;
            _pointerModel = null;
        }

        private static void SetPointerVisible(GameObject target, bool visible)
        {
            // CvdModelRenderer disposes on MonoBehaviour.OnDisable. Visibility
            // must keep the model alive for selection changes and paused redraws.
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = visible;
        }

        public void PreparePointers(BattleSession combat, BattleData data, BattlePreview preview, double? presentationSeconds = null)
        {
            Synchronize(combat);
            if (_pointerOwner != preview.View)
            {
                if (_pointerOwner?.Root != null)
                    foreach (var old in _pointers.Values)
                        if (old.Object != null)
                            _pointerOwner.ReleaseCompositeObject(old.Object);
                _pointers.Clear();
                _pointerOwner = preview.View;
                _pointerModel = null;
            }
            if (_pointerOwner.Root == null)
            {
                _pointers.Clear();
                return;
            }
            double seconds = presentationSeconds ?? EditorApplication.timeSinceStartup;
            bool previewTargets = WorldSelectionOpen && (_navigationInput ||
                _interaction.Touch && (_touchTargetArmed != null || _touchWorldPreview != null) || !_interaction.Touch && _pointerTarget != null);
            string[] selected = WorldSelectionOpen && !previewTargets ? Array.Empty<string>() : PointerActors(combat, _presenter).ToArray();
            preview.SetSelection(previewTargets ? selected : Array.Empty<string>(), seconds);
            foreach (var pointer in _pointers.Values)
                if (pointer.Object != null)
                    SetPointerVisible(pointer.Object, false);
            foreach (string id in selected)
            {
                if (!preview.TryHudPointerPose(combat, id, out Vector3 position, out float scale, seconds))
                    continue;
                if (!_pointers.TryGetValue(id, out var pointer))
                {
                    const string path = "basedata.cpk\\ui\\combat\\pointer\\pointer.cvd";
                    _pointerModel ??= data.Read(path, new CvdFileReader());
                    GameObject target = _pointerOwner.CreateObject("Battle selection pointer " + id);
                    try
                    {
                        pointer = (target, _pointerOwner.SetHierarchicalModel(target, path, _pointerModel));
                        _pointers.Add(id, pointer);
                    }
                    catch
                    {
                        _pointerOwner.ReleaseCompositeObject(target);
                        throw;
                    }
                    BattleActorSnapshot actor = combat.Actors.First(actor => actor.Id == id);
                    if (!_pointerOwner.UsesToonRendering)
                        GameBoxBattleRenderPhase.Apply(target, GameBoxBattleRenderOrder.Actor(combat.Variant == "PAL3A",
                        actor.Side == BattleSide.Ally, actor.ActorId));
                }
                SetPointerVisible(pointer.Object, true);
                pointer.Object.transform.SetPositionAndRotation(position, Quaternion.identity);
                pointer.Object.transform.localScale = new Vector3(1, scale, 1);
                float duration = _pointerModel.AnimationDuration;
                pointer.Renderer.SetCurrentTime(duration > 0 ? (float)(seconds % duration) : 0);
            }
        }
    }
}
