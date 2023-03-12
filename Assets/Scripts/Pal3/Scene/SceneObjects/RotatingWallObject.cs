// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3A

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Common;
    using Core.Animation;
    using Core.DataLoader;
    using Core.DataReader.Pol;
    using Core.DataReader.Scn;
    using Data;
    using Renderer;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.RotatingWall)]
    public sealed class RotatingWallObject : SceneObject
    {
        private SceneObjectMeshCollider _meshCollider;

        private GameObject _subObjectGameObject;
        private SceneObjectMeshCollider _subObjectMeshCollider;

        public RotatingWallObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            // Add mesh collider to the main object.
            _meshCollider = sceneGameObject.AddComponent<SceneObjectMeshCollider>();
            _meshCollider.Init(new Vector3(-1f, 0f, 0f));

            // Add sub-component to the main object.
            if (ModelFilePath.EndsWith("1.pol"))
            {
                _subObjectGameObject = new GameObject($"Object_{ObjectInfo.Id}_{ObjectInfo.Type}_SubObject");

                var subObjectModelPath = ModelFilePath.Replace("1.pol", "2.pol");
                (PolFile PolFile, ITextureResourceProvider TextureProvider) poly = resourceProvider.GetPol(subObjectModelPath);
                var subObjectModelRenderer = _subObjectGameObject.AddComponent<PolyModelRenderer>();
                subObjectModelRenderer.Render(poly.PolFile,
                    resourceProvider.GetMaterialFactory(),
                    poly.TextureProvider,
                    tintColor);

                // Add mesh collider to the sub-component.
                _subObjectMeshCollider = _subObjectGameObject.AddComponent<SceneObjectMeshCollider>();
                _subObjectMeshCollider.Init(new Vector3(0f, 0f, -1f));

                _subObjectGameObject.transform.SetParent(sceneGameObject.transform, false);
            }

            return sceneGameObject;
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            PlaySfxIfAny();

            Transform objectTransform = GetGameObject().transform;
            Quaternion rotation = objectTransform.rotation;
            Quaternion targetRotation = rotation * Quaternion.Euler(0, -ObjectInfo.Parameters[1], 0);

            yield return AnimationHelper.RotateTransformAsync(objectTransform,
                targetRotation, 2.5f, AnimationCurveType.Sine);

            SaveYRotation();
        }

        public override void Deactivate()
        {
            if (_meshCollider != null)
            {
                Object.Destroy(_meshCollider);
            }

            if (_subObjectMeshCollider != null)
            {
                Object.Destroy(_subObjectMeshCollider);
            }

            if (_subObjectGameObject != null)
            {
                Object.Destroy(_subObjectGameObject);
            }

            base.Deactivate();
        }
    }
}

#endif