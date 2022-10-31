// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace ResourceViewer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Core.DataLoader;
    using Core.DataReader.Cpk;
    using Core.DataReader.Cvd;
    using Core.DataReader.Mv3;
    using Core.DataReader.Pol;
    using Core.DataReader.Sce;
    using Core.FileSystem;
    using Core.Services;
    using Core.Utils;
    using IngameDebugConsole;
    using Newtonsoft.Json;
    using Pal3.Command;
    using Pal3.Data;
    using Pal3.MetaData;
    using Pal3.Renderer;
    using Pal3.Script;
    using TMPro;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UI;
    using Random = System.Random;


    public class WaveFrontObjHelper
    {
        public static void Write(String folderName,String prefixName,Mv3ModelRenderer modelRenderer,bool bCarryNormalInfo, bool bCarryUVInfo)
        {
            RenderMeshComponent[] meshComps = modelRenderer.GetRenderMeshComps();
            
            /*
             * obj file format reference here
             * https://people.computing.clemson.edu/~dhouse/courses/405/docs/brief-obj-file-format.html
             */
            for (int subMeshIndex = 0;subMeshIndex < meshComps.Length;subMeshIndex++)
            {
                using (StreamWriter w = new StreamWriter(folderName + prefixName + "_"+ subMeshIndex + ".obj"))
                {
                    RenderMeshComponent rmc = meshComps[subMeshIndex];
                    Mesh mesh = rmc.Mesh;
                    
                    // write title
                    w.WriteLine("# ayy test");
                    
                    // write vertices
                    for (int i = 0;i < mesh.vertices.Length;i++)
                    {
                        w.WriteLine("v " + mesh.vertices[i].x + " " + mesh.vertices[i].y + " " + mesh.vertices[i].z);    
                    }
                    
                    // write normals
                    if (bCarryNormalInfo)
                    {
                        for (int i = 0;i < mesh.vertices.Length;i++)
                        {
                            w.WriteLine("vn " + mesh.normals[i].x + " " + mesh.normals[i].y + " " + mesh.normals[i].z);    
                        }    
                    }
                    
                    // write uvs
                    if (bCarryUVInfo)
                    {
                        for (int i = 0;i < mesh.uv.Length;i++)
                        {
                            w.WriteLine("vt " + mesh.uv[i].x + " " + mesh.uv[i].y);
                        }    
                    }
                    
                    // write faces
                    for (int faceIndex = 0;faceIndex < mesh.triangles.Length / 3;faceIndex++)
                    {
                        int v1 = mesh.triangles[faceIndex * 3 + 0] + 1;
                        int v2 = mesh.triangles[faceIndex * 3 + 1] + 1;
                        int v3 = mesh.triangles[faceIndex * 3 + 2] + 1;
                        
                        w.WriteLine("f " + v1 + " " + v2 + " " + v3);
                    }

                    // simple obj file test
                    // w.WriteLine("v 0.0 0.0 0.0");
                    // w.WriteLine("v 0.0 1.0 0.0");
                    // w.WriteLine("v 1.0 0.0 0.0");
                    // w.WriteLine("f 1 2 3");
                }                    
            }
            

        }
    }
}