// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.GameSystems.Combat.Scene
{
    using System.Collections.Generic;
    using Actor;
    using Actor.Controllers;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Cpk;
    using Core.DataReader.Gdb;
    using Core.DataReader.Ini;
    using Core.DataReader.Pol;
    using Data;
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
    using Engine.Extensions;
    using Engine.Services;
    using Settings;
    using Rendering.Toon;
    using Rendering.Material;
    using Rendering.Renderer;

    using Vector3 = UnityEngine.Vector3;
    using Quaternion = UnityEngine.Quaternion;

    public sealed class CombatScene : GameEntityScript
    {
        private const string COMBAT_CONFIG_FILE_NAME = "combat.ini";

        private GameResourceProvider _resourceProvider;
        private GameSettings _lightingSettings;
        private UnityEngine.Light _keyLight;
        private IMaterialManager _materialManager;

        private string _combatSceneName;
        private bool _usesToonRendering;

        private IGameEntity _parent;
        private IGameEntity _mesh;

        private CombatConfigFile _combatConfigFile;

        private static readonly Quaternion EnemyFormationRotation = Quaternion.Euler(0, 145, 0);
        private static readonly Quaternion PlayerFormationRotation = Quaternion.Euler(0, -45, 0);

        private Dictionary<ElementPosition, CombatActorController> _combatActorControllers;

        public void Init(GameResourceProvider resourceProvider)
        {
            _resourceProvider = resourceProvider;
            _materialManager = resourceProvider.GetMaterialManager();

            _combatConfigFile = _resourceProvider.GetGameResourceFile<CombatConfigFile>(
                FileConstants.DataScriptFolderVirtualPath + COMBAT_CONFIG_FILE_NAME);
        }

        public void Load(IGameEntity parent,
            string combatSceneName,
            bool usesToonRendering)
        {
            _parent = parent;
            _combatSceneName = combatSceneName;
            _usesToonRendering = usesToonRendering;
            if (usesToonRendering)
            {
                _lightingSettings = ServiceLocator.Instance.Get<GameSettings>();
                _keyLight = ToonLighting.CreateKey(transform, Quaternion.Euler(58f, -30f, 0f),
                    new UnityEngine.Color(1f, 0.95f, 0.9f), 1f);
                ToonLighting.ConfigureKeyShadows(_keyLight, _lightingSettings);
                _lightingSettings.OnGameSettingsChanged += OnLightingSettingsChanged;
            }

            string meshFileRelativeFolderPath = FileConstants.CombatSceneFolderVirtualPath + combatSceneName;

            ITextureResourceProvider sceneTextureProvider = _resourceProvider.CreateTextureResourceProvider(
                meshFileRelativeFolderPath);

            PolFile polFile = _resourceProvider.GetGameResourceFile<PolFile>(meshFileRelativeFolderPath +
                CpkConstants.DirectorySeparatorChar + combatSceneName.ToLower() + ".pol");

            RenderMesh(polFile, sceneTextureProvider);
        }

        private void OnLightingSettingsChanged(string propertyName)
        {
            if (_keyLight != null) ToonLighting.ConfigureKeyShadows(_keyLight, _lightingSettings);
        }

        protected override void OnDisableGameEntity()
        {
            if (_lightingSettings != null) _lightingSettings.OnGameSettingsChanged -= OnLightingSettingsChanged;
        }

        private void RenderMesh(PolFile polFile, ITextureResourceProvider textureProvider)
        {
            // Render mesh
            _mesh = GameEntityFactory.Create($"Mesh_{_combatSceneName}",
                _parent, worldPositionStays: false);
            _mesh.IsStatic = true; // Combat Scene mesh is static

            PolyModelRenderer polyMeshRenderer = _mesh.AddComponent<PolyModelRenderer>();
            polyMeshRenderer.Render(polFile,
                textureProvider,
                _materialManager,
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
            return _combatActorControllers.GetValueOrDefault(elementPosition);
        }

        public void LoadActors(Dictionary<ElementPosition, CombatActorInfo> combatActors,
            MeetType meetType)
        {
            _combatActorControllers = new Dictionary<ElementPosition, CombatActorController>();

            foreach ((ElementPosition elementPosition, CombatActorInfo actorInfo) in combatActors)
            {
                CombatActorInfo combatActorInfo = actorInfo;

                if (combatActorInfo.Type == CombatActorType.MainActor &&
                    (string.IsNullOrEmpty(combatActorInfo.ModelId) || combatActorInfo.ModelId == "Null"))
                {
                    // Player actor's model id is not set in the file but it is equal
                    // to its id in string format
                    combatActorInfo.ModelId = combatActorInfo.Id.ToString();
                }

                CombatActor combatActor = new(_resourceProvider, combatActorInfo);
                IGameEntity combatActorGameEntity = CombatActorFactory.CreateCombatActorGameEntity(
                    _resourceProvider,
                    combatActor,
                    elementPosition,
                    isDropShadowEnabled: !_usesToonRendering);
                combatActorGameEntity.SetParent(_parent, worldPositionStays: false);

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

                combatActorGameEntity.Transform.SetPositionAndRotation(GetWorldPosition(elementPosition), rotation);

                _combatActorControllers[elementPosition] = combatActorGameEntity.GetComponent<CombatActorController>();
            }
        }
    }
}