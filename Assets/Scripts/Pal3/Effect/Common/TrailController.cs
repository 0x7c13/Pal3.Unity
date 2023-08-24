using System;
using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace Pal3.Effect
{
    public class TrailController : MonoBehaviour
    {
        private LineRenderer _lineRenderer = null;
        private int _maxPoint = 50;
        private Vector3 _posOffset = new Vector3(0, 3, 0);
        private float _defaultLifeSpan = 3.0f;
        class TrailPoint
        {
            public Vector3 point;
            public float lifeSpan;
        }
        
        private List<TrailPoint> _allPoints = new List<TrailPoint>();
        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            
        }

        public void SetPointLifeSpan(float lifeSpan)
        {
            _defaultLifeSpan = lifeSpan;
        }

        void Update()
        {
            gameObject.transform.localPosition = _posOffset;
            
            ReduceLifeSpan(Time.deltaTime);
            
            var curPos = gameObject.transform.position;

            bool bShouldAddPoint = false;
            if (_allPoints.Count == 0)
            {
                bShouldAddPoint = true;
            }
            else if (!CheckNear(_allPoints[_allPoints.Count - 1].point, curPos))
            {
                bShouldAddPoint = true;
            }
            
            if(bShouldAddPoint)
            {
                AddPoint(curPos);
            }
            RefreshLineRenderer();
        }

        private void ReduceLifeSpan(float deltaTime)
        {
            if (_allPoints.Count == 0)
                return;
            
            foreach (var trailPoint in _allPoints)
            {
                trailPoint.lifeSpan -= deltaTime;
            }
            if (_allPoints[0].lifeSpan <= 0f)
            {
                _allPoints.RemoveAt(0);
            }
        }

        private bool CheckNear(Vector3 p1,Vector3 p2)
        {
            return Vector3.Distance(p1, p2) <= Mathf.Epsilon;
        }

        private void AddPoint(Vector3 point)
        {
            var trailPoint = new TrailPoint();
            trailPoint.point = point;
            trailPoint.lifeSpan = _defaultLifeSpan;
            _allPoints.Add(trailPoint);
        }

        private void RefreshLineRenderer()
        {
            Vector3[] allPos = new Vector3[_allPoints.Count];
            for(int i = 0;i < _allPoints.Count;i++)
            {
                allPos[i] = _allPoints[i].point;
            }
            
            _lineRenderer.positionCount = _allPoints.Count;
            _lineRenderer.SetPositions(allPos);
        }
    }

}

