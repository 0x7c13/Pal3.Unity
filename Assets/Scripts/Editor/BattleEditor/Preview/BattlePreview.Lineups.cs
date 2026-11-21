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
    using UnityEngine;
    using UnityEngine.Rendering;
    using Object = UnityEngine.Object;

    public sealed partial class BattlePreview
    {
        private sealed class LineupView
        {
            public GameObject Object;
            public Mesh Mesh;
            public Material Material;
        }

        private readonly Dictionary<(BattleSide Side, bool Center), LineupView> _lineups = new();

        public IReadOnlyList<BattleLineupFrame> LineupFrames { get; private set; } = Array.Empty<BattleLineupFrame>();

        private LineupView Lineup(BattleSide side, bool center)
        {
            var key = (side, center);
            if (_lineups.TryGetValue(key, out LineupView view))
                return view;
            view = new LineupView { Object = View.CreateObject("Original " + side + (center ? " lineup center" : " five-spirit lineup")) };
            try
            {
                Shader shader = Shader.Find("Pal3/Effect") ?? throw new InvalidOperationException("The original effect shader is missing.");
                view.Material = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    mainTexture = View.GetTexture("basedata.cpk\\cbscene\\fivelineup" + (center ? "_center" : "") + ".tga")
                };
                view.Material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                view.Material.SetInt("_DstBlend", (int)BlendMode.One);
                view.Material.SetInt("_ZTest", (int)CompareFunction.LessEqual);
                view.Material.SetInt("_Cull", (int)CullMode.Off);
                view.Mesh = new Mesh { name = view.Object.name, hideFlags = HideFlags.HideAndDontSave };
                view.Mesh.MarkDynamic();
                view.Object.AddComponent<MeshFilter>().sharedMesh = view.Mesh;
                var renderer = view.Object.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = view.Material;
                renderer.sortingOrder = 3; // Native RenderLineup follows effects and sparks, before Trail.End.
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                _lineups.Add(key, view);
                return view;
            }
            catch
            {
                ReleaseLineup(view);
                throw;
            }
        }

        private void ApplyLineups(BattleSession battle)
        {
            LineupFrames = BattleLineupProjection.Capture(battle);
            foreach (BattleSide side in Enum.GetValues(typeof(BattleSide)))
            {
                BattleLineupFrame[] frames = LineupFrames.Where(frame => frame.Side == side && frame.Alpha > 0).ToArray();
                UpdateLineup(side, false, frames, battle.SceneElement);
                if (side == BattleSide.Ally)
                    UpdateLineup(side, true, frames, battle.SceneElement);
            }
        }

        private void UpdateLineup(BattleSide side, bool center, IReadOnlyList<BattleLineupFrame> frames, int sceneElement)
        {
            if (frames.Count == 0 && !_lineups.ContainsKey((side, center)))
                return;
            LineupView view = Lineup(side, center);
            var vertices = new List<Vector3>();
            var colors = new List<Color32>();
            var uv = new List<Vector2>();
            var indices = new List<int>();
            var config = side == BattleSide.Ally ? _data.Layout.AllyFormationConfig : _data.Layout.EnemyFormationConfig;
            var origin = new BattleMovementPoint(config.CenterGameBoxPosition.X, config.CenterGameBoxPosition.Y, config.CenterGameBoxPosition.Z);
            float roll = side == BattleSide.Ally ? BattleLineupProjection.AllyRoll(config.RollRadians, sceneElement) : config.RollRadians;
            uint rgb = center ? BattleLineupProjection.CenterColor(sceneElement) : 0xffffffu;
            foreach (BattleLineupFrame frame in frames)
            {
                int offset = vertices.Count;
                for (int corner = 0; corner < 4; corner++)
                {
                    vertices.Add(BattleAnimationCatalog.UnityPosition(BattleLineupProjection.Corner(origin, config.GameBoxRadius, roll, corner, center)));
                    colors.Add(new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, frame.Alpha));
                }
                uv.AddRange(new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
                indices.AddRange(new[] { offset, offset + 3, offset + 1, offset + 3, offset + 2, offset + 1 });
            }
            view.Mesh.Clear();
            view.Mesh.SetVertices(vertices);
            view.Mesh.SetColors(colors);
            view.Mesh.SetUVs(0, uv);
            view.Mesh.SetTriangles(indices, 0, true);
            view.Object.GetComponent<MeshRenderer>().enabled = frames.Count > 0;
        }

        private void ReleaseLineup(LineupView view)
        {
            if (view.Object != null)
                View.ReleaseCompositeObject(view.Object);
            if (view.Mesh != null)
                Object.DestroyImmediate(view.Mesh);
            if (view.Material != null)
                Object.DestroyImmediate(view.Material);
        }

        private void DisposeLineups()
        {
            foreach (LineupView view in _lineups.Values)
                ReleaseLineup(view);
            _lineups.Clear();
            LineupFrames = Array.Empty<BattleLineupFrame>();
        }
    }
}
