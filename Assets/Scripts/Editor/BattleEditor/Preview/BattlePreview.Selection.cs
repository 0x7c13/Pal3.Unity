// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Engine.Extensions;
    using UnityEngine;

    public sealed partial class BattlePreview
    {
        private static Bounds ActorSelectionBounds(ActorView actor)
        {
            var bounds = new Bounds(actor.Model.Meshes[0].GameBoxBoundsMin.ToUnityPosition(), Vector3.zero);
            foreach (var mesh in actor.Model.Meshes)
            {
                bounds.Encapsulate(mesh.GameBoxBoundsMin.ToUnityPosition());
                bounds.Encapsulate(mesh.GameBoxBoundsMax.ToUnityPosition());
            }
            return bounds;
        }

        public bool TryActorSelectionPoint(string id, Rect viewport, out Vector2 point)
        {
            point = default;
            return _actors.TryGetValue(id, out var actor) && actor.Visible && actor.Model?.Meshes.Length > 0 &&
                View.Project(actor.Object.transform.TransformPoint(ActorSelectionBounds(actor).center), viewport, out point);
        }

        public string PickActor(Vector2 point, Rect viewport, IEnumerable<string> candidates)
        {
            if (!viewport.Contains(point))
                return null;
            Ray ray = View.ScreenRay(point, viewport);
            float nearest = float.PositiveInfinity;
            string picked = null;
            foreach (string id in candidates)
            {
                if (!_actors.TryGetValue(id, out var actor) || !actor.Visible || actor.Model?.Meshes.Length is not > 0)
                    continue;
                Transform transform = actor.Object.transform;
                var localRay = new Ray(transform.InverseTransformPoint(ray.origin), transform.InverseTransformVector(ray.direction));
                if (!ActorSelectionBounds(actor).IntersectRay(localRay, out float distance))
                    continue;
                float depth = Vector3.Distance(ray.origin, transform.TransformPoint(localRay.GetPoint(distance)));
                if (depth < nearest)
                {
                    nearest = depth;
                    picked = id;
                }
            }
            return picked;
        }

        public void SetSelection(IReadOnlyCollection<string> actors, double presentationSeconds)
        {
            float luminance = .65f + .35f * MathF.Sin((float)presentationSeconds * 7.8f);
            foreach (var actor in _actors)
                actor.Value.Renderer?.SetSelectionLuminance(actors.Contains(actor.Key) ? luminance : -1);
        }
    }
}
