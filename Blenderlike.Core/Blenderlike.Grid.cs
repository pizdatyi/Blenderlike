using UnityEngine;

namespace Blenderlike
{
    public class Blenderlike_Grid : MonoBehaviour
    {
        // GRID SETTINGS

        public bool drawGrid = true;
        public bool useLookPosition = false;
        public bool useLookPositionForAxes = false;

        public Color gridColor = new Color(0.6f, 0.6f, 0.6f, 0.3f);
        public Color secondaryGridColor = new Color(0.6f, 0.6f, 0.6f, 0.05f);

        public int gridCount = 100;
        public float gridSize = 1.0f;
        public int secondaryGridSize = 3;

        public Material GLMat;
        public Camera cam;

        // PRIVATE

        private Ray ray;
        private float rayDist;
        private Vector3 lookPosition;
        private Plane world = new Plane(Vector3.up, Vector3.zero);

        void Update()
        {
            // Update config variables
            drawGrid = Blenderlike.drawGrid.Value;
            useLookPosition = Blenderlike.useLookPosition.Value;
            useLookPositionForAxes = Blenderlike.useLookPositionForAxes.Value;
            gridColor = Blenderlike.gridColor.Value;
            secondaryGridColor = Blenderlike.gridSecondaryColor.Value;
            gridCount = Blenderlike.gridCount.Value;
            secondaryGridSize = Blenderlike.secondaryGridSize.Value;
            gridSize = Blenderlike.gridSize.Value;

            if (Blenderlike.gridColorPicker.Value)
            {
                Studio.Studio.Instance.colorPalette.visible = false;
                Studio.Studio.Instance.colorPalette.Setup("Grid Color", gridColor, (col) =>
                {
                    Blenderlike.gridColor.Value = col;
                }, true);

                Blenderlike.gridColorPicker.Value = false;
            }

            if (Blenderlike.gridSecondaryColorPicker.Value)
            {
                Studio.Studio.Instance.colorPalette.visible = false;
                Studio.Studio.Instance.colorPalette.Setup("Grid Secondary Color", secondaryGridColor, (col) =>
                {
                    Blenderlike.gridSecondaryColor.Value = col;
                }, true);

                Blenderlike.gridSecondaryColorPicker.Value = false;
            }

            if (!drawGrid)
                return;

            if (GLMat == null || cam == null)
                return;

            ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            world.Raycast(ray, out rayDist);
            lookPosition = ray.GetPoint(rayDist);
        }

        void OnPostRender()
        {
            if (!drawGrid)
                return;

            if (GLMat == null || cam == null)
                return;

            GL.PushMatrix();
            GLMat.SetPass(0);
            GL.Begin(GL.LINES);

            Vector3 gridCenter = useLookPosition ? lookPosition : CameraControlWrapper.TargetPos;

            Vector3 roundedPos = new Vector3(Round(gridCenter.x), 0, Round(gridCenter.z));

            // Primary Grid
            DrawGridLines(roundedPos, gridCount, gridSize, gridColor);

            // Secondary Grid
            int secondaryGridCount = gridCount * secondaryGridSize - 1;
            float _secondaryGridSize = gridSize / secondaryGridSize;
            DrawGridLines(roundedPos, secondaryGridCount, _secondaryGridSize, secondaryGridColor);

            // XYZ axes lines
            DrawAxisLines();

            GL.End();
            GL.PopMatrix();
        }

        void DrawGridLines(Vector3 center, int gridCount, float gridSize, Color color)
        {
            GL.Color(color);

            /*
            for (int i = -count + 1; i < count + 1; i++)
            {
                // X lines
                GL.Vertex(center + new Vector3(i * size - 0.5f * size, 0, count * size - 0.5f * size));
                GL.Vertex(center + new Vector3(i * size - 0.5f * size, 0, -count * size + 0.5f * size));
                // Z lines
                GL.Vertex(center + new Vector3(count * size - 0.5f * size, 0, i * size - 0.5f * size));
                GL.Vertex(center + new Vector3(-count * size + 0.5f * size, 0, i * size - 0.5f * size));
            }
            */

            //Major x line
            GL.Vertex(center + new Vector3(gridCount * gridSize, 0, 0));
            GL.Vertex(center + new Vector3(-gridCount * gridSize, 0, 0));
            //Major z line
            GL.Vertex(center + new Vector3(0, 0, gridCount * gridSize));
            GL.Vertex(center + new Vector3(0, 0, -gridCount * gridSize));

            for (int i = 1; i < gridCount + 1; i++)
            {
                //positive x lines
                GL.Vertex(center + new Vector3(i * gridSize, 0, gridCount * gridSize));
                GL.Vertex(center + new Vector3(i * gridSize, 0, -gridCount * gridSize));
                //negative x lines
                GL.Vertex(center + new Vector3(-i * gridSize, 0, gridCount * gridSize));
                GL.Vertex(center + new Vector3(-i * gridSize, 0, -gridCount * gridSize));
                //positive z lines
                GL.Vertex(center + new Vector3(gridCount * gridSize, 0, i * gridSize));
                GL.Vertex(center + new Vector3(-gridCount * gridSize, 0, i * gridSize));
                //negative z lines
                GL.Vertex(center + new Vector3(gridCount * gridSize, 0, -i * gridSize));
                GL.Vertex(center + new Vector3(-gridCount * gridSize, 0, -i * gridSize));


            }
        }

        void DrawAxisLines()
        {
            float v = 0.6f;
            float s = 0.2f;
            float a = 0.8f;

            Vector3 roundedPos = new Vector3(Round(lookPosition.x), 0, Round(lookPosition.z));
            Vector3 camPos = new Vector3(Round(cam.transform.position.x), cam.transform.position.y, Round(cam.transform.position.z));

            Vector3 center = useLookPositionForAxes ? roundedPos : Vector3.zero;

            // XYZ axes lines
            GL.Color(new Color(v, s, s, a));
            GL.Vertex(center);
            GL.Vertex(new Vector3(Mathf.Abs(camPos.x) + gridCount * gridSize, center.y, center.z)); // Extend to the end of positive X axis
            GL.Vertex(center);
            GL.Vertex(new Vector3((Mathf.Abs(camPos.x) + gridCount * gridSize) * -1, center.y, center.z)); // Extend to the end of negative X axis

            GL.Color(new Color(s, v, s, a));
            GL.Vertex(center);
            GL.Vertex(new Vector3(center.x, Mathf.Abs(camPos.y) + gridCount * gridSize, center.z)); // Extend to the end of positive Y axis
            GL.Vertex(center);
            GL.Vertex(new Vector3(center.x, (Mathf.Abs(camPos.y) + gridCount * gridSize) * -1, center.z)); // Extend to the end of negative Y axis

            GL.Color(new Color(s, s, v, a));
            GL.Vertex(center);
            GL.Vertex(new Vector3(center.x, center.y, Mathf.Abs(camPos.z) + gridCount * gridSize)); // Extend to the end of positive Z axis
            GL.Vertex(center);
            GL.Vertex(new Vector3(center.x, center.y, (Mathf.Abs(camPos.z) + gridCount * gridSize) * -1)); // Extend to the end of negative Z axis
        }

        float Round(float x)
        {
            return Mathf.Round(x / gridSize) * gridSize;
        }
    }
}