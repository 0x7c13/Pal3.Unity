// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.PlayMode.Tests
{
    using System.Collections;
    using System.Reflection;
    using Core.DataReader.Pol;
    using Core.Primitives;
    using NUnit.Framework;
    using Rendering.Renderer;
    using UnityEngine;
    using UnityEngine.TestTools;
    using Object = UnityEngine.Object;

    public sealed class WeaponBladeLifetimeTests
    {
        [UnityTest]
        public IEnumerator RuntimeCameraWithoutSceneOverrideRendersAndDisposesActiveBlades()
        {
            var owner = new GameObject("Runtime blade owner");
            var camera = owner.AddComponent<Camera>();
            camera.enabled = false;
            Assert.That(camera.scene.IsValid(), Is.False, "A normal runtime camera must keep its default scene override.");
            var tags = new[]
            {
                new TagNode { GameBoxTransformMatrix = new GameBoxMatrix4x4(), TintColor = 0xffffffff },
                new TagNode { GameBoxTransformMatrix = new GameBoxMatrix4x4 { Ty = 20 }, TintColor = 0xffffffff },
            };
            using var blade = new WeaponBlade(tags);
            var material = WeaponBlade.CreateMaterial();
            Mesh mesh = null;
            GameObject flush = null;
            try
            {
                blade.SamplePose(owner.transform, true, .12f, false);
                blade.Advance(.02f, 1, false);
                owner.transform.position = Vector3.right;
                blade.SamplePose(owner.transform, true, .12f, false);
                blade.RenderFlush(camera, material, 0, 0, true);
                blade.RenderFlush(camera, material, 0, 1, true);
                Assert.That(camera.scene.IsValid(), Is.False, "Rendering must not install a camera scene override.");
                const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
                mesh = (Mesh)typeof(WeaponBlade).GetField("_mesh", fields).GetValue(blade);
                flush = ((MeshRenderer[])typeof(WeaponBlade).GetField("_flushRenderers", fields).GetValue(blade))[0].gameObject;
                var renderers = (MeshRenderer[])typeof(WeaponBlade).GetField("_flushRenderers", fields).GetValue(blade);
                var flushMeshes = (Mesh[])typeof(WeaponBlade).GetField("_flushMeshes", fields).GetValue(blade);
                foreach (MeshRenderer renderer in renderers)
                {
                    Assert.That(renderer.enabled, Is.True);
                    Assert.That(renderer.gameObject.scene, Is.EqualTo(owner.scene));
                }
                blade.Dispose();
                Assert.That(mesh == null, Is.False, "Runtime disposal must use deferred Destroy.");
                Assert.That(flush == null, Is.False);
                yield return null;
                Assert.That(mesh == null, Is.True);
                Assert.That(flush == null, Is.True);
                foreach (MeshRenderer renderer in renderers)
                    Assert.That(renderer == null, Is.True);
                foreach (Mesh snapshot in flushMeshes)
                    Assert.That(snapshot == null, Is.True);
            }
            finally
            {
                Object.Destroy(owner);
                Object.Destroy(material);
            }
        }
    }
}
