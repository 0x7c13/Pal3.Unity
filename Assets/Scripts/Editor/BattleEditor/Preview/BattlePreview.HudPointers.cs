// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using Battle;
    using Engine.Extensions;
    using UnityEngine;

    public sealed partial class BattlePreview
    {
        public bool TryHudPointerPose(BattleSession combat, string id, out Vector3 position, out float scale, double? presentationSeconds = null)
        {
            position = default;
            scale = 1;
            if (!_actors.TryGetValue(id, out ActorView actor) || !actor.Visible || actor.Object == null || actor.Model?.Meshes.Length is not > 0)
                return false;
            // gbMVActorDef.GetCollideOBB unions the current action's stored
            // mesh boxes; it does not use the sampled frame or weapon bounds.
            Bounds bounds = ActorSelectionBounds(actor);
            BattleActorSnapshot state = null;
            foreach (BattleActorSnapshot candidate in combat.Actors)
                if (candidate.Id == id)
                    state = candidate;
            if (state == null)
                return false;
            float wave = MathF.Sin((float)(presentationSeconds ?? combat.SimulationTick * BattleSession.StepSeconds) * 10 + state.NativeIndex);
            float nativeOffset = 10 + wave * 5 + 6;
            if (combat.Definitions.Variant == "PAL3" && state.ActorId == 707)
                nativeOffset -= 80;
            if (combat.Definitions.Variant == "PAL3A" && state.NativeIndex == 0 && combat.IsPal3ATransformed(id))
                nativeOffset += 16;
            position = actor.Object.transform.TransformPoint(bounds.center) + Vector3.up *
                (bounds.extents.y + nativeOffset / UnityPrimitivesConvertor.GameBoxUnitToUnityUnit);
            scale = 1 + wave / 5;
            return true;
        }
    }
}
