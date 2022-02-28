// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Ini
{
    public struct ActorAction
    {
        public string ActionName;
        public string ActionFileName;
    }

    public class ActorConfigFile
    {
        public ActorConfigFile(ActorAction[] actorActions)
        {
            ActorActions = actorActions;
        }

        public ActorAction[] ActorActions { get; }
    }
}