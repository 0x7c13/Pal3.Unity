// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Core.DataReader.Cpk;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Data;
    using Dev;
    using MetaData;
    using Renderer;
    using UnityEngine;

    public abstract class SceneObject
    {
        public ScnObjectInfo Info;

        private readonly string _modelFilePath;

        protected SceneObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo, bool hasModel = true)
        {
            Info = objectInfo;

            _modelFilePath = hasModel && !string.IsNullOrEmpty(objectInfo.Name) ?
                GetModelFilePath(objectInfo, sceneInfo) : string.Empty;
        }

        private static string GetModelFilePath(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
        {
            var separator = CpkConstants.CpkDirectorySeparatorChar;
            var modelFilePath = string.Empty;

            if (objectInfo.Name.StartsWith('_'))
            {
                modelFilePath = $"{sceneInfo.CityName}.cpk{separator}" +
                                $"{sceneInfo.Model}{separator}{objectInfo.Name}";
            }
            else if (objectInfo.Name.StartsWith('+'))
            {
                // Special graphics effect.
            }
            else if (!objectInfo.Name.Contains('.'))
            {
                modelFilePath = $"{FileConstants.BaseDataCpkPathInfo.cpkName}{separator}item" +
                                $"{separator}{objectInfo.Name}{separator}{objectInfo.Name}.pol";
            }
            else
            {
                modelFilePath = $"{FileConstants.BaseDataCpkPathInfo.cpkName}{separator}object" +
                                $"{separator}{objectInfo.Name}";
            }

            return modelFilePath;
        }

        public virtual GameObject Activate(GameResourceProvider resourceProvider,
            Color tintColor)
        {
            var sceneGameObject = new GameObject($"Object_{Info.Id}_{Info.Type}");

            // Attach SceneObjectInfo to the GameObject for better debuggability
            #if UNITY_EDITOR
            var infoPresenter = sceneGameObject.AddComponent<SceneObjectInfoPresenter>();
            infoPresenter.sceneObjectInfo = Info;
            #endif

            if (!string.IsNullOrEmpty(_modelFilePath))
            {
                if (_modelFilePath.ToLower().EndsWith(".pol"))
                {
                    var poly = resourceProvider.GetPol(_modelFilePath);
                    var sceneObjectRenderer = sceneGameObject.AddComponent<PolyModelRenderer>();
                    sceneObjectRenderer.Render(poly.PolFile, poly.TextureProvider, tintColor, disableTransparency: true);
                }
                else if (_modelFilePath.ToLower().EndsWith(".cvd"))
                {
                    var cvd = resourceProvider.GetCvd(_modelFilePath);
                    var sceneObjectRenderer = sceneGameObject.AddComponent<CvdModelRenderer>();

                    var initTime = 0f;
                    // if (Info.Type == ScnSceneObjectType.Switch &&
                    //     Info.Parameters[0] == 1 &&
                    //     Info.Parameters[1] == 1)
                    // {
                    //     initTime = cvd.CvdFile.AnimationDuration;
                    // }

                    sceneObjectRenderer.Init(cvd.CvdFile, cvd.TextureProvider, tintColor, initTime);

                    if (Info.Type == ScnSceneObjectType.General)
                    {
                        sceneObjectRenderer.PlayAnimation();
                    }
                }
            }

            sceneGameObject.transform.position = GameBoxInterpreter.ToUnityPosition(Info.Position);
            sceneGameObject.transform.rotation =
                Quaternion.Euler(Info.XRotation, -Info.YRotation, 0f);

            return sceneGameObject;
        }

        public virtual bool IsInteractable(float distance)
        {
            return false;
        }

        public virtual void Interact()
        {
        }
    }
}