// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect.Original
{
    using UnityEngine;

    // MXTransport is a scene effect, not flexeff group zero.
    public sealed class OriginalPortalEffect : MonoBehaviour
    {
        private EffectMeshBuffer _base, _outer, _inner;
        private float _time;
        private Color32 _outerColor = new(245, 188, 28, 255), _innerColor = new(10, 67, 227, 255);

        public bool AutoAdvance { get; set; } = true;

        public void Initialize(OriginalEffectResources resources, uint type = 0)
        {
            GameObject Child(string name)
            {
                var go = new GameObject(name) { hideFlags = HideFlags.DontSave, layer = gameObject.layer };
                go.transform.SetParent(transform, false);
                return go;
            }
            string texture = "effect\\trans.tga";
            if (resources.GameVariant == "PAL3A")
            {
                // MXTransport::Create 0x4aec50: the scene parameter selects
                // one of six discs and independently authored cone colors.
                (string suffix, Color32 outer, Color32 inner) style = type switch
                {
                    1 => ("s", new Color32(0, 238, 255, 255), new Color32(210, 255, 249, 255)),
                    2 => ("m", new Color32(246, 0, 208, 255), new Color32(254, 224, 255, 255)),
                    3 => ("l", new Color32(108, 0, 255, 255), new Color32(214, 184, 255, 255)),
                    4 => ("f", new Color32(0, 255, 112, 255), new Color32(199, 255, 200, 255)),
                    5 => ("t", new Color32(255, 218, 0, 255), new Color32(255, 250, 163, 255)),
                    6 => ("p", new Color32(255, 0, 0, 255), new Color32(255, 152, 152, 255)),
                    _ => (null, _outerColor, _innerColor)
                };
                if (style.suffix != null)
                    texture = $"effect\\trans_{style.suffix}.tga";
                _outerColor = style.outer;
                _innerColor = style.inner;
            }
            _base = new EffectMeshBuffer(Child("Transport plane"), resources.Material(texture, true, false, 2));
            _outer = new EffectMeshBuffer(Child("Transport outer cone"), resources.Material("effect\\ray.tga", true, false, 2));
            _inner = new EffectMeshBuffer(Child("Transport inner cone"), resources.Material("effect\\ray.tga", true, false, 2));
            Sample(0);
        }

        public void Sample(float time)
        {
            _time = time;
            if (_base == null)
                return;
            _base.Clear();
            Quaternion rotation = Quaternion.AngleAxis(-time * 32 * 0.2f, Vector3.up);
            Vector3 P(float x, float z) => OriginalEffectNode.UnityPosition(rotation * new Vector3(x, 0, z));
            _base.Quad(P(-36, -36), P(-36, 36), P(36, 36), P(36, -36), Vector2.zero, Vector2.one, Color.white);
            _base.Upload();
            Cone(_outer, 41, 28, 50, rotation, _outerColor);
            Cone(_inner, 34, 18, 68, Quaternion.AngleAxis(-time * 32 * 2, Vector3.up), _innerColor);
        }

        private static void Cone(EffectMeshBuffer mesh, float top, float bottom, float height, Quaternion rotation, Color32 color)
        {
            mesh.Clear();
            Vector3 P(int i, float radius, float y)
            {
                float a = i * 2 * Mathf.PI / 6;
                return OriginalEffectNode.UnityPosition(rotation * new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius));
            }
            for (int i = 0; i < 6; i++)
                mesh.Quad(P(i, bottom, 0), P(i + 1, bottom, 0), P(i + 1, top, height), P(i, top, height),
                    new Vector2(i / 3f, 0), new Vector2((i + 1) / 3f, 0.94f), color);
            mesh.Upload();
        }

        private void Update()
        {
            if (AutoAdvance)
                Sample(_time + Time.deltaTime);
        }

        private void OnDestroy()
        {
            _base?.Dispose();
            _outer?.Dispose();
            _inner?.Dispose();
        }
    }
}
