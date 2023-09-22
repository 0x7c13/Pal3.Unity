namespace Engine.Utilities
{
    using UnityEngine;
    using UnityEngine.Rendering;

    public class GridOverlay : MonoBehaviour
    {
        public bool showMain = true;
        public bool showSub = false;

        public int gridSizeX;
        public int gridSizeY;
        public int gridSizeZ;

        public float smallStep;
        public float largeStep;

        public float startX;
        public float startY;
        public float startZ;

        private Material _lineMaterial;

        public Color mainColor = new Color(0f, 1f, 0f, 1f);
        public Color subColor = new Color(0f, 0.5f, 0f, 1f);

        void CreateLineMaterial()
        {
            if (!_lineMaterial)
            {
                // Unity has a built-in shader that is useful for drawing
                // simple colored things.
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                _lineMaterial = new Material(shader);
                _lineMaterial.hideFlags = HideFlags.HideAndDontSave;
                // Turn on alpha blending
                _lineMaterial.SetInt("_SrcBlend", (int) BlendMode.SrcAlpha);
                _lineMaterial.SetInt("_DstBlend", (int) BlendMode.OneMinusSrcAlpha);
                // Turn backface culling off
                _lineMaterial.SetInt("_Cull", (int) CullMode.Off);
                // Turn off depth writes
                _lineMaterial.SetInt("_ZWrite", 0);
            }
        }

        void OnPostRender()
        {
            CreateLineMaterial();

            _lineMaterial.SetPass(0);

            GL.Begin(GL.LINES);

            if (showSub)
            {
                GL.Color(subColor);

                for (float j = 0; j <= gridSizeY; j += smallStep)
                {
                    for (float i = 0; i <= gridSizeZ; i += smallStep)
                    {
                        GL.Vertex3(startX, startY + j, startZ + i);
                        GL.Vertex3(startX + gridSizeX, startY + j, startZ + i);
                    }

                    for (float i = 0; i <= gridSizeX; i += smallStep)
                    {
                        GL.Vertex3(startX + i, startY + j, startZ);
                        GL.Vertex3(startX + i, startY + j, startZ + gridSizeZ);
                    }
                }

                for (float i = 0; i <= gridSizeZ; i += smallStep)
                {
                    for (float k = 0; k <= gridSizeX; k += smallStep)
                    {
                        GL.Vertex3(startX + k, startY, startZ + i);
                        GL.Vertex3(startX + k, startY + gridSizeY, startZ + i);
                    }
                }
            }

            if (showMain)
            {
                GL.Color(mainColor);

                for (float j = 0; j <= gridSizeY; j += largeStep)
                {
                    for (float i = 0; i <= gridSizeZ; i += largeStep)
                    {
                        GL.Vertex3(startX, startY + j, startZ + i);
                        GL.Vertex3(startX + gridSizeX, startY + j, startZ + i);
                    }

                    for (float i = 0; i <= gridSizeX; i += largeStep)
                    {
                        GL.Vertex3(startX + i, startY + j, startZ);
                        GL.Vertex3(startX + i, startY + j, startZ + gridSizeZ);
                    }
                }

                for (float i = 0; i <= gridSizeZ; i += largeStep)
                {
                    for (float k = 0; k <= gridSizeX; k += largeStep)
                    {
                        GL.Vertex3(startX + k, startY, startZ + i);
                        GL.Vertex3(startX + k, startY + gridSizeY, startZ + i);
                    }
                }
            }

            GL.End();
        }
    }
}