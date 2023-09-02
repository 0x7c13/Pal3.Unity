// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene
{
    using System.Collections.Generic;
    using Actor;
    using Actor.Controllers;
    using Core.Contracts;
    using Core.DataLoader;
    using Core.DataReader.Cpk;
    using Core.DataReader.Gdb;
    using Core.DataReader.Ini;
    using Core.DataReader.Pol;
    using Core.GameBox;
    using Data;
    using GameSystems.Combat;
    using MetaData;
    using Rendering.Material;
    using Rendering.Renderer;
    using UnityEngine;

    public sealed class CombatScene : MonoBehaviour
    {
        private const string COMBAT_CONFIG_FILE_NAME = "combat.ini";

        private GameResourceProvider _resourceProvider;
        private IMaterialFactory _materialFactory;

        private string _combatSceneName;
        private bool _isLightingEnabled;

        private GameObject _parent;
        private GameObject _mesh;

        private CombatConfigFile _combatConfigFile;

        private static readonly Quaternion EnemyFormationRotation = Quaternion.Euler(0, 145, 0);
        private static readonly Quaternion PlayerFormationRotation = Quaternion.Euler(0, -45, 0);

        private Dictionary<ElementPosition, CombatActorController> _combatActorControllers;

        public void Init(GameResourceProvider resourceProvider)
        {
            _resourceProvider = resourceProvider;
            _materialFactory = resourceProvider.GetMaterialFactory();

            _combatConfigFile = _resourceProvider.GetGameResourceFile<CombatConfigFile>(
                FileConstants.DataScriptFolderVirtualPath + COMBAT_CONFIG_FILE_NAME);
        }

        public void Load(GameObject parent,
            string combatSceneName,
            bool isLightingEnabled)
        {
            _parent = parent;
            _combatSceneName = combatSceneName;
            _isLightingEnabled = isLightingEnabled;

            var meshFileRelativeFolderPath = FileConstants.CombatSceneFolderVirtualPath + combatSceneName;

            ITextureResourceProvider sceneTextureProvider = _resourceProvider.CreateTextureResourceProvider(
                meshFileRelativeFolderPath);

            PolFile polFile = _resourceProvider.GetGameResourceFile<PolFile>(meshFileRelativeFolderPath +
                CpkConstants.DirectorySeparatorChar + combatSceneName.ToLower() + ".pol");

            RenderMesh(polFile, sceneTextureProvider);
        }

        private void RenderMesh(PolFile polFile, ITextureResourceProvider textureProvider)
        {
            // Render mesh
            _mesh = new GameObject($"Mesh_{_combatSceneName}")
            {
                isStatic = true // Combat Scene mesh is static
            };

            var polyMeshRenderer = _mesh.AddComponent<PolyModelRenderer>();
            _mesh.transform.SetParent(_parent.transform, false);

            polyMeshRenderer.Render(polFile,
                textureProvider,
                _materialFactory,
                isStaticObject: true); // Combat Scene mesh is static
        }

        public Vector3 GetWorldPosition(ElementPosition position)
        {
            int positionIndex = (int) position;

            if (position >= ElementPosition.EnemyWater)
            {
                positionIndex--;
            }

            return position switch
            {
                ElementPosition.AllyCenter => _combatConfigFile.AllyFormationConfig.CenterGameBoxPosition.ToUnityPosition(),
                ElementPosition.EnemyCenter => _combatConfigFile.EnemyFormationConfig.CenterGameBoxPosition.ToUnityPosition(),
                _ => _combatConfigFile.ActorGameBoxPositions[positionIndex].ToUnityPosition()
            };
        }

        public CombatActorController GetCombatActorController(ElementPosition elementPosition)
        {
            return _combatActorControllers.TryGetValue(elementPosition,
                out CombatActorController combatActorController) ? combatActorController : null;
        }

        public void LoadActors(Dictionary<ElementPosition, CombatActorInfo> combatActors,
            MeetType meetType)
        {
            _combatActorControllers = new Dictionary<ElementPosition, CombatActorController>();

            foreach ((ElementPosition elementPosition, CombatActorInfo actorInfo) in combatActors)
            {
                CombatActorInfo combatActorInfo = actorInfo;

                if (combatActorInfo.Type == CombatActorType.MainActor)
                {
                    // Player actor's model id is not set in the file but it is equal
                    // to its id in string format
                    combatActorInfo.ModelId = combatActorInfo.Id.ToString();
                }

                CombatActor combatActor = new CombatActor(_resourceProvider, combatActorInfo);
                GameObject combatActorGameObject = ActorFactory.CreateCombatActorGameObject(
                    _resourceProvider,
                    combatActor,
                    elementPosition,
                    isDropShadowEnabled: !_isLightingEnabled);
                combatActorGameObject.transform.SetParent(_parent.transform, false);

                Quaternion rotation;

                if (elementPosition <= ElementPosition.AllyCenter)
                {
                    rotation = meetType is MeetType.PlayerChasingEnemy or MeetType.RunningIntoEachOther
                        ? PlayerFormationRotation
                        : EnemyFormationRotation;
                }
                else
                {
                    rotation = meetType is MeetType.EnemyChasingPlayer or MeetType.RunningIntoEachOther
                        ? EnemyFormationRotation
                        : PlayerFormationRotation;
                }

                combatActorGameObject.transform.SetPositionAndRotation(GetWorldPosition(elementPosition), rotation);

                _combatActorControllers[elementPosition] = combatActorGameObject.GetComponent<CombatActorController>();
            }
        }
    }
}