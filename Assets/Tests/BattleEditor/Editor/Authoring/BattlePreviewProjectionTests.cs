// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using Battle;
    using NUnit.Framework;
    using ResourceBrowser;
    using UnityEngine;

    public sealed class BattlePreviewProjectionTests
    {
        [TestCase(EventType.MouseDrag, 0, EventModifiers.None)]
        [TestCase(EventType.MouseDrag, 0, EventModifiers.Shift)]
        [TestCase(EventType.MouseDrag, 2, EventModifiers.None)]
        [TestCase(EventType.ScrollWheel, 0, EventModifiers.None)]
        [TestCase(EventType.KeyDown, 0, EventModifiers.None)]
        public void LockedCameraIgnoresInspectionGesturesWhilePreviewCameraStillResponds(EventType type, int button, EventModifiers modifiers)
        {
            using var preview = new ResourceModelPreview(null, false);
            preview.SetHorizontalCameraOrientation(Vector3.back * 10, Vector3.forward, Vector3.up, 50);
            var viewport = new Rect(0, 0, 800, 600);
            var marker = new Vector3(2, 1, 10);
            Vector2 before = preview.ProjectReference(marker, 800, 600);
            var input = new Event
            {
                type = type,
                button = button,
                modifiers = modifiers,
                keyCode = KeyCode.F,
                mousePosition = viewport.center,
                delta = new Vector2(36, 24)
            };
            var handle = typeof(ResourceModelPreview).GetMethod("HandleCameraInput", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            handle.Invoke(preview, new object[] { input, viewport, false });
            Assert.That(Vector2.Distance(preview.ProjectReference(marker, 800, 600), before), Is.LessThan(.0001));
            Assert.That(input.type, Is.EqualTo(type), "Locked camera must leave HUD input available.");
            handle.Invoke(preview, new object[] { input, viewport, true });
            Assert.That(Vector2.Distance(preview.ProjectReference(marker, 800, 600), before), Is.GreaterThan(1));
            Assert.That(input.type, Is.EqualTo(EventType.Used));
        }

        [TestCase(800, 600)]
        [TestCase(1600, 900)]
        public void NativeHorizontalFrustumProjectsTheSameHorizontalEdge(int width, int height)
        {
            using var preview = new ResourceModelPreview(null, false);
            preview.SetHorizontalCameraOrientation(Vector3.zero, Vector3.forward, Vector3.up, 50);
            // gbCamera: hw = near*tan(fov/2); hh = hw*height/width.
            const float depth = 10;
            float halfWidth = depth * 0.466307658f;
            Vector2 right = preview.ProjectReference(new Vector3(halfWidth, 0, depth), width, height);
            Vector2 top = preview.ProjectReference(new Vector3(0, halfWidth * height / width, depth), width, height);
            Assert.That(right.x, Is.EqualTo(width).Within(.002));
            Assert.That(right.y, Is.EqualTo(height / 2f).Within(.002));
            Assert.That(top.y, Is.Zero.Within(.002));
            Assert.That(preview.HorizontalFieldOfView, Is.EqualTo(50));
        }

        [TestCase(800, 600, false)]
        [TestCase(1600, 900, false)]
        [TestCase(800, 800, false)]
        [TestCase(600, 900, false)]
        [TestCase(1600, 900, true)]
        public void TrailPixelsProjectionAndPickingShareThePresentedFrustum(int width, int height, bool expand)
        {
            using var preview = new ResourceModelPreview(null, false) { ExpandReferenceFraming = expand };
            using var trail = new BattleTrailPreview { Enabled = true };
            preview.FrameEffect = trail;
            preview.SetHorizontalCameraOrientation(Vector3.zero, Vector3.forward, Vector3.up, 50);
            var view = new Rect(17, 29, width, height);
            var world = new Vector3(1, 1, 10);
            preview.SetViewportContext(view);
            preview.Project(world, view, out Vector2 before);
            var target = preview.CreateObject("Projection marker");
            target.transform.position = world;
            var mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = new[] { new Vector3(-.25f, -.25f, 0), new Vector3(-.25f, .25f, 0), new Vector3(.25f, .25f, 0), new Vector3(.25f, -.25f, 0) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            target.AddComponent<MeshFilter>().sharedMesh = mesh;
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { hideFlags = HideFlags.HideAndDontSave };
            material.SetColor("_BaseColor", Color.red);
            target.AddComponent<MeshRenderer>().sharedMaterial = material;
            preview.Rendering += camera => camera.backgroundColor = Color.black;
            var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = preview.Render(width, height);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                string output = System.Environment.GetEnvironmentVariable("PAL3_PROJECTION_QA_ROOT");
                if (output != null)
                {
                    System.IO.Directory.CreateDirectory(output);
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(output, $"trail-{width}-{height}-{expand}.png"), image.EncodeToPNG());
                }
                preview.Project(world, view, out Vector2 after);
                Assert.That(Vector2.Distance(before, after), Is.LessThan(.002), "Rendering must not change the input/overlay camera.");
                Ray ray = preview.ScreenRay(after, view);
                Assert.That(Vector3.Distance(ray.GetPoint((world.z - ray.origin.z) / ray.direction.z), world), Is.LessThan(.0001));
                var pixels = image.GetPixels32();
                double x = 0, y = 0, weight = 0;
                for (int index = 0; index < pixels.Length; index++)
                {
                    int red = pixels[index].r - System.Math.Max(pixels[index].g, pixels[index].b);
                    if (red <= 4)
                        continue;
                    weight += red;
                    x += (index % width + .5) * red;
                    y += (height - index / width - .5) * red;
                }
                Assert.That(weight, Is.GreaterThan(100));
                var visible = new Vector2(view.x + (float)(x / weight), view.y + (float)(y / weight));
                Assert.That(Vector2.Distance(visible, after), Is.LessThan(width / 256f * 1.5f), "The rendered marker must agree with HUD projection and picking.");
                TestContext.WriteLine($"{width}x{height}, expand={expand}: projected={after}, visible={visible}");
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(image);
            }
        }

        [Test]
        public void AuthoringCameraRetainsItsVerticalAngleAndUpdatesBeforeRepaint()
        {
            using var preview = new ResourceModelPreview(null, false);
            preview.SetHorizontalCameraOrientation(Vector3.zero, Vector3.forward, Vector3.up, 50);
            preview.ProjectReference(Vector3.forward, 800, 600);
            preview.SetCamera(new Vector3(10, 0, 0), new Vector3(10, 0, 5), 60);
            Vector2 center = preview.ProjectReference(new Vector3(10, 0, 10), 800, 600);
            Assert.That(Vector2.Distance(center, new Vector2(400, 300)), Is.LessThan(.0001));
            preview.GetCamera(out Vector3 position, out _, out float fieldOfView);
            Assert.That(Vector3.Distance(position, new Vector3(10, 0, 0)), Is.LessThan(.00001));
            Assert.That(fieldOfView, Is.EqualTo(60));
        }

        [Test]
        public void ThrownWeaponUsesTheMirroredNativeRotationStack()
        {
            // Source rows: Rz(90)*Rx(90)*Ry(90), followed by X reflection.
            Quaternion rotation = BattlePreview.ThrownWeaponRotation(new BattleMovementPoint(90, 90, 90));
            Assert.That(Vector3.Distance(rotation * Vector3.left, Vector3.left), Is.LessThan(.00001));
            Assert.That(Vector3.Distance(rotation * Vector3.up, Vector3.forward), Is.LessThan(.00001));
            Assert.That(Vector3.Distance(rotation * Vector3.forward, Vector3.down), Is.LessThan(.00001));
        }
    }
}
