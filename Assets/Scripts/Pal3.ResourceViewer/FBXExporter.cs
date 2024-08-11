using Autodesk.Fbx;
using Pal3.Core.DataReader.Mv3;
using Pal3.Core.Primitives;
using UnityEngine.SceneManagement;

namespace Pal3.ResourceViewer
{
    public class FBXExporter
    {
        public void ExportMv3File(Mv3File mv3File)
        {

            FbxManager fbxManager = FbxManager.Create();
            FbxScene fbxScene = FbxScene.Create(fbxManager, "MyScene");
            
            for (int meshIdx = 0;meshIdx < mv3File.Meshes.Length;meshIdx++)
            {
                FbxNode meshNode = FbxNode.Create(fbxScene,"MyMesh");
                FbxMesh mesh = FbxMesh.Create(fbxScene,"MyMeshGeometry");
                
                Mv3Mesh mv3Mesh = mv3File.Meshes[meshIdx];
                ExportMv3Mesh(mesh,mv3Mesh);
                //ExportMv3Anim(fbxManager,fbxScene, mesh, mv3Mesh);
                ExportMv3AnimV2(fbxManager,fbxScene, mesh, mv3Mesh);

                meshNode.SetNodeAttribute(mesh);
                fbxScene.GetRootNode().AddChild(meshNode);
            }


            // @miao @todo
            var exporter = Autodesk.Fbx.FbxExporter.Create(fbxManager,"");
            var settings = FbxIOSettings.Create(fbxManager, "test");
            settings.SetBoolProp("Shape",true);
            settings.SetBoolProp("ShapeAttributes",true);
            settings.SetBoolProp("ShapeAttributesValues",true);
            settings.SetBoolProp("ShapeAnimation",true);
            
            
            
            
            //exporter.Initialize("ayy_test.fbx", -1, fbxManager.GetIOSettings());
            exporter.Initialize("ayy_test.fbx", -1, settings);

            exporter.Export(fbxScene);
        }

        private void ExportMv3Mesh(FbxMesh destMesh, Mv3Mesh mv3Mesh)
        {
            // vertices
            int vertexCount = mv3Mesh.KeyFrames[0].GameBoxVertices.Length;
            
            destMesh.InitControlPoints(vertexCount);    // position
            FbxLayerElementUV uvElement = destMesh.CreateElementUV("MyUVs");  // uv
            FbxLayerElementNormal normalElement = destMesh.CreateElementNormal(); // normal
            
            for (int vertIdx = 0;vertIdx < vertexCount;vertIdx++)
            {
                // vert position
                GameBoxVector3 attPos = mv3Mesh.KeyFrames[0].GameBoxVertices[vertIdx];
                destMesh.SetControlPointAt(new FbxVector4(attPos.X,attPos.Y,attPos.Z,1),vertIdx);    
                
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
        
        private void ExportMv3Anim(FbxManager fbxManager,FbxScene fbxScene,FbxMesh fbxMesh,Mv3Mesh mv3Mesh)
        {
            FbxBlendShapeChannel blendShapeChannel = FbxBlendShapeChannel.Create(fbxScene,"MyBlendShapeChannel");
            FbxBlendShape blendShape = FbxBlendShape.Create(fbxScene,"MyBlendShape");
            blendShape.AddBlendShapeChannel(blendShapeChannel);
            fbxMesh.AddDeformer(blendShape);

            int frameCount = mv3Mesh.KeyFrames.Length;
            int vertexCount = mv3Mesh.KeyFrames[0].GameBoxVertices.Length;
            for (int frameIndex = 0;frameIndex < frameCount;frameIndex++)
            {
                FbxShape shape = FbxShape.Create(fbxScene,"MyShape_" + frameIndex);
                for (int vertIdx = 0;vertIdx < vertexCount;vertIdx++) 
                {
                    GameBoxVector3 pos = mv3Mesh.KeyFrames[frameIndex].GameBoxVertices[vertIdx];
                    shape.SetControlPointAt(new FbxVector4(pos.X,pos.Y,pos.Z,1.0),vertIdx);
                }
                blendShapeChannel.AddTargetShape(shape);
            }


        }


        private void ExportMv3AnimV2(FbxManager fbxManager, FbxScene fbxScene, FbxMesh fbxMesh, Mv3Mesh mv3Mesh)
        {
            //FbxBlendShape blendShape = FbxBlendShape.Create(fbxScene, "MyBlendShape");
            //fbxMesh.AddDeformer(blendShape);

            // var deformer = FbxDeformer.Create(fbxManager, "test11");
            // fbxMesh.AddDeformer(deformer);

            
            FbxBlendShape deformer = fbxMesh.GetBlendShapeDeformer(0);
            if (deformer == null)
            {
                deformer = FbxBlendShape.Create(fbxMesh, "MyDefoner");
            }

            FbxBlendShapeChannel channel = FbxBlendShapeChannel.Create(deformer,"MyChannel");
            // channel.AddTargetShape()


            deformer.AddBlendShapeChannel(channel);
        }

    }
}