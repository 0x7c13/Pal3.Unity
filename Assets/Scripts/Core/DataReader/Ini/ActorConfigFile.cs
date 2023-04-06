// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Ini
{
    public struct BoneActor
    {
        public string MeshFileName;
        public string MaterialFileName;
        public string EffectFileName;
    }

    public struct ActorAction
    {
        public string ActionName;
        public string ActionFileName;
    }

    public struct BoneMaterial
    {
        public string MaterialName;
        public string MaterialFileName;
        public string EffectFileName;
    }

    public abstract class ActorActionConfig
    {
        protected ActorActionConfig(ActorAction[] actorActions)
        {
            ActorActions = actorActions;
        }

        public ActorAction[] ActorActions { get; }
    }

    public sealed class Mv3ActionConfig : ActorActionConfig
    {
        public Mv3ActionConfig(ActorAction[] actorActions) : base(actorActions)
        {
        }
    }

    public sealed class MovActionConfig : ActorActionConfig
    {
        public MovActionConfig(BoneActor actor,
            ActorAction[] actorActions,
            BoneMaterial[] boneMaterials) : base(actorActions)
        {
            Actor = actor;
            BoneMaterials = boneMaterials;
        }

        public BoneActor Actor { get; }
        public BoneMaterial[] BoneMaterials { get; }
    }
}