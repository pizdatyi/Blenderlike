using UnityEngine;
using System.Collections.Generic;
using RTG;
using HarmonyLib;
using Studio;
using System.Collections;
using ToolBox.Extensions;

namespace Blenderlike
{
    public static class StudioGuideObjectManager
    {
        private enum GizmoId
        {
            Move = 0,
            Rotate = 1,
            Scale = 2,
            Universal = 3
        }

        private static HashSet<GuideObject> _selectedGuideObjects = new HashSet<GuideObject>();
        private static int _studioGizmoMode = 0;

        public static Dictionary<GuideObject, GuideObjectGizmo> _objectSelectGizmos = new Dictionary<GuideObject, GuideObjectGizmo>();
        public static GuideObjectTransformGizmo _objectMoveGizmo;
        public static GuideObjectTransformGizmo _objectRotationGizmo;
        public static GuideObjectTransformGizmo _objectScaleGizmo;
        public static GuideObjectTransformGizmo _objectUniversalGizmo;

        public static bool _showGizmos = true;

        public static float _showTransformSpacesTimer = 0f;

        public static GizmoSpace _transformSpace = GizmoSpace.Global;

        /// <summary>
        /// The current work gizmo id. The work gizmo is the gizmo which is currently used
        /// to transform objects. The W,E,R,T keys can be used to change the work gizmo as
        /// needed.
        /// </summary>
        private static GizmoId _workGizmoId;
        /// <summary>
        /// A reference to the current work gizmo. If the work gizmo id is GizmoId.Move, then
        /// this will point to '_objectMoveGizmo'. For GizmoId.Rotate, it will point to 
        /// '_objectRotationGizmo' and so on.
        /// </summary>
        public static GuideObjectTransformGizmo _workGizmo;
        /// <summary>
        /// A list of objects which are currently selected. This is also the list that holds
        /// the gizmo target objects. 
        /// </summary>
        private static List<GuideObject> _selectedObjects = new List<GuideObject>();

        private static GuideObjectManager originalGuideObjectManager;

        public static RTGApp.RTContainer container;

        public static bool IsInitialized { get; private set; }

