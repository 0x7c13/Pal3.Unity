// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using Battle;
    using NUnit.Framework;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public sealed class BattleCameraProjectionTests
    {
        [TestCase(50, 0, 0, 0)]
        [TestCase(39, .7f, -.2f, .1f)]
        [TestCase(85, -1.2f, .3f, -.6f)]
        [TestCase(25, 2.1f, -.4f, .4f)]
        public void FixedReferenceProjectionMatchesUnityCameraWithoutOwningItsMutableState(float horizontal, float yaw, float pitch, float roll)
        {
            var pose = new BattleCameraPose(new BattleMovementPoint(100, 60, -200), yaw, pitch, roll);
            var projection = new BattleCameraProjection(pose, horizontal);
            var owner = new GameObject("Projection oracle") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                Camera camera = owner.AddComponent<Camera>();
                camera.enabled = false;
                camera.aspect = 4f / 3;
                camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(horizontal, camera.aspect);
                camera.transform.SetPositionAndRotation(BattleAnimationCatalog.UnityPosition(pose.Position),
                    Quaternion.LookRotation(new Vector3(-pose.Forward.X, pose.Forward.Y, pose.Forward.Z),
                        new Vector3(-pose.Up.X, pose.Up.Y, pose.Up.Z)));
                // Sample visible, offscreen and rear points at stable depths;
                // points on the camera plane have ill-conditioned pixel values.
                BattleMovementPoint Point(float depth, float height, float offset) => new(
                    pose.Position.X + pose.Forward.X * depth + pose.Up.X * height + offset,
                    pose.Position.Y + pose.Forward.Y * depth + pose.Up.Y * height,
                    pose.Position.Z + pose.Forward.Z * depth + pose.Up.Z * height);
                foreach (BattleMovementPoint world in new[] { Point(700, 80, 50), Point(700, -30, -800), Point(-500, 50, 50) })
                {
                    Vector3 point = camera.WorldToViewportPoint(BattleAnimationCatalog.UnityPosition(world));
                    BattleSpiritPoint actual = projection.Project(world);
                    Assert.That(actual.X, Is.EqualTo(point.x * 800).Within(.003));
                    Assert.That(actual.Y, Is.EqualTo((1 - point.y) * 600).Within(.003));
                }
                BattleMovementPoint sample = Point(700, 80, 50);
                BattleSpiritPoint before = projection.Project(sample);
                camera.transform.position = Vector3.one * 100;
                camera.fieldOfView = 80;
                camera.aspect = 21f / 9;
                BattleSpiritPoint after = projection.Project(sample);
                Assert.That((after.X, after.Y), Is.EqualTo((before.X, before.Y)));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }
    }
}
