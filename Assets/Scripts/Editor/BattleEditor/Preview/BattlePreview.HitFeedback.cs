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
    using Engine.Extensions;
    using UnityEngine;
    using UnityEngine.Rendering;
    using Object = UnityEngine.Object;

    public sealed partial class BattlePreview
    {
        private BattleSession _hitFeedbackOwner;
        private IReadOnlyList<BattleHitFlashSnapshot> _hitFeedbackFrames = Array.Empty<BattleHitFlashSnapshot>();
        private readonly List<BattleWeaponSpark> _weaponSparks = new();
        private BattleRandom _weaponSparkRandom;
        private double? _lastWeaponSparkTime;
        private float _weaponSparkEmissionTime;
        private GameObject _hitFlashObject, _weaponSparkObject;
        private Mesh _hitFlashMesh, _weaponSparkMesh;
        private Material _hitFlashMaterial, _weaponSparkMaterial;

        public IReadOnlyList<BattleHitFlashSnapshot> HitFlashFrames => _hitFeedbackFrames;

        public int WeaponSparkCount => _weaponSparks.Count;

        public void ApplyHitFeedback(BattleSession combat)
        {
            if (_hitFeedbackOwner != combat)
            {
                _hitFeedbackOwner = combat;
                _weaponSparks.Clear();
                _weaponSparkRandom = new BattleRandom(_scenario.Seed);
                _lastWeaponSparkTime = null;
                _weaponSparkEmissionTime = 0;
            }
            _hitFeedbackFrames = combat.Status == BattleSessionStatus.Running ? combat.HitFlashes : Array.Empty<BattleHitFlashSnapshot>();
        }

        private void EnsureHitFeedbackMesh(bool sparks)
        {
            if (sparks ? _weaponSparkObject != null : _hitFlashObject != null)
                return;
            Shader shader = Shader.Find("Pal3/Effect");
            if (shader == null)
                throw new InvalidOperationException("The battle hit feedback shader is missing.");
            Texture texture = View.GetTexture(sparks ? "basedata.cpk\\effect\\flare2.bmp" : "basedata.cpk\\effect\\combat\\100\\z01\\z01.tga");
            GameObject root = null;
            Mesh mesh = null;
            Material material = null;
            try
            {
                root = View.CreateObject(sparks ? "Original weapon red sparks" : "Original monster hit flashes");
                mesh = new Mesh { name = root.name, hideFlags = HideFlags.HideAndDontSave };
                mesh.MarkDynamic();
                material = new Material(shader)
                {
                    name = root.name,
                    hideFlags = HideFlags.HideAndDontSave,
                    mainTexture = texture
                };
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.One);
                material.SetInt("_Cull", (int)CullMode.Off);
                material.SetInt("_ZTest", (int)CompareFunction.LessEqual);
                root.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = root.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.sortingOrder = sparks ? 2 : 4;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                renderer.enabled = sparks;
            }
            catch
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(material);
                throw;
            }
            if (sparks)
            {
                _weaponSparkObject = root;
                _weaponSparkMesh = mesh;
                _weaponSparkMaterial = material;
            }
            else
            {
                _hitFlashObject = root;
                _hitFlashMesh = mesh;
                _hitFlashMaterial = material;
            }
        }

        private void RenderHitFeedback(Camera camera, RenderTexture target)
        {
            if (camera == null || target == null || _hitFeedbackOwner == null || _hitFeedbackOwner.IsModalPresentation)
                return;
            if (_hitFeedbackFrames.Count > 0)
                EnsureHitFeedbackMesh(false);
            if (_hitFlashMesh != null)
            {
                var vertices = new List<Vector3>();
                var uv = new List<Vector2>();
                var colors = new List<Color32>();
                var indices = new List<int>();
                foreach (BattleHitFlashSnapshot flash in _hitFeedbackFrames)
                {
                    Vector3 P(float x, float z) => BattleAnimationCatalog.UnityPosition(new BattleMovementPoint(
                        flash.Position.X + x, flash.Position.Y, flash.Position.Z + z));
                    float left = flash.Frame % 4 / 4f, bottom = flash.Frame / 4 / 4f;
                    AppendFeedbackQuad(vertices, uv, colors, indices,
                        P(-100, 100), P(100, 100), P(100, -100), P(-100, -100),
                        new Vector2(left, bottom), new Vector2(left + .25f, bottom + .25f), new Color32(255, 255, 255, 255));
                }
                UploadHitFeedback(_hitFlashMesh, _hitFlashObject, vertices, uv, colors, indices);
                // CEffectSimple is rendered after CTrail::End, so this mesh
                // must never participate in the ordinary scene/history pass.
                _hitFlashObject.GetComponent<MeshRenderer>().enabled = false;
                if (vertices.Count > 0)
                {
                    // CTrail::End clears the original target and depth. Its
                    // composited target has no depth attachment; the ordinary
                    // camera target still retains scene occlusion.
                    _hitFlashMaterial.SetInt("_ZTest", (int)(target.depth == 0 ? CompareFunction.Always : CompareFunction.LessEqual));
                    RenderTexture previous = RenderTexture.active;
                    using var commands = new CommandBuffer { name = "Original post-trail hit flash" };
                    try
                    {
                        commands.SetRenderTarget(target);
                        commands.SetViewport(new Rect(0, 0, target.width, target.height));
                        // Match URP SetCameraMatrices: this command accepts the
                        // camera projection and performs the backend conversion.
                        // Pre-converting it flips Metal's render texture twice.
                        commands.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
                        commands.DrawMesh(_hitFlashMesh, Matrix4x4.identity, _hitFlashMaterial);
                        Graphics.ExecuteCommandBuffer(commands);
                    }
                    finally
                    {
                        RenderTexture.active = previous;
                    }
                }
            }
        }

        private void RenderWeaponSparks(Camera camera)
        {
            if (BattleData.Variant != "PAL3" || camera == null || _hitFeedbackOwner == null)
                return;
            double time = _hitFeedbackOwner.SimulationTick * BattleSession.StepSeconds;
            if (_resultBattle != null)
                time += Math.Max(0, _bladeFrameTime - _resultStart);
            bool newFrame = !_lastWeaponSparkTime.HasValue || _lastWeaponSparkTime.Value != time;
            if (newFrame)
            {
                float delta = _lastWeaponSparkTime.HasValue ? (float)Math.Max(0, time - _lastWeaponSparkTime.Value) : 0;
                _lastWeaponSparkTime = time;
                foreach (BattleWeaponSpark spark in _weaponSparks)
                    spark.Advance(delta);
                _weaponSparks.RemoveAll(spark => !spark.Alive);
                // Render's static timer advances only while Zi Xuan's right
                // weapon exists and is drawn. GetLastBladePos returns vertex
                // zero of the most recently retained native blade record.
                foreach (ActorView actor in _actors.Values)
                {
                    if (!actor.Visible || actor.Actor.Side != BattleSide.Ally)
                        continue;
                    BattleActorDefinition definition = _hitFeedbackOwner.Definitions.Actors[actor.Actor.InstanceId];
                    if (definition.Identity != 3 || definition.Equipment.FirstOrDefault(item => item.Slot == 0)?.WeaponType != 3 ||
                        !actor.Renderer.RightWeaponVisible || !actor.Renderer.TryGetRightWeaponBladeTip(out Vector3 position))
                        continue;
                    _weaponSparkEmissionTime += delta;
                    if (_weaponSparkEmissionTime >= .05f)
                    {
                        _weaponSparkEmissionTime -= .05f;
                        if (_weaponSparks.Count < 128)
                        {
                            float scale = UnityPrimitivesConvertor.GameBoxUnitToUnityUnit;
                            _weaponSparks.Add(new BattleWeaponSpark(new BattleMovementPoint(-position.x * scale, position.y * scale, position.z * scale), _weaponSparkRandom));
                        }
                    }
                }
            }
            if (_weaponSparks.Count > 0)
                EnsureHitFeedbackMesh(true);
            if (_weaponSparkMesh == null)
                return;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var colors = new List<Color32>();
            var indices = new List<int>();
            float radius = BattleWeaponSpark.Size / 2 / UnityPrimitivesConvertor.GameBoxUnitToUnityUnit;
            Vector3 right = camera.transform.right * radius, up = camera.transform.up * radius;
            foreach (BattleWeaponSpark spark in _weaponSparks)
            {
                Vector3 center = BattleAnimationCatalog.UnityPosition(spark.Position);
                AppendFeedbackQuad(vertices, uv, colors, indices,
                    center - right - up, center + right - up, center + right + up, center - right + up,
                    Vector2.zero, Vector2.one, new Color32(255, 255, 255, spark.Alpha));
            }
            UploadHitFeedback(_weaponSparkMesh, _weaponSparkObject, vertices, uv, colors, indices);
        }

        private static void AppendFeedbackQuad(List<Vector3> vertices, List<Vector2> uv, List<Color32> colors, List<int> indices,
            Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 minimum, Vector2 maximum, Color32 color)
        {
            int first = vertices.Count;
            vertices.AddRange(new[] { a, b, c, d });
            uv.AddRange(new[] { minimum, new Vector2(maximum.x, minimum.y), maximum, new Vector2(minimum.x, maximum.y) });
            colors.AddRange(new[] { color, color, color, color });
            indices.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
        }

        private static void UploadHitFeedback(Mesh mesh, GameObject root, List<Vector3> vertices, List<Vector2> uv, List<Color32> colors, List<int> indices)
        {
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetColors(colors);
            mesh.SetTriangles(indices, 0);
            root.GetComponent<MeshRenderer>().enabled = vertices.Count > 0;
        }

        private void DisposeHitFeedback()
        {
            foreach (Mesh mesh in new[] { _hitFlashMesh, _weaponSparkMesh })
                if (mesh != null)
                    Object.DestroyImmediate(mesh);
            foreach (Material material in new[] { _hitFlashMaterial, _weaponSparkMaterial })
                if (material != null)
                    Object.DestroyImmediate(material);
            foreach (GameObject root in new[] { _hitFlashObject, _weaponSparkObject })
                if (root != null)
                    View.ReleaseCompositeObject(root);
            _hitFeedbackFrames = Array.Empty<BattleHitFlashSnapshot>();
            _weaponSparks.Clear();
            _hitFeedbackOwner = null;
        }
    }
}
