using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace  Pal3.Effect
{
    public class VisionRange : MonoBehaviour
    {
        [SerializeField] [Range(1, 20)] private float _radius = 12.0f;
        
        [SerializeField][Range(0,180)] 
        public float _angle = 70;

        private Transform _meshTransform = null;
        private Material _visionMaterial = null;
        private UnityEngine.Camera _visionCamera = null;
        
        void Start()
        {
            // @temp
            _radius = 12.0f;
            _angle = 70;
        
            transform.localPosition = new Vector3(0, 0.1f, 0);
            _meshTransform = transform.GetChild(0);
            //_meshTransform.forward = transform.forward;
            
            // Init Mesh Renderer
            var meshRenderer = _meshTransform.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = new Material(Shader.Find("Pal3/Effect/VisionRange"));
            _visionMaterial = meshRenderer.sharedMaterial;
            
            // Init Vision Camera
            _visionCamera = SetupDepthCamera(gameObject);
            _visionCamera.transform.forward = transform.forward;
        }
        protected UnityEngine.Camera SetupDepthCamera(GameObject rootGameObject)
        {
            var gameObject = new GameObject("[depth camera]");
            gameObject.transform.SetParent(rootGameObject.transform);
            gameObject.transform.localPosition = Vector3.zero;
            
            var camera = gameObject.AddComponent<UnityEngine.Camera>();
            camera.depthTextureMode = DepthTextureMode.Depth;
            camera.clearFlags = CameraClearFlags.Depth;
            
            /*
             * Near Plane & Far Plane influence Depth Texture value range,
             * it also influence limit max Radius 
             */
            //camera.nearClipPlane = 0.3f;
            //camera.farClipPlane = 100.0f;    // 
            // camera.farClipPlane = 1000.0f;

            RenderTexture rt = new RenderTexture(
                Screen.width,
                Screen.height,
                32,
                RenderTextureFormat.Depth);            
            camera.targetTexture = rt;
            
            return camera;
        }
        
        void Update()
        {
            SyncDepthCameraSettings();
            SyncFov();
            SyncRadius();
        }
        
        private void SyncDepthCameraSettings()
        {
            // camera depth texture
            _visionMaterial.SetTexture(Shader.PropertyToID("_DepthTex"),_visionCamera.targetTexture);
                
            // camera view matrix
            _visionMaterial.SetMatrix(
                Shader.PropertyToID("_depthCameraViewMatrix"),
                _visionCamera.worldToCameraMatrix);
                
            // camera projection matrix
            _visionMaterial.SetMatrix(
                Shader.PropertyToID("_depthCameraProjMatrix"),
                _visionCamera.projectionMatrix);
        }

        private void SyncFov()
        {
            // Sync fov
            float fovInDegree = _angle;
            float fovInRadian = Mathf.Deg2Rad * _angle;
            _visionMaterial.SetFloat(Shader.PropertyToID("_Angle"),fovInRadian);
            
            
            //float w = item.visionCamera.targetTexture.width;
            //float h = item.visionCamera.targetTexture.height;
            // here item.visionCamera.aspect = w/h
            float fovX = fovInDegree;
            float fovy = UnityEngine.Camera.HorizontalToVerticalFieldOfView(fovX, _visionCamera.aspect);
            _visionCamera.fieldOfView = fovy;   // camera default Fov Axis is Vertical
        }

        private void SyncRadius()
        {
            _meshTransform.localScale = new Vector3(_radius * 2.0f, _radius * 2.0f, 1);
        }
        
    }
    
}
