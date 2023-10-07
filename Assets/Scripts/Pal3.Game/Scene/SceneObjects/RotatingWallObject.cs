// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3A

namespace Pal3.Game.Scene.SceneObjects
{
    using System.Collections;
    using Common;
    using Core.Contract.Enums;
    using Core.DataReader.Cpk;
    using Core.DataReader.Pol;
    using Core.DataReader.Scn;
    using Core.Utilities;
    using Data;
    using Engine.Abstraction;
    using Engine.Animation;
    using Engine.DataLoader;
    using Engine.Extensions;
    using Rendering.Renderer;

    using Color = Core.Primitives.Color;
    using Quaternion = UnityEngine.Quaternion;
    using Vector3 = UnityEngine.Vector3;

    [ScnSceneObject(SceneObjectType.RotatingWall)]
    public sealed class RotatingWallObject : SceneObject
    {
        private SceneObjectMeshCollider _meshCollider;

        private IGameEntity _subObjectGameEntity;
        private SceneObjectMeshCollider _subObjectMeshCollider;

        public RotatingWallObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();
            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            // Add mesh collider to block player
            _meshCollider = sceneObjectGameEntity.AddComponent<SceneObjectMeshCollider>();
            _meshCollider.Init(new Vector3(-1f, 0f, 0f));

            // Add sub-component to the main object.
            if (ModelFileVirtualPath.EndsWith("1.pol"))
            {
                _subObjectGameEntity = new GameEntity($"Object_{ObjectInfo.Id}_{ObjectInfo.Type}_SubObject");

                var subObjectModelPath = ModelFileVirtualPath.Replace("1.pol", "2.pol");
                PolFile polFile = resourceProvider.GetGameResourceFile<PolFile>(subObjectModelPath);
                ITextureResourceProvider textureProvider = resourceProvider.CreateTextureResourceProvider(
                    CoreUtility.GetDirectoryName(subObjectModelPath, CpkConstants.DirectorySeparatorChar));
                var subObjectModelRenderer = _subObjectGameEntity.AddComponent<PolyModelRenderer>();
                subObjectModelRenderer.Render(polFile,
                    textureProvider,
                    resourceProvider.GetMaterialFactory(),
                    isStaticObject: false,
                    tintColor);

                // Sub-object should block player as well, so let's add mesh collider to it
                _subObjectMeshCollider = _subObjectGameEntity.AddComponent<SceneObjectMeshCollider>();
                _subObjectMeshCollider.Init(new Vector3(0f, 0f, -1f));

                _subObjectGameEntity.SetParent(sceneObjectGameEntity, worldPositionStays: false);
            }

            return sceneObjectGameEntity;
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            PlaySfxIfAny();

            ITransform objectTransform = GetGameEntity().Transform;
            Quaternion rotation = objectTransform.Rotation;
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

            if (_subObjectGameEntity != null)
            {
                _subObjectGameEntity.Destroy();
                _subObjectGameEntity = null;
            }

            base.Deactivate();
        }
    }
}

#endif