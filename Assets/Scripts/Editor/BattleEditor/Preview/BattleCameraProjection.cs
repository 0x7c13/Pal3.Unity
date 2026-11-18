// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using Battle;
    using UnityEngine;

    // Immutable projection of a published gameplay camera. Editor navigation,
    // framing and viewport dimensions have no ownership of capture physics.
    public readonly struct BattleCameraProjection
    {
        private readonly Matrix4x4 _worldToClip;

        public BattleCameraProjection(BattleCameraPose pose, float horizontalFieldOfView)
        {
            if (!float.IsFinite(horizontalFieldOfView) || horizontalFieldOfView <= 0 || horizontalFieldOfView >= 180)
                throw new ArgumentOutOfRangeException(nameof(horizontalFieldOfView));
            const float aspect = 4f / 3;
            Vector3 Direction(BattleMovementPoint value) => new(-value.X, value.Y, value.Z);
            Quaternion orientation = Quaternion.LookRotation(Direction(pose.Forward), Direction(pose.Up));
            Matrix4x4 view = Matrix4x4.Scale(new Vector3(1, 1, -1)) *
                Matrix4x4.TRS(BattleAnimationCatalog.UnityPosition(pose.Position), orientation, Vector3.one).inverse;
            Matrix4x4 projection = Matrix4x4.Perspective(Camera.HorizontalToVerticalFieldOfView(horizontalFieldOfView, aspect), aspect, .01f, 10000);
            _worldToClip = projection * view;
        }

        public BattleSpiritPoint Project(BattleMovementPoint world)
        {
            Vector3 point = _worldToClip.MultiplyPoint(BattleAnimationCatalog.UnityPosition(world));
            return new BattleSpiritPoint((point.x + 1) * 400, (1 - point.y) * 300);
        }
    }
}
