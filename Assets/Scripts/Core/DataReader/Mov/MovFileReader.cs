// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#define USE_UNSAFE_BINARY_READER

using UnityEngine.Assertions;
using UnityEngine.PlayerLoop;

namespace Core.DataReader.Mov
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Extensions;
    using GameBox;
    using UnityEngine;
    using Utils;

    public static class MovFileReader
    {
        public static MovFile Read(byte[] data, int codepage)
        {
#if USE_UNSAFE_BINARY_READER
            using var reader = new UnsafeBinaryReader(data);
#else
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);
#endif
            MovFile result = new MovFile();
            ReadHeader(reader,codepage,result);
            ReadEvent(reader,codepage,result);
            ReadBoneTrack(reader,codepage,result);
            result.nDuration = ComputeDurationWithTracks(result);
            
            return result;
        }

        private static void ReadHeader(UnsafeBinaryReader reader, int codepage,MovFile mov)
        {
            var h = reader.ReadChars(4);
            var headerStr = new string(h[..^1]);   // headStr === "anm\0"
            var version = reader.ReadInt32();   // version === 100
            
            MovHeader header = new MovHeader();
            header.Magic = headerStr;
            header.Version = version;
            header.Duration = reader.ReadSingle();
            header.TrackNum = reader.ReadInt32();
            header.VertNum = reader.ReadInt32();
            header.EventNum = reader.ReadInt32();

            mov.header = header;
        }

        private static void ReadEvent(UnsafeBinaryReader reader, int codepage, MovFile mov)
        {
			mov.nEvent = mov.header.EventNum;
			Debug.Assert(mov.nEvent == 0);  // @miao @test // seems pal3 all event num == 0
			if (mov.nEvent > 0)
			{
				mov.actionEventArray = new ActionEvent[mov.nEvent];
				for (int i = 0 ;i < mov.nEvent;i++)
				{
						var actionEvent = new ActionEvent();
						
						float ftime = reader.ReadSingle();
						actionEvent.time = GameBoxInterpreter.SecondToTick(ftime);

						var t = reader.ReadChars(16);
						actionEvent.name = new string(t);
						// @todo, here should crc string 2 int 
						
						mov.actionEventArray[i] = actionEvent;
				}
			}
        }
        
          
#if USE_UNSAFE_BINARY_READER
        private static void ReadBoneTrack(UnsafeBinaryReader reader, int codepage, MovFile mov)
#endif
        {
			mov.nBoneTrack = mov.header.TrackNum;
			if (mov.nBoneTrack > 0)
			{
				mov.boneTrackArray = new BoneActTrack[mov.nBoneTrack];
				for (int i = 0;i < mov.nBoneTrack;i++)
				{
					mov.boneTrackArray[i] = ReadOneBoneTrack(reader, codepage);
				}
			}
        }

        private static BoneActTrack ReadOneBoneTrack(UnsafeBinaryReader reader, int codepage)
        {
			BoneActTrack track = new BoneActTrack();
			track.boneId = reader.ReadInt32();

			int nameLen = reader.ReadInt32();
			var tmp = reader.ReadChars(nameLen);
			track.boneName = new string(tmp[..^1]);

			track.nKey = reader.ReadInt32();
			track.animFlags = reader.ReadInt32();

			if (track.nKey > 0)
			{
				track.keyArray = new RigidKey[track.nKey];
				for (int i = 0;i < track.nKey;i++)
				{
					var key = new RigidKey();
					
					float keytime = reader.ReadSingle();
					key.time = GameBoxInterpreter.SecondToTick(keytime);
					key.trans = GameBoxInterpreter.ToUnityPosition(reader.ReadVector3());
					key.rot = GameBoxInterpreter.MshQuaternionToUnityQuaternion(new GameBoxQuaternion()
		            {
		                X = reader.ReadSingle(),
		                Y = reader.ReadSingle(),
		                Z = reader.ReadSingle(),
		                W = reader.ReadSingle(),
		            });
					
					// read scale[3x3] float for placeholder
					reader.ReadSingle();reader.ReadSingle();reader.ReadSingle();
					reader.ReadSingle();reader.ReadSingle();reader.ReadSingle();
					reader.ReadSingle();reader.ReadSingle();reader.ReadSingle();
					
					track.keyArray[i] = key;
				}
			}
			return track;
        }

		// get the longest max key num of bone track
        private static int ComputeDurationWithTracks(MovFile mov)
        {
	        int duration = 0;
	        for (int i = 0;i < mov.nBoneTrack;i++)
	        {
		        int nkey = mov.boneTrackArray[i].nKey;
		        Debug.Assert(nkey > 0);
		        int tmp = mov.boneTrackArray[i].keyArray[nkey - 1].time;
		        if (tmp > duration)
			        duration = tmp;
	        }

	        return duration;
        }
    }
}