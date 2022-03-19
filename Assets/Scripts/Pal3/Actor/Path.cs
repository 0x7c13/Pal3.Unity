// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Actor
{
    using System.Collections.Generic;
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

        private readonly List<Vector3> _wayPoints = new ();

        private int _currentWayPointIndex;

        public void SetPath(Vector3[] wayPoints,
            int movementMode,
            EndOfPathActionType endOfPathAction)
        {
            Clear();

            _wayPoints.AddRange(wayPoints);
            _currentWayPointIndex = 0;

            MovementMode = movementMode;
            EndOfPathAction = endOfPathAction;
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

        public List<Vector3> GetAllWayPoints()
        {
            return _wayPoints;
        }

        public Vector3 GetCurrentWayPoint()
        {
            return _wayPoints[_currentWayPointIndex];
        }
    }
}