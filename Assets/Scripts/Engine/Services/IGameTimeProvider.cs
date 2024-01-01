// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Services
{
        /// <summary>
        /// Interface for providing game time information.
        /// </summary>
        public interface IGameTimeProvider
        {
            /// <summary>
            /// The time since the game started, in seconds.
            /// </summary>
            public double TimeSinceStartup { get; }

            /// <summary>
            /// The real time since the game started, in seconds.
            /// </summary>
            public double RealTimeSinceStartup { get; }

            /// <summary>
            /// The time in seconds it took to complete the last frame.
            /// </summary>
            public float DeltaTime { get; }
        }
}