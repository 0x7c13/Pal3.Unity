// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Command.Extensions;
    using Common;
    using Core.Command;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Core.Utilities;
    using Data;
    using Engine.Core.Abstraction;

    using Color = Core.Primitives.Color;

    [ScnSceneObject(SceneObjectType.SceneSfx)]
    public sealed class SceneSfxObject : SceneObject
    {
        public string SfxName { get; }

        private const string SCENE_SFX_AUDIO_SOURCE_NAME = nameof(SceneSfxObject);
        private const float SCENE_SFX_VOLUME = 0.4f;

        public SceneSfxObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo, hasModel: false)
        {
            SfxName = objectInfo.Name;
        }

        public override IGameEntity Activate(GameResourceProvider gameResourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();

            IGameEntity sceneObjectGameEntity = base.Activate(gameResourceProvider, tintColor);

            // We want some random delay before playing the scene sfx
            // since there might be more than one audio source in the current scene
            // playing the exact same audio sfx, which could potentially cause unwanted
            // "Comb filter" effect.
            float startDelay = RandomGenerator.Range(0f, 1f);

            float interval = ObjectInfo.Parameters[0] > 0 ? ObjectInfo.Parameters[0] / 1000f : 0f;

            Pal3.Instance.Execute(new AttachSfxToGameEntityRequest(sceneObjectGameEntity,
                    SfxName,
                    SCENE_SFX_AUDIO_SOURCE_NAME,
                    loopCount: -1,
                    SCENE_SFX_VOLUME,
                    startDelay,
                    interval));

            return sceneObjectGameEntity;
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => false;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            yield break;
        }
    }
}