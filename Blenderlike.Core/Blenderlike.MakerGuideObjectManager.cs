
using UnityEngine;
using System.Collections.Generic;
using RTG;
using HarmonyLib;
using Studio;
using System.Collections;
using ToolBox.Extensions;
#if KK
using ChaCustom;
#endif

namespace Blenderlike
{
    public static class MakerGuideObjectManager
    {
        private enum GizmoId
        {
            Move = 0,
            Rotate = 1,
            Scale = 2,
            Universal = 3
        }

        public static AccesoryTransformGizmo _objectMoveGizmo;
        public static AccesoryTransformGizmo _objectRotationGizmo;
        public static AccesoryTransformGizmo _objectScaleGizmo;
        public static AccesoryTransformGizmo _objectUniversalGizmo;

        public static bool _showGizmos = true;

        public static float _showTransformSpacesTimer = 0f;

        public static GizmoSpace _transformSpace = GizmoSpace.Global;

        private static GizmoId _workGizmoId;

        public static AccesoryTransformGizmo _workGizmo;

        public static RTGApp.RTContainer container;

#if KK
        public static CustomAcsMoveWindow customAcsMoveWindow;
#endif

        public static bool IsInitialized { get; private set; }

        public static IEnumerator Init()
        {
            yield return null;

            container = new RTGApp.RTContainer();
            container.sceneGrid.Settings.IsVisible = false;
            container.gizmoEngine.SceneGizmoLookAndFeel.ScreenOffset = new Vector2(-260, -10);

            container.gizmoEngine.MoveGizmoLookAndFeel3D.SetDblSliderSize(2f);
            container.gizmoEngine.MoveGizmoLookAndFeel3D.SetDblSliderBorderType(GizmoQuad3DBorderType.Box);
            container.gizmoEngine.MoveGizmoLookAndFeel3D.SetDblSliderBorderShadeMode(GizmoShadeMode.Flat);
            container.gizmoEngine.MoveGizmoLookAndFeel3D.SetDblSliderBorderFillMode(GizmoFillMode3D.Wire);
            container.gizmoEngine.MoveGizmoLookAndFeel3D.SetDblSliderBorderBoxDepth(0.05f);
            container.gizmoEngine.MoveGizmoLookAndFeel3D.SetDblSliderBorderBoxHeight(0.05f);

            container.gizmoEngine.ScaleGizmoLookAndFeel3D.SetDblSliderSize(2f);
            container.gizmoEngine.ScaleGizmoLookAndFeel3D.SetDblSliderBorderType(GizmoQuad3DBorderType.Box);
            container.gizmoEngine.ScaleGizmoLookAndFeel3D.SetDblSliderBorderShadeMode(GizmoShadeMode.Flat);
            container.gizmoEngine.ScaleGizmoLookAndFeel3D.SetDblSliderBorderFillMode(GizmoFillMode3D.Wire);
            container.gizmoEngine.ScaleGizmoLookAndFeel3D.SetDblSliderBorderBoxDepth(0.05f);
            container.gizmoEngine.ScaleGizmoLookAndFeel3D.SetDblSliderBorderBoxHeight(0.05f);

            container.gizmoEngine.MoveGizmoLookAndFeel2D.SetSliderVisible(0, AxisSign.Positive, false);
            container.gizmoEngine.MoveGizmoLookAndFeel2D.SetSliderVisible(1, AxisSign.Positive, false);
            container.gizmoEngine.MoveGizmoLookAndFeel2D.SetSliderVisible(2, AxisSign.Positive, false);
            container.gizmoEngine.MoveGizmoLookAndFeel2D.SetSliderCapVisible(0, AxisSign.Positive, false);
            container.gizmoEngine.MoveGizmoLookAndFeel2D.SetSliderCapVisible(1, AxisSign.Positive, false);
            container.gizmoEngine.MoveGizmoLookAndFeel2D.SetSliderCapVisible(2, AxisSign.Positive, false);
            container.gizmoEngine.MoveGizmoLookAndFeel2D.SetDblSliderPlaneType(GizmoPlane2DType.Circle);
            container.gizmoEngine.MoveGizmoLookAndFeel2D.SetDblSliderFillMode(GizmoFillMode2D.Filled);

            container.gizmoEngine.RotationGizmoLookAndFeel3D.SetRotationArcVisible(false);
            container.gizmoEngine.RotationGizmoLookAndFeel3D.SetAxisBorderType(GizmoCircle3DBorderType.Torus);
            container.gizmoEngine.RotationGizmoLookAndFeel3D.SetMidCapVisible(false);
            container.gizmoEngine.RotationGizmoLookAndFeel3D.SetCamLookSliderPolyBorderType(GizmoPolygon2DBorderType.Thick);
            container.gizmoEngine.RotationGizmoLookAndFeel3D.SetCamLookSliderPolyBorderThickness(2f);

            container.focusCamera.LookAroundSettings.IsLookAroundEnabled = false;
            container.focusCamera.OrbitSettings.IsOrbitEnabled = false;
            container.focusCamera.PanSettings.IsPanningEnabled = false;
            container.focusCamera.ZoomSettings.IsZoomEnabled = true;
            container.focusCamera.ProjectionSwitchSettings.SwitchMode = CameraProjectionSwitchMode.Instant;
            container.focusCamera.ZoomSettings.OrthoStandardZoomSensitivity = 30f;
            container.focusCamera.ZoomSettings.PerspStandardZoomSensitivity = 0f;
            container.focusCamera.RotationSwitchSettings.SwitchMode = CameraRotationSwitchMode.Constant;
            container.undoRedo.SetEnabled(false);

            yield return null;

            // Create the 4 gizmos
            _objectMoveGizmo = RTGizmosEngine.Get.CreateAccesoryMoveGizmo();
            _objectRotationGizmo = RTGizmosEngine.Get.CreateAccesoryRotationGizmo();
            _objectScaleGizmo = RTGizmosEngine.Get.CreateAccesoryScaleGizmo();
            _objectUniversalGizmo = RTGizmosEngine.Get.CreateAccesoryUniversalGizmo();

            // Start hidden
            _objectMoveGizmo.Gizmo.SetEnabled(false);
            _objectRotationGizmo.Gizmo.SetEnabled(false);
            _objectScaleGizmo.Gizmo.SetEnabled(false);
            _objectUniversalGizmo.Gizmo.SetEnabled(false);

#if KK
            customAcsMoveWindow = Object.FindObjectOfType<CustomAcsMoveWindow>();


            _objectMoveGizmo._customMoveWindow = customAcsMoveWindow;
            _objectRotationGizmo._customMoveWindow = customAcsMoveWindow;
            _objectScaleGizmo._customMoveWindow = customAcsMoveWindow;
            _objectUniversalGizmo._customMoveWindow = customAcsMoveWindow;
#endif

            _workGizmo = _objectMoveGizmo;
            _workGizmoId = GizmoId.Move;

            IsInitialized = true;
        }


