// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.InternalCommands
{
    [AvailableInConsole]
    public class SceneSaveGlobalObjectLayerIndexCommand : ICommand
    {
        public SceneSaveGlobalObjectLayerIndexCommand(string cityName,
            string sceneName,
            int objectId,
            byte layerIndex)
        {
            CityName = cityName;
            SceneName = sceneName;
            ObjectId = objectId;
            LayerIndex = layerIndex;
        }

        public string CityName { get; }
        public string SceneName { get; }
        public int ObjectId { get; }
        public byte LayerIndex { get; }
    }
}