// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Navigation
{
    using UnityEngine;

    public enum EndOfPathActionType
    {
        Idle = 0,
        DisposeSelf,
        WaitAndReverse
    }

    public class Path
    {
        public MovementMode MovementMode { get; private set; }

        public EndOfPathActionType EndOfPathAction { get; private set; }

        public bool IgnoreObstacle { get; private set; }

        private Vector3[] _wayPoints = {};

        private int _currentWayPointIndex;

        public void SetPath(Vector3[] wayPoints,
            MovementMode movementMode,
            EndOfPathActionType endOfPathAction,
            bool ignoreObstacle)
        {
            Clear();

            _wayPoints = wayPoints;
            _currentWayPointIndex = 0;

            MovementMode = movementMode;
            EndOfPathAction = endOfPathAction;
            IgnoreObstacle = ignoreObstacle;
        }

        public void Clear()
        {
            MovementMode = 0;
            EndOfPathAction = EndOfPathActionType.Idle;
            _wayPoints = new Vector3[] {};
            _currentWayPointIndex = 0;
        }

        public bool MoveToNextWayPoint()
        {
            return ++_currentWayPointIndex < _wayPoints.Length;
        }

        public bool IsEndOfPath()
        {
            return !(_wayPoints.Length > 0 && _currentWayPointIndex < _wayPoints.Length);
        }

        public Vector3[] GetAllWayPoints()
        {
            return _wayPoints;
        }

        public Vector3 GetCurrentWayPoint()
        {
            return _wayPoints[_currentWayPointIndex];
        }
    }
}