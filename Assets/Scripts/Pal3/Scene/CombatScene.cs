// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene
{
    using Core.DataLoader;
    using Core.DataReader.Cpk;
    using Core.DataReader.Pol;
    using Data;
    using MetaData;
    using Rendering.Material;
    using Rendering.Renderer;
    using UnityEngine;

    public sealed class CombatScene : MonoBehaviour
    {
        private GameResourceProvider _resourceProvider;
        private IMaterialFactory _materialFactory;

        private string _combatSceneName;

        private GameObject _parent;
        private GameObject _mesh;
        private Light _mainLight;

        private (PolFile PolFile, ITextureResourceProvider TextureProvider) _scenePolyMesh;

        public void Init(GameResourceProvider resourceProvider)
        {
            _resourceProvider = resourceProvider;
            _materialFactory = resourceProvider.GetMaterialFactory();
        }

        public void Load(GameObject parent,
            string combatSceneName)
        {
            _parent = parent;
            _combatSceneName = combatSceneName;

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
                isStaticObject: true, // Scene mesh is static
                Color.white);
        }
    }
}