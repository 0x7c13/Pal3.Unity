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
    using Core.Contracts;
    using Core.DataLoader;
    using Core.DataReader.Cpk;
    using Core.DataReader.Pol;
    using Core.DataReader.Scn;
    using Core.Extensions;
    using Core.Utils;
    using Data;
    using Rendering.Renderer;
    using UnityEngine;

    [ScnSceneObject(SceneObjectType.RotatingWall)]
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
            if (IsActivated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            // Add mesh collider to block player
            _meshCollider = sceneGameObject.AddComponent<SceneObjectMeshCollider>();
            _meshCollider.Init(new Vector3(-1f, 0f, 0f));

            // Add sub-component to the main object.
            if (ModelFileVirtualPath.EndsWith("1.pol"))
            {
                _subObjectGameObject = new GameObject($"Object_{ObjectInfo.Id}_{ObjectInfo.Type}_SubObject");

                var subObjectModelPath = ModelFileVirtualPath.Replace("1.pol", "2.pol");
                PolFile polFile = resourceProvider.GetGameResourceFile<PolFile>(subObjectModelPath);
                ITextureResourceProvider textureProvider = resourceProvider.CreateTextureResourceProvider(
                    Utility.GetDirectoryName(subObjectModelPath, CpkConstants.DirectorySeparatorChar));
                var subObjectModelRenderer = _subObjectGameObject.AddComponent<PolyModelRenderer>();
                subObjectModelRenderer.Render(polFile,
                    textureProvider,
                    resourceProvider.GetMaterialFactory(),
                    isStaticObject: false,
                    tintColor);

                // Sub-object should block player as well, so let's add mesh collider to it
                _subObjectMeshCollider = _subObjectGameObject.AddComponent<SceneObjectMeshCollider>();
                _subObjectMeshCollider.Init(new Vector3(0f, 0f, -1f));

                _subObjectGameObject.transform.SetParent(sceneGameObject.transform, false);
            }

            return sceneGameObject;
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            PlaySfxIfAny();

            Transform objectTransform = GetGameObject().transform;
            Quaternion rotation = objectTransform.rotation;
            Quaternion targetRotation = rotation * Quaternion.Euler(0, -ObjectInfo.Parameters[1], 0);

            yield return objectTransform.RotateAsync(targetRotation, 2.2f, AnimationCurveType.Sine);

            SaveCurrentYRotation();
        }

        public override void Deactivate()
        {
            if (_meshCollider != null)
            {
                _meshCollider.Destroy();
                _meshCollider = null;
            }

            if (_subObjectMeshCollider != null)
            {
                _subObjectMeshCollider.Destroy();
                _subObjectMeshCollider = null;
            }

            if (_subObjectGameObject != null)
            {
                _subObjectGameObject.Destroy();
                _subObjectGameObject = null;
            }

            base.Deactivate();
        }
    }
}

#endif