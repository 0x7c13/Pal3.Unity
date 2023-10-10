// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Services
{
    using System;

    /// <summary>
    /// Tracks the time since the game started and the delta time between frames.
    /// </summary>
    public sealed class GameTimeProvider : IGameTimeProvider
    {
        public static GameTimeProvider Instance
        {
            get { return _instance ??= new GameTimeProvider(); }
        }

        private static GameTimeProvider _instance; // Singleton

        private GameTimeProvider() { } // Hide constructor, use Instance instead.

        public double TimeSinceStartup { get; private set; }

        public double RealTimeSinceStartup => DateTime.UtcNow.Subtract(_startTimeUtc).TotalSeconds;

        public float DeltaTime { get; private set; }

        private readonly DateTime _startTimeUtc = DateTime.UtcNow;

        public void Tick(float deltaTime)
        {
            DeltaTime = deltaTime;
            TimeSinceStartup += deltaTime;
        }
    }
}