// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene
{
    using System.Collections.Generic;
    using Actor;
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
        private Light _mainLight;

        private (PolFile PolFile, ITextureResourceProvider TextureProvider) _scenePolyMesh;
        private CombatConfigFile _combatConfigFile;

        private static readonly Quaternion EnemyFormationRotation = Quaternion.Euler(0, 145, 0);
        private static readonly Quaternion PlayerFormationRotation = Quaternion.Euler(0, -45, 0);

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

            _scenePolyMesh = (polFile, sceneTextureProvider);

            RenderMesh();
        }

        private void RenderMesh()
        {
            // Render mesh
            _mesh = new GameObject($"Mesh_{_combatSceneName}")
            {
                isStatic = true // Combat Scene mesh is static
            };

            var polyMeshRenderer = _mesh.AddComponent<PolyModelRenderer>();
            _mesh.transform.SetParent(_parent.transform, false);

            polyMeshRenderer.Render(_scenePolyMesh.PolFile,
                _scenePolyMesh.TextureProvider,
                _materialFactory,
                isStaticObject: true); // Combat Scene mesh is static
        }

        public void LoadActors(Dictionary<int, CombatActorInfo> enemyActors,
            Dictionary<int, CombatActorInfo> playerActors,
            MeetType meetType)
        {
            foreach ((int positionIndex, CombatActorInfo actorInfo) in playerActors)
            {
                CombatActorInfo combatActorInfo = actorInfo;
                combatActorInfo.ModelId = combatActorInfo.Id.ToString();

                CombatActor combatActor = new CombatActor(_resourceProvider, combatActorInfo);
                GameObject combatActorGameObject = ActorFactory.CreateCombatActorGameObject(
                    _resourceProvider,
                    combatActor,
                    isDropShadowEnabled: !_isLightingEnabled);
                combatActorGameObject.transform.SetParent(_parent.transform, false);

                Vector3 position = GameBoxInterpreter.ToUnityPosition(positionIndex == 5 ?
                    _combatConfigFile.PlayerFormationConfig.CenterGameBoxPosition :
                    _combatConfigFile.ActorGameBoxPositions[positionIndex]);

                combatActorGameObject.transform.SetPositionAndRotation(position,
                    meetType is MeetType.PlayerChasingEnemy or MeetType.RunningIntoEachOther
                        ? PlayerFormationRotation
                        : EnemyFormationRotation);
            }

            foreach ((int positionIndex, CombatActorInfo actorInfo) in enemyActors)
            {
                CombatActor combatActor = new CombatActor(_resourceProvider, actorInfo);
                GameObject combatActorGameObject = ActorFactory.CreateCombatActorGameObject(
                    _resourceProvider,
                    combatActor,
                    isDropShadowEnabled: !_isLightingEnabled);
                combatActorGameObject.transform.SetParent(_parent.transform, false);

                Vector3 position = GameBoxInterpreter.ToUnityPosition(positionIndex == 5 ?
                    _combatConfigFile.EnemyFormationConfig.CenterGameBoxPosition :
                    _combatConfigFile.ActorGameBoxPositions[5 + positionIndex]);

                combatActorGameObject.transform.SetPositionAndRotation(position,
                    meetType is MeetType.EnemyChasingPlayer or MeetType.RunningIntoEachOther
                        ? EnemyFormationRotation
                        : PlayerFormationRotation);
            }
        }
    }
}