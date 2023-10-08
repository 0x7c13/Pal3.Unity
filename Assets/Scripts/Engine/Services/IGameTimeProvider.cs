// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Services
{
    /// <summary>
    /// Tracks the time since the game started and the delta time between frames.
    /// </summary>
    public interface IGameTimeProvider
    {
        public double TimeSinceStartup { get; }

        public double RealTimeSinceStartup { get; }

        public float DeltaTime { get; }
    }
}