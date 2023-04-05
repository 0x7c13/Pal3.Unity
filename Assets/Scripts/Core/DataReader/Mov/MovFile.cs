// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Mov
{
    using GameBox;
    using UnityEngine;
    
    public class MovFile
    {
        public MovHeader header;

        public int nBoneTrack;
        public BoneActTrack[] boneTrackArray = null;
        
        public int nDuration;

        public int nEvent;
        public ActionEvent[] actionEventArray = null;
    }

    public class MovHeader
    {
        public string Magic;
        public int Version;
        public float Duration;
        public int TrackNum;
        public int VertNum;
        public int EventNum;
    }
    
    public class BoneActTrack
    {
        public int boneId;
        public string boneName; // len = 64
        public int animFlags;
        public int nKey;
        public RigidKey[] keyArray = null;
    }

    public class ActionEvent
    {
        public int time;
        public string name; // len = 16
        public uint name32; // crc32
    }

    public class RigidKey
    {
        public int time;
        public Vector3 trans;
        public Quaternion rot;
    }

}
