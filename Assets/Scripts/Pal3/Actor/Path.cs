// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Actor
{
    using System;
    using UnityEngine;

    public enum EndOfPathActionType
    {
        Idle = 0,
        DisposeSelf,
        Reverse
    }

    public class Path
    {
        public int MovementMode { get; private set; }

        public EndOfPathActionType EndOfPathAction { get; private set; }

        private Vector3[] _wayPoints;

        private int _currentWayPointIndex;

        public Path(Vector3[] wayPoints,
            int movementMode,
            EndOfPathActionType endOfPathAction)
        {
            _wayPoints = wayPoints;
            _currentWayPointIndex = 0;

            MovementMode = movementMode;
            EndOfPathAction = endOfPathAction;
        }

        public void Clear()
        {
            MovementMode = 0;
            EndOfPathAction = EndOfPathActionType.Idle;
            _wayPoints = Array.Empty<Vector3>();
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