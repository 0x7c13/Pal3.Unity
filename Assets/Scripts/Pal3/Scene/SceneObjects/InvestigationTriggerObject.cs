// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Core.Renderer;
    using Data;
    using MetaData;
    using State;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.Pushable)]
    [ScnSceneObject(ScnSceneObjectType.Switch)]
    [ScnSceneObject(ScnSceneObjectType.LiftingMechanism)]
    [ScnSceneObject(ScnSceneObjectType.VirtualInvestigationTrigger)]
    [ScnSceneObject(ScnSceneObjectType.InvestigationTriggerObject)]
    public class InvestigationTriggerObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 5f;

        public InvestigationTriggerObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            sceneGameObject.AddComponent<InvestigationTriggerController>().Init(this);
            return sceneGameObject;
        }

        public override bool IsInteractable(float distance, Vector2Int actorTilePosition)
        {
            return distance < MAX_INTERACTION_DISTANCE;
        }

        public override void Interact()
        {
            if (Info.Times != 0xFF)
            {
                if (Info.Times <= 0) return;
                else Info.Times--;
            }

            CommandDispatcher<ICommand>.Instance.Dispatch(
                new GameStateChangeRequest(GameState.Cutscene));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ScriptRunCommand((int)Info.ScriptId));
        }
    }

    internal class InvestigationTriggerController : MonoBehaviour
    {
        private InvestigationTriggerObject _investigationTriggerObject;
        private Bounds _bounds;

        public void Init(InvestigationTriggerObject investigationTriggerObject)
        {
            _investigationTriggerObject = investigationTriggerObject;

            // 2: 模型触发
            if (_investigationTriggerObject.Info.TriggerType == 2 &&
                GetComponentInChildren<StaticMeshRenderer>() is { } meshRenderer)
            {
                _bounds = meshRenderer.GetRendererBounds();
            }
            else
            {
                _bounds = investigationTriggerObject.Info.Bounds;
            }

            // Set active Y position of the lifting mechanism
            // TODO: impl of the lifting logic
            if (_investigationTriggerObject.Info.Type == ScnSceneObjectType.LiftingMechanism)
            {
                Vector3 oldPosition = transform.position;
                float gameBoxYPosition = _investigationTriggerObject.Info.Parameters[0];
                // A small Y offset to ensure actor shadow is properly rendered
                float activeYPosition = GameBoxInterpreter.ToUnityYPosition(gameBoxYPosition) - 0.02f;
                transform.position = new Vector3(oldPosition.x, activeYPosition, oldPosition.z);
            }
        }
    }
}