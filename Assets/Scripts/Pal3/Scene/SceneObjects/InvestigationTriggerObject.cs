// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Common;
    using Core.DataReader.Scn;
    using Core.Renderer;
    using Data;
    using MetaData;
    using State;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.Pushable)]
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
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            sceneGameObject.AddComponent<InvestigationTriggerController>().Init(this);
            return sceneGameObject;
        }

        public override bool IsInteractable(InteractionContext ctx)
        {
            return Activated && ctx.DistanceToActor < MAX_INTERACTION_DISTANCE;
        }

        public override void Interact(bool triggerredByPlayer)
        {
            if (!IsInteractableBasedOnTimesCount()) return;

            if (triggerredByPlayer)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new GameStateChangeRequest(GameState.Cutscene));
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
            }

            ExecuteScriptIfAny();
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
            if (_investigationTriggerObject.ObjectInfo.TriggerType == 2 &&
                GetComponentInChildren<StaticMeshRenderer>() is { } meshRenderer)
            {
                _bounds = meshRenderer.GetRendererBounds();
            }
            else
            {
                _bounds = investigationTriggerObject.ObjectInfo.Bounds;
            }
        }
    }
}