        public static IEnumerator Init()
        {
            yield return null;

            container = new RTGApp.RTContainer();
            container.sceneGrid.Settings.IsVisible = false;
            container.gizmoEngine.SceneGizmoLookAndFeel.ScreenOffset = new Vector2(0, -50);

            container.gizmoEngine.UniversalGizmoLookAndFeel3D.SetMvDblSliderSize(2f);
            container.gizmoEngine.UniversalGizmoLookAndFeel3D.SetMvDblSliderBorderType(GizmoQuad3DBorderType.Box);
            container.gizmoEngine.UniversalGizmoLookAndFeel3D.SetMvDblSliderBorderShadeMode(GizmoShadeMode.Flat);
            container.gizmoEngine.UniversalGizmoLookAndFeel3D.SetMvDblSliderBorderFillMode(GizmoFillMode3D.Wire);
            container.gizmoEngine.UniversalGizmoLookAndFeel3D.SetMvDblSliderBorderBoxDepth(0.05f);
            container.gizmoEngine.UniversalGizmoLookAndFeel3D.SetMvDblSliderBorderBoxHeight(0.05f);
            container.gizmoEngine.UniversalGizmoLookAndFeel3D.SetMvSliderLength(10f);

            container.gizmoEngine.UniversalGizmoLookAndFeel3D.SetRtRotationArcVisible(false);
            container.gizmoEngine.UniversalGizmoLookAndFeel3D.SetRtAxisBorderType(GizmoCircle3DBorderType.Torus);
            container.gizmoEngine.UniversalGizmoLookAndFeel3D.SetRtMidCapVisible(false);
            container.gizmoEngine.UniversalGizmoLookAndFeel3D.SetRtCamLookSliderPolyBorderType(GizmoPolygon2DBorderType.Thick);
            container.gizmoEngine.UniversalGizmoLookAndFeel3D.SetRtCamLookSliderPolyBorderThickness(2f);

            container.gizmoEngine.UniversalGizmoLookAndFeel2D.SetMvSliderVisible(0, AxisSign.Positive, false);
            container.gizmoEngine.UniversalGizmoLookAndFeel2D.SetMvSliderVisible(1, AxisSign.Positive, false);
            container.gizmoEngine.UniversalGizmoLookAndFeel2D.SetMvSliderVisible(2, AxisSign.Positive, false);
            container.gizmoEngine.UniversalGizmoLookAndFeel2D.SetMvSliderCapVisible(0, AxisSign.Positive, false);
            container.gizmoEngine.UniversalGizmoLookAndFeel2D.SetMvSliderCapVisible(1, AxisSign.Positive, false);
            container.gizmoEngine.UniversalGizmoLookAndFeel2D.SetMvSliderCapVisible(2, AxisSign.Positive, false);

            container.gizmoEngine.UniversalGizmoLookAndFeel3D.SetScDblSliderSize(2f);

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

            originalGuideObjectManager = Singleton<GuideObjectManager>.Instance;

            _showGizmos = Singleton<Studio.Studio>.Instance.workInfo.visibleAxis;

            //var cameraInfo = Singleton<StudioScene>.Instance.cameraInfo;
            //Singleton<Studio.Studio>.Instance.workInfo.visibleAxis = false;
            //cameraInfo.physicsRaycaster.enabled = false;
            //cameraInfo.cameraCtrl.ReflectOption();     

            // Create the 4 gizmos
            _objectMoveGizmo = RTGizmosEngine.Get.CreateGuideObjectMoveGizmo();
            _objectRotationGizmo = RTGizmosEngine.Get.CreateGuideObjectRotationGizmo();
            _objectScaleGizmo = RTGizmosEngine.Get.CreateGuideObjectScaleGizmo();
            _objectUniversalGizmo = RTGizmosEngine.Get.CreateGuideObjectUniversalGizmo();

            // Start hidden
            _objectMoveGizmo.Gizmo.SetEnabled(false);
            _objectRotationGizmo.Gizmo.SetEnabled(false);
            _objectScaleGizmo.Gizmo.SetEnabled(false);
            _objectUniversalGizmo.Gizmo.SetEnabled(false);

            _objectMoveGizmo.SetTargetObjects(_selectedObjects);
            _objectRotationGizmo.SetTargetObjects(_selectedObjects);
            _objectScaleGizmo.SetTargetObjects(_selectedObjects);
            _objectUniversalGizmo.SetTargetObjects(_selectedObjects);

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

            // We will change the pivot type when the P key is pressed
            if (RTInput.WasKeyPressedThisFrame(KeyCode.P))
            {
                GizmoObjectTransformPivot currentPivot = _objectMoveGizmo.TransformPivot;

                if (currentPivot == GizmoObjectTransformPivot.ObjectGroupCenter)
                { 
                    SetTransformPivot(GizmoObjectTransformPivot.ObjectMeshPivot);
                }
                else
                { 
                    SetTransformPivot(GizmoObjectTransformPivot.ObjectGroupCenter);
                }
            }


            if (_studioGizmoMode != originalGuideObjectManager.mode)
            {
                _studioGizmoMode = originalGuideObjectManager.mode;
                SetWorkGizmoId((GizmoId)_studioGizmoMode);
            }

            if (!(_selectedGuideObjects.SetEquals(originalGuideObjectManager.hashSelectObject)))
            {
                _selectedGuideObjects = new HashSet<GuideObject>(originalGuideObjectManager.hashSelectObject);
                OnSelectionChanged();
            }


            if (RTInput.WasKeyPressedThisFrame(KeyCode.T)) SetWorkGizmoId(GizmoId.Universal);
        }


