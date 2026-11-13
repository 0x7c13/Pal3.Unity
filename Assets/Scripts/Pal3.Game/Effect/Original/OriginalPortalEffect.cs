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

        public bool AutoAdvance { get; set; } = true;

        public void Initialize(OriginalEffectResources resources)
        {
            GameObject Child(string name)
            {
                var go = new GameObject(name) { hideFlags = HideFlags.DontSave, layer = gameObject.layer };
                go.transform.SetParent(transform, false);
                return go;
            }
            _base = new EffectMeshBuffer(Child("Transport plane"), resources.Material("effect\\trans.tga", true, false, 2));
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
            Cone(_outer, 41, 28, 50, rotation, new Color32(245, 188, 28, 255));
            Cone(_inner, 34, 18, 68, Quaternion.AngleAxis(-time * 32 * 2, Vector3.up), new Color32(10, 67, 227, 255));
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