        public static void UpdateManager()
        {
            if (!IsInitialized) return;

            if (_showTransformSpacesTimer > 0)
                _showTransformSpacesTimer -= Time.deltaTime;
            
            
            if (RTInput.WasKeyPressedThisFrame(KeyCode.G)) SetTransformSpace(GizmoSpace.Global);
            else if (RTInput.WasKeyPressedThisFrame(KeyCode.L)) SetTransformSpace(GizmoSpace.Local);

            if (RTInput.WasKeyPressedThisFrame(KeyCode.Q)) SetWorkGizmoId(GizmoId.Move);
            else if (RTInput.WasKeyPressedThisFrame(KeyCode.W)) SetWorkGizmoId(GizmoId.Rotate);
            else if (RTInput.WasKeyPressedThisFrame(KeyCode.E)) SetWorkGizmoId(GizmoId.Scale);
            else if (RTInput.WasKeyPressedThisFrame(KeyCode.T)) SetWorkGizmoId(GizmoId.Universal);
        }


        public static void DrawGUI(bool isUsingCursor)
        {
            if (!IsInitialized) return;

            var guiStyle = new GUIStyle();
            guiStyle.normal.textColor = Color.green;

            Vector2 mousePosition = isUsingCursor ? Blenderlike.cursorPosition : (Vector2)Input.mousePosition;
            mousePosition.y = Screen.height - mousePosition.y;

            string transformSpace = "Transform Space: " + _objectMoveGizmo.TransformSpace.ToString();

            GUI.Label(new Rect(mousePosition.x + 20, mousePosition.y + 20, 200, 20), transformSpace, guiStyle);
        }

        private static void SetWorkGizmoId(GizmoId gizmoId)
        {
            if (gizmoId == _workGizmoId) return;

            SetGizmoEnabled(false);

            _workGizmoId = gizmoId;
            if (gizmoId == GizmoId.Move) _workGizmo = _objectMoveGizmo;
            else if (gizmoId == GizmoId.Rotate) _workGizmo = _objectRotationGizmo;
            else if (gizmoId == GizmoId.Scale) _workGizmo = _objectScaleGizmo;
            else if (gizmoId == GizmoId.Universal) _workGizmo = _objectUniversalGizmo;

#if KK
            if (customAcsMoveWindow != null && customAcsMoveWindow.tglReference.isOn)
            {
                if (_showGizmos)
                    _workGizmo.Gizmo.SetEnabled(true);
            }
#endif
        }

        private static void SetGizmoEnabled(bool value)
        {
            _objectMoveGizmo.Gizmo.SetEnabled(value);
            _objectRotationGizmo.Gizmo.SetEnabled(value);
            _objectScaleGizmo.Gizmo.SetEnabled(value);
            _objectUniversalGizmo.Gizmo.SetEnabled(value);
        }

        private static void SetTransformSpace(GizmoSpace transformSpace)
        {
            if (transformSpace == _transformSpace) return;

            _showTransformSpacesTimer = 1f;

            _objectMoveGizmo.SetTransformSpace(transformSpace);
            _objectRotationGizmo.SetTransformSpace(transformSpace);
            _objectScaleGizmo.SetTransformSpace(transformSpace);
            _objectUniversalGizmo.SetTransformSpace(transformSpace);

            _transformSpace = transformSpace;
        }

    }
}