        public static void DrawGUI(bool isUsingCursor)
        {
            if (!IsInitialized) return;

            var guiStyle = new GUIStyle();
            guiStyle.normal.textColor = Color.green;


            Vector2 mousePosition = isUsingCursor ? Blenderlike.cursorPosition : (Vector2)Input.mousePosition;
            mousePosition.y = Screen.height - mousePosition.y;

            string transformSpace = "Transform Space: " + _objectMoveGizmo.TransformSpace.ToString();
            string transformPivot = "Transform Pivot: " + _objectMoveGizmo.TransformPivot.ToString();

            GUI.Label(new Rect(mousePosition.x + 20, mousePosition.y + 20, 200, 20), transformSpace, guiStyle);
            GUI.Label(new Rect(mousePosition.x + 20, mousePosition.y + 40, 200, 20), transformPivot, guiStyle);
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

            if (_selectedObjects.Count != 0)
            {
                if (_showGizmos)
                    _workGizmo.Gizmo.SetEnabled(true);

                _workGizmo.SetTargetPivotObject(_selectedObjects[_selectedObjects.Count - 1]);
            }
        }

        private static void OnSelectionChanged()
        {
            if (!IsInitialized) return;

            _selectedObjects.Clear();

            // HIDE ORIGINAL GUIDES
            if (!originalGuideObjectManager.selectObjects.IsNullOrEmpty())
            {
                _selectedObjects.AddRange(originalGuideObjectManager.selectObjects);
                foreach (GuideObject go in _selectedObjects)
                {
                    foreach (var guide in go.guide)
                    {
                        if (guide is GuideSelect) continue;
                        guide.draw = !Blenderlike.enableNewTransformGizmos.Value;
                    }
                }
                /*
                foreach (GuideObject go in _objectSelectGizmos.Keys)
                {
                    go.guideSelect.draw = !Blenderlike.enableNewSelectGizmos.Value;
                }*/
            }

            if (_selectedObjects.Count != 0)
            {
                _workGizmo.Gizmo.SetEnabled(_showGizmos);
                _workGizmo.SetTargetPivotObject(_selectedObjects[_selectedObjects.Count - 1]);
                _workGizmo.RefreshPositionAndRotation();
            }
            else
            {
                SetGizmoEnabled(false);
            }

            foreach (var gizmo in _objectSelectGizmos.Values)
            {
                gizmo.UpdateGizmo();
            }
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

        private static void SetTransformPivot(GizmoObjectTransformPivot transformPivot)
        {
            _showTransformSpacesTimer = 1f;

            _objectMoveGizmo.SetTransformPivot(transformPivot);
            _objectRotationGizmo.SetTransformPivot(transformPivot);
            _objectScaleGizmo.SetTransformPivot(transformPivot);
            _objectUniversalGizmo.SetTransformPivot(transformPivot);
        }

        private static void RefreshGizmoTransform()
        {
            if (!IsInitialized || _workGizmo == null) return;

            _workGizmo.RefreshPositionAndRotation(true);
        }

        private static void RefreshGizmoTransform(Vector3 scale)
        {
            if (!IsInitialized || _workGizmo == null) return;

            _workGizmo.RefreshPositionAndRotation(true);
        }

        public static class Hooks
        {
            /*
            [HarmonyPostfix, HarmonyPatch(typeof(FKCtrl.TargetInfo), nameof(FKCtrl.TargetInfo.CopyBone))]
            static void CopyBoneChar(FKCtrl.TargetInfo __instance)
            {
                __instance.changeAmount.pos = __instance.transform.localPosition;
                __instance.changeAmount.scale = __instance.transform.localScale;
            }
            [HarmonyPostfix, HarmonyPatch(typeof(FKCtrl.TargetInfo), nameof(FKCtrl.TargetInfo.Update))]
            static void UpdateChar(FKCtrl.TargetInfo __instance)
            {
                __instance.transform.localPosition = __instance.changeAmount.pos;
                __instance.transform.localScale = __instance.changeAmount.scale;
            }





            [HarmonyPostfix, HarmonyPatch(typeof(ItemFKCtrl.TargetInfo), nameof(ItemFKCtrl.TargetInfo.CopyBone))]
            static void CopyBone(ItemFKCtrl.TargetInfo __instance)
            {
                __instance.changeAmount.pos = __instance.transform.localPosition;
                __instance.changeAmount.scale = __instance.transform.localScale;
            }

            [HarmonyPostfix, HarmonyPatch(typeof(ItemFKCtrl.TargetInfo), nameof(ItemFKCtrl.TargetInfo.Update))]
            static void Update(ItemFKCtrl.TargetInfo __instance)
            {
                __instance.transform.localPosition = __instance.changeAmount.pos;
                __instance.transform.localScale = __instance.changeAmount.scale;
            }
            */


            [HarmonyPostfix, HarmonyPatch(typeof(GuideObjectManager), nameof(GuideObjectManager.Add))]
            static void OnGuideObjectCreatePostfix(ref GuideObject __result)
            {
                GuideObject guideObject = __result;
                if (guideObject == null) return;

                guideObject.changeAmount.onChangePos -= RefreshGizmoTransform;
                guideObject.changeAmount.onChangePos += RefreshGizmoTransform;

                guideObject.changeAmount.onChangeRot -= RefreshGizmoTransform;
                guideObject.changeAmount.onChangeRot += RefreshGizmoTransform;

                guideObject.changeAmount.onChangeScale -= RefreshGizmoTransform;
                guideObject.changeAmount.onChangeScale += RefreshGizmoTransform;

                if (!IsInitialized) return;
                
                //GuideObjectGizmo gizmo = RTGizmosEngine.Get.CreateGuideObjectGizmo(guideObject);
                //_objectSelectGizmos.Add(guideObject, gizmo);
            }

            [HarmonyPostfix, HarmonyPatch(typeof(GuideObjectManager), nameof(GuideObjectManager.SetSelectObject))]
            static void SetSelectObjectPostfix(GuideObjectManager __instance, GuideObject _object, bool _multiple = true)
            {
                GuideObject guideObject = _object;
                if (guideObject == null) return;


                guideObject.changeAmount.onChangePos -= RefreshGizmoTransform;
                guideObject.changeAmount.onChangePos += RefreshGizmoTransform;

                guideObject.changeAmount.onChangeRot -= RefreshGizmoTransform;
                guideObject.changeAmount.onChangeRot += RefreshGizmoTransform;

                guideObject.changeAmount.onChangeScale -= RefreshGizmoTransform;
                guideObject.changeAmount.onChangeScale += RefreshGizmoTransform;
            }


            [HarmonyPrefix, HarmonyPatch(typeof(GuideObjectManager), nameof(GuideObjectManager.Delete))]
            static void OnGuideObjectDeletePostfix(GuideObject _object, bool _destroy = true)
            {
                GuideObject guideObject = _object;
                if (guideObject == null || !IsInitialized) return;

                if (_objectSelectGizmos.ContainsKey(guideObject))
                    _objectSelectGizmos.Remove(guideObject);
            }
            
            [HarmonyPostfix, HarmonyPatch(typeof(StudioScene), nameof(StudioScene.OnClickAxis))]
            static void OnClickAxisPostfix(StudioScene __instance)
            {
                if (!IsInitialized) return;

                bool value = Singleton<Studio.Studio>.Instance.workInfo.visibleAxis;

                bool transformGizmos = Blenderlike.enableNewTransformGizmos.Value && value;

                _showGizmos = transformGizmos;
                OnSelectionChanged();
                /*
                bool selectGizmos = Blenderlike.enableNewSelectGizmos.Value && value;
                foreach (var kvp in _objectSelectGizmos)
                {
                    kvp.Value.Gizmo.SetEnabled(selectGizmos);
                }*/
            }
            
            [HarmonyPrefix, HarmonyPatch(typeof(Studio.CameraControl), nameof(Studio.CameraControl.InputMouseProc))]
            static bool InputMouseProcPostfix()
            {
                if (!IsInitialized) return true;

                if (Blenderlike.isUsingGizmo)
                {
                    return false;
                }

                return true;
            }

        }

    }
}
