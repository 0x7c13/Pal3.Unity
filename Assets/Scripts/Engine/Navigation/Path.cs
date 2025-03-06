// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Navigation
{
    using System.Collections.Generic;
    using Pal3.Core.Contract.Enums;

    using Vector3 = UnityEngine.Vector3;

    public enum EndOfPathActionType
    {
        Idle = 0,
        DisposeSelf,
        WaitAndReverse
    }

    public sealed class Path
    {
        public MovementMode MovementMode { get; private set; }

        public EndOfPathActionType EndOfPathAction { get; private set; }

        public bool IgnoreObstacle { get; private set; }

        private readonly List<Vector3> _wayPoints = new ();

        private int _currentWayPointIndex;

        public void SetPath(Vector3[] wayPoints,
            MovementMode movementMode,
            EndOfPathActionType endOfPathAction,
            bool ignoreObstacle)
        {
            Clear();

            _wayPoints.AddRange(wayPoints);
            _currentWayPointIndex = 0;

            MovementMode = movementMode;
            EndOfPathAction = endOfPathAction;
            IgnoreObstacle = ignoreObstacle;
        }

        public void Clear()
        {
            MovementMode = 0;
            EndOfPathAction = EndOfPathActionType.Idle;
            _wayPoints.Clear();
            _currentWayPointIndex = 0;
        }

        public bool MoveToNextWayPoint()
        {
            return ++_currentWayPointIndex < _wayPoints.Count;
        }

        public bool IsEndOfPath()
        {
            return !(_wayPoints.Count > 0 && _currentWayPointIndex < _wayPoints.Count);
        }

        public Vector3[] GetAllWayPoints()
        {
            return _wayPoints.ToArray();
        }

        public Vector3 GetCurrentWayPoint()
        {
            return _wayPoints[_currentWayPointIndex];
        }
    }
}