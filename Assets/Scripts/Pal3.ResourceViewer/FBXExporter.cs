using Autodesk.Fbx;
using Engine.Extensions;
using Pal3.Core.DataReader.Mv3;
using Pal3.Core.Primitives;
using UnityEngine;

namespace Pal3.ResourceViewer
{
    public class FBXExporter
    {
        public void ExportMv3File(Mv3File mv3File,string exportFilePath)
        {

            FbxManager fbxManager = FbxManager.Create();
            FbxScene fbxScene = FbxScene.Create(fbxManager, "MyScene");
            
            for (int meshIdx = 0;meshIdx < mv3File.Meshes.Length;meshIdx++)
            {
                Mv3Mesh mv3Mesh = mv3File.Meshes[meshIdx];
                int frameCnt = mv3Mesh.KeyFrames.Length;
                
                
                int frameIdx = 0;
                FbxNode meshNode = FbxNode.Create(fbxScene,"mesh_at_frame_" + frameIdx);
                FbxMesh fbxMesh = FbxMesh.Create(fbxScene,"MyMeshGeometry");
                
                ExportMv3Mesh(fbxManager,fbxMesh,mv3Mesh,frameIdx);
                
                meshNode.SetNodeAttribute(fbxMesh);
                fbxScene.GetRootNode().AddChild(meshNode);


                // Set BlendShape & Shape keys 
                int vertexCount = mv3Mesh.KeyFrames[0].GameBoxVertices.Length; 
                FbxBlendShape blendShape = FbxBlendShape.Create(fbxManager,"bs1");
                for (frameIdx = 1;frameIdx < frameCnt;frameIdx++)
                {
                    FbxBlendShapeChannel channel = FbxBlendShapeChannel.Create(fbxManager, "shapeKeyChannel_" + frameIdx);
                    FbxShape shape = FbxShape.Create(fbxManager,"frame_" + frameIdx);
                    shape.InitControlPoints(vertexCount);
                    for (int vertIdx = 0; vertIdx < vertexCount; vertIdx++)
                    {
                        GameBoxVector3 pt = mv3Mesh.KeyFrames[frameIdx].GameBoxVertices[vertIdx];
                        Vector3 unityAttPos = pt.ToUnityPosition();
                        shape.SetControlPointAt(new FbxVector4(unityAttPos.x,unityAttPos.y,unityAttPos.z,1),vertIdx);
                    }
                    channel.AddTargetShape(shape);
                    blendShape.AddBlendShapeChannel(channel);
                }
                fbxMesh.AddDeformer(blendShape);
            }
            
            var exporter = Autodesk.Fbx.FbxExporter.Create(fbxManager,"test111");
            //exporter.Initialize("../export_fbx/ayy_test.fbx", -1, fbxManager.GetIOSettings());
            exporter.Initialize(exportFilePath, -1, fbxManager.GetIOSettings());
            exporter.Export(fbxScene);
        }

        private void ExportMv3Mesh(FbxManager manager,FbxMesh destMesh, Mv3Mesh mv3Mesh,int frameIndex)
        {
            // vertices
            int vertexCount = mv3Mesh.KeyFrames[0].GameBoxVertices.Length;
            
            destMesh.InitControlPoints(vertexCount);    // position
            FbxLayerElementUV uvElement = destMesh.CreateElementUV("MyUVs");  // uv
            FbxLayerElementNormal normalElement = destMesh.CreateElementNormal(); // normal
            
            for (int vertIdx = 0;vertIdx < vertexCount;vertIdx++)
            {
                // vert position
                GameBoxVector3 attPos = mv3Mesh.KeyFrames[frameIndex].GameBoxVertices[vertIdx];
                Vector3 unityAttPos = attPos.ToUnityPosition();
                destMesh.SetControlPointAt(new FbxVector4(unityAttPos.x,unityAttPos.y,unityAttPos.z,1),vertIdx);    
                
                // vert UV
                GameBoxVector2 attUV = mv3Mesh.Uvs[vertIdx];
                uvElement.GetDirectArray().SetAt(vertIdx,new FbxVector2(attUV.X,attUV.Y));
                
                // normal
                GameBoxVector3 attNormal = mv3Mesh.GameBoxNormals[vertIdx];
                normalElement.GetDirectArray().SetAt(vertIdx,new FbxVector4(attNormal.X,attNormal.Y,attNormal.Z,0.0));
            }
            
            // Faces
            int faceCount = mv3Mesh.GameBoxTriangles.Length / 3;
            for (int faceIdx = 0;faceIdx < faceCount;faceIdx++)
            {
                int vertIdx = mv3Mesh.GameBoxTriangles[faceIdx * 3];
                destMesh.BeginPolygon();
                destMesh.AddPolygon(vertIdx);
                destMesh.AddPolygon(vertIdx + 1);
                destMesh.AddPolygon(vertIdx + 2);
                destMesh.EndPolygon();
            }
        }
        
     

    }
}