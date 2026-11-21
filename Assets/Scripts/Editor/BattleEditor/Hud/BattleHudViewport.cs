// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using UnityEngine;
    using Battle;

    public readonly struct BattleHudViewport
    {
        public Rect Viewport
        {
            get;
        }

        public Rect SafeRect
        {
            get;
        }

        public float Scale
        {
            get;
        }

        public Vector2 CanvasSize => SafeRect.size / Scale;

        public Rect ReferenceRect => BattleHudLayout.ContentRect(SafeRect);

        // Insets are outer-layer GUI points: left, top, right, bottom.
        public BattleHudViewport(Rect viewport, Vector4 insets)
        {
            Viewport = viewport;
            float left = Mathf.Clamp(insets.x, 0, Math.Max(0, viewport.width - 1));
            float top = Mathf.Clamp(insets.y, 0, Math.Max(0, viewport.height - 1));
            float right = Mathf.Clamp(insets.z, 0, Math.Max(0, viewport.width - left - 1));
            float bottom = Mathf.Clamp(insets.w, 0, Math.Max(0, viewport.height - top - 1));
            SafeRect = new Rect(viewport.x + left, viewport.y + top, Math.Max(1, viewport.width - left - right), Math.Max(1, viewport.height - top - bottom));
            Scale = Math.Max(.001f, Math.Min(SafeRect.width / 800, SafeRect.height / 600));
        }

        public Vector2 ToCanvas(Vector2 point) => (point - SafeRect.position) / Scale;

        public Vector2 ToViewport(Vector2 point) => SafeRect.position + point * Scale;

        public Vector2 NativeToViewport(Vector2 point) => new Vector2(SafeRect.xMax, SafeRect.y) + (point - new Vector2(800, 0)) * Scale;

        public Vector2 ProjectionReferencePoint(Vector2 native, bool expandReference) =>
            ProjectionReferencePoint(native, expandReference ? Math.Min(Viewport.width / 800, Viewport.height / 600) : Viewport.width / 800);

        public Vector2 ProjectionReferencePoint(Vector2 native, float projectionScale) =>
            Viewport.center + (native - new Vector2(400, 300)) * projectionScale;

        public Vector2 SpiritOrbPoint(BattleSpiritOrbSnapshot orb, bool expandReference) =>
            SpiritOrbPoint(orb, expandReference ? Math.Min(Viewport.width / 800, Viewport.height / 600) : Viewport.width / 800);

        public Vector2 SpiritOrbPoint(BattleSpiritOrbSnapshot orb, float projectionScale) =>
            SpiritOrbPoint(new Vector2(orb.Position.X, orb.Position.Y), new Vector2(orb.Origin.X, orb.Origin.Y),
                new Vector2(orb.Destination.Center.X, orb.Destination.Center.Y), projectionScale);

        public Vector2 SpiritOrbPoint(Vector2 point, Vector2 origin, Vector2 destination, bool expandReference) =>
            SpiritOrbPoint(point, origin, destination, expandReference ? Math.Min(Viewport.width / 800, Viewport.height / 600) : Viewport.width / 800);

        public Vector2 SpiritOrbPoint(Vector2 point, Vector2 origin, Vector2 destination, float projectionScale)
        {
            Vector2 travel = destination - origin;
            float progress = travel.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(point - origin, travel) / travel.sqrMagnitude) : 1;
            Vector2 offset = ProjectionReferencePoint(origin, projectionScale) - NativeToViewport(origin);
            return NativeToViewport(point) + offset * (1 - progress);
        }

        public Rect StateCard(int localIndex)
        {
            float x = BattleHudLayout.StateCard(localIndex).x + localIndex * (CanvasSize.x - 800) / 3;
            return new Rect(x, CanvasSize.y - 109, 200, 106);
        }
    }
}
