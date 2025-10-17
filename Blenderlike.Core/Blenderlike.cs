using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using KKAPI.Utilities;
using Studio;
using Vectrosity;
using System.Collections;
using System;
using KKAPI.Studio.UI;
using RTG;
using HarmonyLib;

// WARNING: ASS CODE AHEAD

namespace Blenderlike 
{
    [BepInPlugin(GUID, PluginName, Version)]
    //[BepInProcess(KK_Plugins.Constants.StudioProcessName)]
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    public class Blenderlike : BaseUnityPlugin
    {
        #region BEPINEX CONFIG VARIABLES

        public const string GUID = "com.shallty.Blenderlike";
        public const string PluginName = "Blenderlike";
#if KK
        public const string PluginNameInternal = "KK_Blenderlike";
#elif KKS
        public const string PluginNameInternal = "KKS_Blenderlike";
#elif HS2
        public const string PluginNameInternal = "HS2_Blenderlike";
#endif
        private int WindowsUniqueId = GUID.GetHashCode();
        public const string Version = "1.0.0";
        internal static new ManualLogSource Logger;

        #endregion CONFIG VARIABLES

        #region SAVED DATA.

        // MODULES

        public static ConfigEntry<KeyboardShortcut> modulesKey;

        public static ConfigEntry<bool> modulesEnabled;
        public static ConfigEntry<bool> inputDeviceEnabled;
        public static ConfigEntry<bool> focusCameraEnabled;
        public static ConfigEntry<bool> sceneEnabled;
        public static ConfigEntry<bool> sceneGridEnabled;
        public static ConfigEntry<bool> gizmosEngineEnabled;
        public static ConfigEntry<bool> undoRedoEnabled;

        // TRANSFORM

        public static ConfigEntry<KeyboardShortcut> translateKey;
        public static ConfigEntry<KeyboardShortcut> rotateKey;
        public static ConfigEntry<KeyboardShortcut> scaleKey;

        public static ConfigEntry<float> translateSpeed;
        public static ConfigEntry<float> rotateSpeed;
        public static ConfigEntry<float> scaleSpeed;
        public static ConfigEntry<float> scrollSpeed;

        public static ConfigEntry<bool> transformEditingEnabled;
        public static ConfigEntry<bool> snappingEnabledByDefault;
        public static ConfigEntry<float> translateSnapIncrement;
        public static ConfigEntry<float> rotateSnapIncrement;
        public static ConfigEntry<float> scaleSnapIncrement;
        public static ConfigEntry<bool> enableMouseConfirmCancel;
        public static ConfigEntry<float> translateOriginSize;
        public static ConfigEntry<Color> translateOriginColor;

        // GRID

        public static ConfigEntry<bool> drawGrid;
        public static ConfigEntry<bool> useLookPosition;
        public static ConfigEntry<bool> useLookPositionForAxes;

        public static ConfigEntry<Color> gridColor;
        public static ConfigEntry<bool> gridColorPicker;

        public static ConfigEntry<Color> gridSecondaryColor;
        public static ConfigEntry<bool> gridSecondaryColorPicker;

        public static ConfigEntry<int> gridCount;
        public static ConfigEntry<float> gridSize;
        public static ConfigEntry<int> secondaryGridSize;

        // NEW GIZMOS

        public static ConfigEntry<bool> enableNewTransformGizmos;
        public static ConfigEntry<bool> enableNewSelectGizmos;

        #endregion

        public static bool translateMode = false;
        public static bool rotateMode = false;
        public static bool scaleMode = false;

        public static bool axisLockX = false;
        public static bool axisLockY = false;
        public static bool axisLockZ = false;

        public static bool isUsingGizmo = false;

        public static bool isSnapping = false;


        public static Camera camera;
        private static Vector2 originalMousePos;
        private static Vector3[] guideObjectsOffsets;
        private static Vector3 originalAvgPos;
        private static GuideObject[] selectedGuideObjects;
        private static float originalDistance;

        public static Dictionary<GuideObject, Vector3> _oldPosValues = new Dictionary<GuideObject, Vector3>();
        public static Dictionary<int, Vector3> _oldRotValues = new Dictionary<int, Vector3>();
        public static Dictionary<int, Vector3> _oldScaleValues = new Dictionary<int, Vector3>();

        public static Vector3 _makerAccsOldPos = Vector3.zero;
        public static Vector3 _makerAccsOldRot = Vector3.zero;
        public static Vector3 _makerAccsOldScale = Vector3.zero;

        private static VectorLine gizmoLockLine;
        private static VectorLine gizmoLine;
        private static Texture2D gizmoLineTexture;
        private static Texture2D gizmoCursorTexture;

        public static Vector2 cursorPosition = Vector2.zero;

        enum TransformMode
        {
            Translate,
            Rotate,
            Scale
        }

        private void Awake()
        {
            #region BEPINEX CONFIG
            Logger = base.Logger;

            // MODULES

            KeyboardShortcut _modulesKey = new KeyboardShortcut(KeyCode.LeftShift, KeyCode.B);
            modulesKey = Config.Bind("KEYBINDS", "Modules Key", _modulesKey, "Toggle all the modules.");

            modulesEnabled = Config.Bind("MODULES", "Modules Enabled", true, "Enable or disable all modules.");
            inputDeviceEnabled = Config.Bind("MODULES", "Input Device Module", true, "Enable or disable the input device module.");
            focusCameraEnabled = Config.Bind("MODULES", "Focus Camera Module", true, "Enable or disable the focus camera module.");
            sceneEnabled = Config.Bind("MODULES", "Scene Module", true, "Enable or disable the scene module.");
            sceneGridEnabled = Config.Bind("MODULES", "Scene Grid Module", true, "Enable or disable the scene grid module.");
            gizmosEngineEnabled = Config.Bind("MODULES", "Gizmos Engine Module", true, "Enable or disable the gizmos engine module.");
            undoRedoEnabled = Config.Bind("MODULES", "Undo/Redo Module", true, "Enable or disable the undo/redo module.");

            // TRANSFORMS

            KeyboardShortcut _translateKey = new KeyboardShortcut(KeyCode.G);
            translateKey = Config.Bind("KEYBINDS", "Translate Key", _translateKey, "The translate key.");
            KeyboardShortcut _rotateKey = new KeyboardShortcut(KeyCode.R);
            rotateKey = Config.Bind("KEYBINDS", "Rotate Key", _rotateKey, "The rotate key.");
            KeyboardShortcut _scaleKey = new KeyboardShortcut(KeyCode.S);
            scaleKey = Config.Bind("KEYBINDS", "Scale Key", _scaleKey, "The scale key.");


            translateSpeed = Config.Bind("GENERAL", "Translate Speed", 500f, "The speed of the translation.");
            rotateSpeed = Config.Bind("GENERAL", "Rotate Speed", 500f, "The speed of the rotation.");
            scaleSpeed = Config.Bind("GENERAL", "Scale Speed", 500f, "The speed of the scaling.");

            scrollSpeed = Config.Bind("GENERAL", "Scroll Increment", 500f, "The amount of increment when scrolling.");

            // GRID

            drawGrid = Config.Bind("GRID", "Draw Grid", true, "Enable or disable the grid.");
            useLookPosition = Config.Bind("GRID", "Use Look Position", false, "Create the grid around the point the camera is looking.");
            useLookPositionForAxes = Config.Bind("GRID", "Use Look Position For Axes", false, "Create the axes around the point the camera is looking.");

            gridColor = Config.Bind("GRID", "Grid Color", new Color(0.5f, 0.5f, 0.5f, 0.3f), "The color of the grid.");
            gridColorPicker = Config.Bind("GRID", "Grid Color Picker", false, "Open the grid color picker.");

            gridSecondaryColor = Config.Bind("GRID", "Grid Secondary Color", new Color(0.5f, 0.5f, 0.5f, 0.1f), "The color of the secondary grid.");
            gridSecondaryColorPicker = Config.Bind("GRID", "Grid Secondary Color Picker", false, "Open the grid secondary color picker.");

            gridCount = Config.Bind("GRID", "Grid Count", 100, new ConfigDescription("The number of grid cells.", new AcceptableValueRange<int>(1, 1000)));
            gridSize = Config.Bind("GRID", "Grid Size", 1f, new ConfigDescription("The size of each grid cell.", new AcceptableValueRange<float>(0.1f, 100f)));
            secondaryGridSize = Config.Bind("GRID", "Secondary Grid Size", 3, new ConfigDescription("The size of each secondary grid cell.", new AcceptableValueRange<int>(2, 100)));

            // NEW GIZMO

            enableNewSelectGizmos = Config.Bind("NEW GIZMOS", "Enable New Select Gizmos", true, "Enable or disable the new select gizmos.");
            enableNewTransformGizmos = Config.Bind("NEW GIZMOS", "Enable New Transform Gizmos", true, "Enable or disable the new transform gizmos.");
            
            //transformEditingEnabled = Config.Bind("GENERAL", "Transform Editing", true, "Enable or disable the transform editing button.");
            //snappingEnabledByDefault = Config.Bind("GENERAL", "Snapping Enabled By Default", false, "Enable or disable snapping by default.");
            translateSnapIncrement = Config.Bind("GENERAL", "Translate Snap Increment", 1f, "The snap increment for translating.");
            rotateSnapIncrement = Config.Bind("GENERAL", "Rotate Snap Increment", 45f, "The snap increment for rotating.");
            scaleSnapIncrement = Config.Bind("GENERAL", "Scale Snap Increment", 1f, "The snap increment for scaling.");
            //enableMouseConfirmCancel = Config.Bind("GENERAL", "Enable Mouse Confirm Cancel", false, "Enable or disable mouse confirm cancel.");
            //translateOriginSize = Config.Bind("GENERAL", "Translate Origin Size", 0.005f, "The size of the translate origin.");
            //translateOriginColor = Config.Bind("GENERAL", "Translate Origin Color", new Color(0.96f, 0.77f, 0, 1), "The color of the translate origin.");

            #endregion BEPINEX CONFIG

            KKAPI.Maker.MakerAPI.MakerStartedLoading += OnMakerStartedLoading;

            if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
            {
                Harmony.CreateAndPatchAll(typeof(StudioGuideObjectManager.Hooks));
                KKAPI.Studio.StudioAPI.StudioLoadedChanged += OnStudioLoaded;
            }
        }

        private void OnStudioLoaded(object sender, EventArgs e)
        {
            StartCoroutine(StudioGuideObjectManager.Init());
            StartCoroutine(CreateGizmoLine());
            StartCoroutine(LoadGrid());
        }

        private void OnMakerStartedLoading(object sender, EventArgs e)
        {
            StartCoroutine(MakerGuideObjectManager.Init());
            StartCoroutine(CreateGizmoLine());
            StartCoroutine(LoadGrid());
        }

        private static IEnumerator LoadGrid()
        {
            yield return new WaitForSecondsRealtime(1f);

            if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
            {
                var buttTex = ResourceUtils.GetEmbeddedResource("gridButton.png").LoadTexture();
                var _button = CustomToolbarButtons.AddLeftToolbarToggle(buttTex, drawGrid.Value, (b) =>
                {
                    drawGrid.Value = b;
                });
            }

            Material _mat = new Material(Shader.Find("Sprites/Default"));

            if (_mat == null)
            {
                Logger.LogError("Failed to create grid; material was empty");
                yield break;
            }

            camera = Camera.main;

            if (camera != null)
            {
                var grid = camera.gameObject.AddComponent<Blenderlike_Grid>();
                grid.GLMat = _mat;
                grid.cam = camera;
            }
            else
            {
                Logger.LogError("Error creating the grid: Main Camera wasn't found.");
            }

            yield return null;
        }

        private static IEnumerator CreateGizmoLine()
        {
            yield return new WaitForSecondsRealtime(1f);

            gizmoCursorTexture = ResourceUtils.GetEmbeddedResource("cursorTexture.png").LoadTexture();

            gizmoLineTexture = ResourceUtils.GetEmbeddedResource("dashedLineTexture.png").LoadTexture();
            gizmoLineTexture.wrapMode = TextureWrapMode.Repeat;

            gizmoLine = new VectorLine("BlenderlikeGizmoLine", new List<Vector2> { Vector2.zero, Vector2.zero }, 3f, LineType.Discrete, Joins.None)
            {
                //gizmoLine.color = Color.gray;
                texture = gizmoLineTexture,
                textureScale = 2f,
                active = false
            };

            gizmoLockLine = new VectorLine("BlenderlikeGizmoLockLine", new List<Vector3> { Vector3.zero, Vector3.zero }, 2f, LineType.Discrete, Joins.None)
            {
                active = false
            };

            yield return null;
        }

        void OnGUI()
        {
            if (translateMode || rotateMode || scaleMode || isUsingGizmo)
            {
                if (StudioGuideObjectManager._showTransformSpacesTimer > 0)
                    StudioGuideObjectManager.DrawGUI(translateMode || rotateMode || scaleMode);
                else if (MakerGuideObjectManager._showTransformSpacesTimer > 0)
                    MakerGuideObjectManager.DrawGUI(translateMode || rotateMode || scaleMode);
            }

            if (gizmoCursorTexture == null || camera == null) return;
            if (!translateMode && !rotateMode && !scaleMode) return;

            Cursor.visible = false;

            Vector2 target = camera.WorldToScreenPoint(GetGuideObjectsCenter());
            target.y = Screen.height - target.y;

            Vector2 mousePosition = cursorPosition;
            mousePosition.y = Screen.height - mousePosition.y;

            Vector2 textureCenter = new Vector2(gizmoCursorTexture.width / 8, gizmoCursorTexture.height / 8);
            Rect rect = new Rect(mousePosition.x - textureCenter.x, mousePosition.y - textureCenter.y, gizmoCursorTexture.width / 4, gizmoCursorTexture.height / 4);

            Matrix4x4 matrixBackup = GUI.matrix;

            if (rotateMode || scaleMode)
            {
                Vector2 direction = target - mousePosition;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                if (rotateMode) angle += 90;
                GUIUtility.RotateAroundPivot(angle, mousePosition);
                GUI.DrawTexture(rect, gizmoCursorTexture);
                GUI.matrix = matrixBackup;
            }
            else if (translateMode)
            {
                GUIUtility.RotateAroundPivot(90, new Vector2(rect.xMin + rect.width / 2, rect.yMin + rect.height / 2));
                GUI.DrawTexture(rect, gizmoCursorTexture);
                GUI.matrix = matrixBackup;
                GUI.DrawTexture(rect, gizmoCursorTexture);
            }
        }

        private void Update()
        {
            if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
                StudioGuideObjectManager.UpdateManager();
            else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
                MakerGuideObjectManager.UpdateManager();
            else
                return;

            if (modulesKey.Value.IsDown())
               modulesEnabled.Value = !modulesEnabled.Value;       

            if (translateMode)
            {
                translateSpeed.Value += Input.mouseScrollDelta.y * scrollSpeed.Value * Time.fixedUnscaledDeltaTime;
                float finalSpeed = translateSpeed.Value;

                if (!isUsingGizmo)
                {

                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                        finalSpeed = translateSpeed.Value * 10f;

                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                        finalSpeed = translateSpeed.Value * 0.1f;
                }

                finalSpeed = Mathf.Clamp(finalSpeed, 0.0001f, 100000f);

                cursorPosition += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * finalSpeed * Time.fixedUnscaledDeltaTime;
            }

            if (rotateMode)
            {
                rotateSpeed.Value += Input.mouseScrollDelta.y * scrollSpeed.Value * Time.fixedUnscaledDeltaTime;
                float finalSpeed = rotateSpeed.Value;

                if (!isUsingGizmo)
                {

                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                        finalSpeed = rotateSpeed.Value * 10f;

                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                        finalSpeed = rotateSpeed.Value * 0.1f;
                }

                finalSpeed = Mathf.Clamp(finalSpeed, 0.0001f, 100000f);
                

                cursorPosition += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * finalSpeed * Time.fixedUnscaledDeltaTime;
            }

            if (scaleMode)
            {
                scaleSpeed.Value += Input.mouseScrollDelta.y * scrollSpeed.Value * Time.fixedUnscaledDeltaTime;
                float finalSpeed = scaleSpeed.Value;

                if (!isUsingGizmo)
                {

                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                        finalSpeed = scaleSpeed.Value * 10f;

                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                        finalSpeed = scaleSpeed.Value * 0.1f;

                }

                finalSpeed = Mathf.Clamp(finalSpeed, 0.0001f, 100000f);

                cursorPosition += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * finalSpeed * Time.fixedUnscaledDeltaTime;
            }

            if (gizmoLine != null)
            {
                if ((translateMode || rotateMode || scaleMode) && camera != null)
                {
                    Vector3 center = GetGuideObjectsCenter();

                    gizmoLine.points2 = new List<Vector2> { camera.WorldToScreenPoint(center), cursorPosition };

                    gizmoLine.active = true;
                    gizmoLine.Draw();
                }
                else
                {
                    gizmoLine.points2.Clear();
                    gizmoLine.active = false;
                }
            }

            if (gizmoLockLine != null)
            {
                gizmoLockLine.points3.Clear();

                if (axisLockX || axisLockY || axisLockZ)
                {
                    Vector3 center = originalAvgPos;
                    List<Color32> lineColors = new List<Color32>();
                    float v = 0.6f;
                    float s = 0.2f;
                    float a = 0.8f;

                    if (!axisLockX)
                    {
                        Vector3 axis = Vector3.zero;

                        if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
                        {
                            if (StudioGuideObjectManager._transformSpace == GizmoSpace.Global)
                                axis = Vector3.right * 100000;
                            else
                            {
                                if (StudioGuideObjectManager._workGizmo._targetPivotObject != null && StudioGuideObjectManager._workGizmo._targetPivotObject.transformTarget != null)
                                    axis = StudioGuideObjectManager._workGizmo._targetPivotObject.transformTarget.right * 100000;
                            }
                        }
                        else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
                        {

                            if (MakerGuideObjectManager._transformSpace == GizmoSpace.Global)
                                axis = Vector3.right * 100000;
                            else
                            {
                                axis = MakerGuideObjectManager._workGizmo.Gizmo.Transform.Right * 100000;
                            }
                        }

                        gizmoLockLine.points3.Add(center);
                        gizmoLockLine.points3.Add(center + axis);
                        gizmoLockLine.points3.Add(center);
                        gizmoLockLine.points3.Add(center - axis);
                        lineColors.Add(new Color(v, s, s, a));
                        lineColors.Add(new Color(v, s, s, a));
                    }

                    if (!axisLockY)
                    {
                        Vector3 axis = Vector3.zero;
                        if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
                        {
                            if (StudioGuideObjectManager._transformSpace == GizmoSpace.Global)
                                axis = Vector3.up * 100000;
                            else
                            {
                                if (StudioGuideObjectManager._workGizmo._targetPivotObject != null && StudioGuideObjectManager._workGizmo._targetPivotObject.transformTarget != null)
                                    axis = StudioGuideObjectManager._workGizmo._targetPivotObject.transformTarget.up * 100000;
                            }
                        }
                        else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
                        {

                            if (MakerGuideObjectManager._transformSpace == GizmoSpace.Global)
                                axis = Vector3.up * 100000;
                            else
                            {
                                axis = MakerGuideObjectManager._workGizmo.Gizmo.Transform.Up * 100000;
                            }
                        }

                        gizmoLockLine.points3.Add(center);
                        gizmoLockLine.points3.Add(center + axis);
                        gizmoLockLine.points3.Add(center);
                        gizmoLockLine.points3.Add(center - axis);
                        lineColors.Add(new Color(s, v, s, a));
                        lineColors.Add(new Color(s, v, s, a));
                    }

                    if (!axisLockZ)
                    {
                        Vector3 axis  = Vector3.zero;

                        if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
                        {
                            if (StudioGuideObjectManager._transformSpace == GizmoSpace.Global)
                                axis = Vector3.forward * 100000;
                            else
                            {
                                if (StudioGuideObjectManager._workGizmo._targetPivotObject != null && StudioGuideObjectManager._workGizmo._targetPivotObject.transformTarget != null)
                                    axis = StudioGuideObjectManager._workGizmo._targetPivotObject.transformTarget.forward * 100000;
                            }
                        }
                        else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
                        {

                            if (MakerGuideObjectManager._transformSpace == GizmoSpace.Global)
                                axis = Vector3.forward * 100000;
                            else
                            {
                                axis = MakerGuideObjectManager._workGizmo.Gizmo.Transform.Forward * 100000;
                            }
                        }

                        gizmoLockLine.points3.Add(center);
                        gizmoLockLine.points3.Add(center + axis);
                        gizmoLockLine.points3.Add(center);
                        gizmoLockLine.points3.Add(center - axis);
                        lineColors.Add(new Color(s, s, v, a));
                        lineColors.Add(new Color(s, s, v, a));
                    }

                    if (gizmoLockLine.points3.Count > 1)
                    {
                        gizmoLockLine.SetColors(lineColors);
                        gizmoLockLine.active = true;
                        gizmoLockLine.Draw();
                    }

                }
                else
                {
                    gizmoLockLine.points3.Clear();
                    gizmoLockLine.active = false;
                }
            }

            isSnapping = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && (translateMode || rotateMode || scaleMode);

            if (isUsingGizmo) return;

            // Toggle lock axis when pressing X, Y, Z keys.
            LockAxis();

            if (translateKey.Value.IsDown())
                StartTranslation();

            if (rotateKey.Value.IsDown())
                StartRotation();

            if (scaleKey.Value.IsDown())
                StartScale();

            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return))
            {
                if (translateMode || rotateMode || scaleMode)
                {
                    ResetAxisLock();
                    Cursor.visible = true;
                }

                ConfirmTranslation();
                ConfirmRotation();
                ConfirmScale();
            }

            if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2) || Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Escape))
            {
                if (translateMode || rotateMode || scaleMode)
                {
                    ResetAxisLock();
                    Cursor.visible = true;

                }
              
                CancelTranslation();
                CancelRotation();
                CancelScale();
            }


            UpdateTranslation();
            UpdateRotation();
            UpdateScale();
        }


        #region TRANSLATION

        public static void StartTranslation()
        {
            if (translateMode) return;

            CancelTranslation();
            CancelRotation();
            CancelScale();

            if (camera == null) camera = Camera.main;

            cursorPosition = Input.mousePosition;
            originalMousePos = cursorPosition;

            originalAvgPos = GetGuideObjectsCenter();

            if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
            {
                selectedGuideObjects = Singleton<GuideObjectManager>.Instance.selectObjects;
                if (selectedGuideObjects == null || selectedGuideObjects.Length == 0) return;
                guideObjectsOffsets = new Vector3[selectedGuideObjects.Length];

                _oldPosValues.Clear();
                _oldPosValues = selectedGuideObjects.ToDictionary(x => x, x => x.transformTarget.position);
            }
#if KK
            else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
            {
                var window = MakerGuideObjectManager.customAcsMoveWindow;
                if (window != null && window.tglReference.isOn)
                {
                    _makerAccsOldPos = window.accessory.parts[window.nSlotNo].addMove[window.correctNo, 0];
                }
                else
                    return;
            }
#endif

            Vector3 planeVector = camera.transform.forward;

            // SINGLE AXIS
            if (axisLockX == false && axisLockY == true && axisLockZ == true)
            {
                planeVector.x = 0;
                planeVector = planeVector.normalized;
            }
            else if (axisLockX == true && axisLockY == false && axisLockZ == true)
            {
                planeVector.y = 0;
                planeVector = planeVector.normalized;
            }
            else if (axisLockX == true && axisLockY == true && axisLockZ == false)
            {
                planeVector.z = 0;
                planeVector = planeVector.normalized;
            }

            Plane movePlane = new Plane(planeVector, originalAvgPos);

            // DOUBLE AXIS
            if (axisLockX == true && axisLockY == false && axisLockZ == false)
                movePlane = new Plane(Vector3.Cross(Vector3.up, Vector3.forward), originalAvgPos);
            else if (axisLockX == false && axisLockY == true && axisLockZ == false)
                movePlane = new Plane(Vector3.Cross(Vector3.right, Vector3.forward), originalAvgPos);
            else if (axisLockX == false && axisLockY == false && axisLockZ == true)
                movePlane = new Plane(Vector3.Cross(Vector3.right, Vector3.up), originalAvgPos);

            Ray rayToNewPos = camera.ScreenPointToRay(originalMousePos);

            if (movePlane.Raycast(rayToNewPos, out float enter))
            {
                Vector3 moveTo = rayToNewPos.GetPoint(enter);

                Vector3 contactPoint = rayToNewPos.origin + (rayToNewPos.direction * enter);

                // SINGLE AXIS
                if (axisLockX == false && axisLockY == true && axisLockZ == true)
                    moveTo.x = contactPoint.x;
                else if (axisLockX == true && axisLockY == false && axisLockZ == true)
                    moveTo.y = contactPoint.y;
                else if (axisLockX == true && axisLockY == true && axisLockZ == false)
                    moveTo.z = contactPoint.z;

                if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
                {

                    for (int i = 0; i < selectedGuideObjects.Length; i++)
                    {
                        //guideObjectsOffsets[i] = selectedGuideObjects[i].changeAmount.pos - moveTo;
                        guideObjectsOffsets[i] = selectedGuideObjects[i].transformTarget.position - moveTo;
                    }
                }
            }

            translateMode = true;
        }

        public static void StartLocalTranslation()
        {
            if (translateMode) return;

            CancelTranslation();
            CancelRotation();
            CancelScale();

            if (camera == null) camera = Camera.main;

            cursorPosition = Input.mousePosition;
            originalMousePos = cursorPosition;
            originalAvgPos = GetGuideObjectsCenter();

            if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
            {
                selectedGuideObjects = Singleton<GuideObjectManager>.Instance.selectObjects;
                guideObjectsOffsets = new Vector3[selectedGuideObjects.Length];

                _oldPosValues.Clear();
                _oldPosValues = selectedGuideObjects.ToDictionary(x => x, x => x.transformTarget.position);

                Vector2 mousePos = cursorPosition;

                Vector3 planeVector = camera.transform.forward;
                Plane movePlane = new Plane(planeVector, originalAvgPos);
                Ray rayToNewPos = camera.ScreenPointToRay(mousePos);

                for (int i = 0; i < selectedGuideObjects.Length; i++)
                {
                    if (selectedGuideObjects[i] == null) continue;
                    if (!selectedGuideObjects[i].enablePos) continue;

                    Transform transform = selectedGuideObjects[i].transformTarget;
                    Vector3 moveTo = Vector3.zero;

                    #region Single Axis

                    bool isSingleAxis = (axisLockX == false && axisLockY == true && axisLockZ == true) || (axisLockX == true && axisLockY == false && axisLockZ == true) || (axisLockX == true && axisLockY == true && axisLockZ == false);

                    if (isSingleAxis)
                    {

                        Vector3 camViewPlaneNormalLocal = transform.InverseTransformDirection(planeVector);

                        Ray rayToNewPosLocal = new Ray();
                        rayToNewPosLocal.origin = transform.InverseTransformPoint(rayToNewPos.origin);
                        rayToNewPosLocal.direction = transform.InverseTransformDirection(rayToNewPos.direction);

                        if (axisLockX == false && axisLockY == true && axisLockZ == true)
                        {
                            camViewPlaneNormalLocal.x = 0;
                            //planeVector = planeVector.normalized;
                        }
                        else if (axisLockX == true && axisLockY == false && axisLockZ == true)
                        {
                            camViewPlaneNormalLocal.y = 0;
                            //planeVector = planeVector.normalized;
                        }
                        else if (axisLockX == true && axisLockY == true && axisLockZ == false)
                        {
                            camViewPlaneNormalLocal.z = 0;
                            //planeVector = planeVector.normalized;
                        }

                        Plane localMovePlane = new Plane(camViewPlaneNormalLocal, Vector3.zero);

                        if (localMovePlane.Raycast(rayToNewPosLocal, out float distance))
                        {
                            Vector3 contactPoint = rayToNewPosLocal.origin + (rayToNewPosLocal.direction * distance);

                            if (axisLockX == false && axisLockY == true && axisLockZ == true)
                                moveTo.x = contactPoint.x;
                            else if (axisLockX == true && axisLockY == false && axisLockZ == true)
                                moveTo.y = contactPoint.y;
                            else if (axisLockX == true && axisLockY == true && axisLockZ == false)
                                moveTo.z = contactPoint.z;
                        }

                        // Convert back to global space.
                        moveTo = transform.TransformPoint(moveTo);
                    }


                    #endregion


                    #region Double Axis

                    bool isDoubleAxis = (axisLockX == true && axisLockY == false && axisLockZ == false) || (axisLockX == false && axisLockY == true && axisLockZ == false) || (axisLockX == false && axisLockY == false && axisLockZ == true);

                    if (isDoubleAxis)
                    {

                        // DOUBLE AXIS
                        if (axisLockX == true && axisLockY == false && axisLockZ == false)
                            movePlane = new Plane(Vector3.Cross(transform.TransformDirection(Vector3.up), transform.TransformDirection(Vector3.forward)), transform.position);
                        else if (axisLockX == false && axisLockY == true && axisLockZ == false)
                            movePlane = new Plane(Vector3.Cross(transform.TransformDirection(Vector3.right), transform.TransformDirection(Vector3.forward)), transform.position);
                        else if (axisLockX == false && axisLockY == false && axisLockZ == true)
                            movePlane = new Plane(Vector3.Cross(transform.TransformDirection(Vector3.right), transform.TransformDirection(Vector3.up)), transform.position);

                        if (movePlane.Raycast(rayToNewPos, out float enter))
                        {
                            moveTo = rayToNewPos.origin + (rayToNewPos.direction * enter);
                        }
                    }

                    #endregion

                    guideObjectsOffsets[i] = transform.position - moveTo;
                }
            }
#if KK
            else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
            {
                var window = MakerGuideObjectManager.customAcsMoveWindow;
                if (window != null && window.tglReference.isOn)
                {
                    _makerAccsOldPos = window.accessory.parts[window.nSlotNo].addMove[window.correctNo, 0];
                }
                else
                    return;
            }
#endif
            translateMode = true;
        }

        public static void UpdateTranslation()
        {
            if (!translateMode) return;
            
            Vector2 mousePos = cursorPosition;
            Vector2 toNewMouse = mousePos - originalMousePos;

            if (toNewMouse.sqrMagnitude == 0) return;

            Vector3 planeVector = camera.transform.forward;

            // SINGLE AXIS
            if (axisLockX == false && axisLockY == true && axisLockZ == true)
            {
                planeVector.x = 0;
                planeVector = planeVector.normalized;
            }
            else if (axisLockX == true && axisLockY == false && axisLockZ == true)
            {
                planeVector.y = 0;
                planeVector = planeVector.normalized;
            }
            else if (axisLockX == true && axisLockY == true && axisLockZ == false)
            {
                planeVector.z = 0;
                planeVector = planeVector.normalized;
            }

            Plane movePlane = new Plane(planeVector, originalAvgPos);

            // DOUBLE AXIS
            if (axisLockX == true && axisLockY == false && axisLockZ == false)
                movePlane = new Plane(Vector3.Cross(Vector3.up, Vector3.forward), originalAvgPos);
            else if (axisLockX == false && axisLockY == true && axisLockZ == false)
                movePlane = new Plane(Vector3.Cross(Vector3.right, Vector3.forward), originalAvgPos);
            else if (axisLockX == false && axisLockY == false && axisLockZ == true)
                movePlane = new Plane(Vector3.Cross(Vector3.right, Vector3.up), originalAvgPos);


            Ray rayToNewPos = camera.ScreenPointToRay(mousePos);

            if (movePlane.Raycast(rayToNewPos, out float enter))
            {
                Vector3 moveTo = rayToNewPos.GetPoint(enter);

                Vector3 contactPoint = rayToNewPos.origin + (rayToNewPos.direction * enter);

                // SINGLE AXIS
                if (axisLockX == false && axisLockY == true && axisLockZ == true)
                    moveTo.x = contactPoint.x;
                else if (axisLockX == true && axisLockY == false && axisLockZ == true)
                    moveTo.y = contactPoint.y;
                else if (axisLockX == true && axisLockY == true && axisLockZ == false)
                    moveTo.z = contactPoint.z;

                MoveAllGuideObjects(moveTo);
            }
        }

        public static void UpdateLocalTranslation(GizmoTransform gizmoTransform)
        {
            if (!translateMode) return;

            Vector2 mousePos = cursorPosition;
            Vector2 toNewMouse = mousePos - originalMousePos;

            if (toNewMouse.sqrMagnitude == 0) return;

            Vector3 planeVector = camera.transform.forward;
            Plane movePlane = new Plane(planeVector, originalAvgPos);
            Ray rayToNewPos = camera.ScreenPointToRay(mousePos);

            if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
            {
                for (int i = 0; i < selectedGuideObjects.Length; i++)
                {
                    if (selectedGuideObjects[i] == null) continue;
                    if (!selectedGuideObjects[i].enablePos) continue;

                    Transform transform = selectedGuideObjects[i].transformTarget;
                    Vector3 moveTo = Vector3.zero;

                    #region Single Axis

                    bool isSingleAxis = (axisLockX == false && axisLockY == true && axisLockZ == true) || (axisLockX == true && axisLockY == false && axisLockZ == true) || (axisLockX == true && axisLockY == true && axisLockZ == false);

                    if (isSingleAxis)
                    {

                        Vector3 camViewPlaneNormalLocal = transform.InverseTransformDirection(planeVector);

                        Ray rayToNewPosLocal = new Ray();
                        rayToNewPosLocal.origin = transform.InverseTransformPoint(rayToNewPos.origin);
                        rayToNewPosLocal.direction = transform.InverseTransformDirection(rayToNewPos.direction);

                        if (axisLockX == false && axisLockY == true && axisLockZ == true)
                        {
                            camViewPlaneNormalLocal.x = 0;
                            //planeVector = planeVector.normalized;
                        }
                        else if (axisLockX == true && axisLockY == false && axisLockZ == true)
                        {
                            camViewPlaneNormalLocal.y = 0;
                            //planeVector = planeVector.normalized;
                        }
                        else if (axisLockX == true && axisLockY == true && axisLockZ == false)
                        {
                            camViewPlaneNormalLocal.z = 0;
                            //planeVector = planeVector.normalized;
                        }

                        Plane localMovePlane = new Plane(camViewPlaneNormalLocal, Vector3.zero);

                        if (localMovePlane.Raycast(rayToNewPosLocal, out float distance))
                        {
                            Vector3 contactPoint = rayToNewPosLocal.origin + (rayToNewPosLocal.direction * distance);

                            if (axisLockX == false && axisLockY == true && axisLockZ == true)
                                moveTo.x = contactPoint.x;
                            else if (axisLockX == true && axisLockY == false && axisLockZ == true)
                                moveTo.y = contactPoint.y;
                            else if (axisLockX == true && axisLockY == true && axisLockZ == false)
                                moveTo.z = contactPoint.z;
                        }

                        // Convert back to global space.
                        moveTo = transform.TransformPoint(moveTo);
                    }


                    #endregion


                    #region Double Axis

                    bool isDoubleAxis = (axisLockX == true && axisLockY == false && axisLockZ == false) || (axisLockX == false && axisLockY == true && axisLockZ == false) || (axisLockX == false && axisLockY == false && axisLockZ == true);

                    if (isDoubleAxis)
                    {

                        // DOUBLE AXIS
                        if (axisLockX == true && axisLockY == false && axisLockZ == false)
                            movePlane = new Plane(Vector3.Cross(transform.TransformDirection(Vector3.up), transform.TransformDirection(Vector3.forward)), transform.position);
                        else if (axisLockX == false && axisLockY == true && axisLockZ == false)
                            movePlane = new Plane(Vector3.Cross(transform.TransformDirection(Vector3.right), transform.TransformDirection(Vector3.forward)), transform.position);
                        else if (axisLockX == false && axisLockY == false && axisLockZ == true)
                            movePlane = new Plane(Vector3.Cross(transform.TransformDirection(Vector3.right), transform.TransformDirection(Vector3.up)), transform.position);

                        if (movePlane.Raycast(rayToNewPos, out float enter))
                        {
                            moveTo = rayToNewPos.origin + (rayToNewPos.direction * enter);
                        }
                    }

                    #endregion

                    Vector3 worldPos = (moveTo + guideObjectsOffsets[i]);

                    selectedGuideObjects[i].transformTarget.position = worldPos;
                    selectedGuideObjects[i].m_ChangeAmount.pos = selectedGuideObjects[i].transformTarget.localPosition;
                    selectedGuideObjects[i].m_ChangeAmount.onChangePos?.Invoke();
                    selectedGuideObjects[i].m_ChangeAmount.onChangePosAfter?.Invoke();
                }
            }
            #if KK
            else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
            {
                var window = MakerGuideObjectManager.customAcsMoveWindow;
                if (window == null) return;

                Transform transform = window.chaCtrl._objAcsMove[window.nSlotNo, window.correctNo].transform;
                Vector3 moveTo = Vector3.zero;

                #region Single Axis

                bool isSingleAxis = (axisLockX == false && axisLockY == true && axisLockZ == true) || (axisLockX == true && axisLockY == false && axisLockZ == true) || (axisLockX == true && axisLockY == true && axisLockZ == false);

                if (isSingleAxis)
                {

                    Vector3 camViewPlaneNormalLocal = transform.InverseTransformDirection(planeVector);

                    Ray rayToNewPosLocal = new Ray();
                    rayToNewPosLocal.origin = transform.InverseTransformPoint(rayToNewPos.origin);
                    rayToNewPosLocal.direction = transform.InverseTransformDirection(rayToNewPos.direction);

                    if (axisLockX == false && axisLockY == true && axisLockZ == true)
                    {
                        camViewPlaneNormalLocal.x = 0;
                        //planeVector = planeVector.normalized;
                    }
                    else if (axisLockX == true && axisLockY == false && axisLockZ == true)
                    {
                        camViewPlaneNormalLocal.y = 0;
                        //planeVector = planeVector.normalized;
                    }
                    else if (axisLockX == true && axisLockY == true && axisLockZ == false)
                    {
                        camViewPlaneNormalLocal.z = 0;
                        //planeVector = planeVector.normalized;
                    }

                    Plane localMovePlane = new Plane(camViewPlaneNormalLocal, Vector3.zero);

                    if (localMovePlane.Raycast(rayToNewPosLocal, out float distance))
                    {
                        Vector3 contactPoint = rayToNewPosLocal.origin + (rayToNewPosLocal.direction * distance);

                        if (axisLockX == false && axisLockY == true && axisLockZ == true)
                            moveTo.x = contactPoint.x;
                        else if (axisLockX == true && axisLockY == false && axisLockZ == true)
                            moveTo.y = contactPoint.y;
                        else if (axisLockX == true && axisLockY == true && axisLockZ == false)
                            moveTo.z = contactPoint.z;
                    }

                    // Convert back to global space.
                    moveTo = transform.TransformPoint(moveTo);
                }


                #endregion


                #region Double Axis

                bool isDoubleAxis = (axisLockX == true && axisLockY == false && axisLockZ == false) || (axisLockX == false && axisLockY == true && axisLockZ == false) || (axisLockX == false && axisLockY == false && axisLockZ == true);

                if (isDoubleAxis)
                {

                    // DOUBLE AXIS
                    if (axisLockX == true && axisLockY == false && axisLockZ == false)
                        movePlane = new Plane(Vector3.Cross(transform.TransformDirection(Vector3.up), transform.TransformDirection(Vector3.forward)), transform.position);
                    else if (axisLockX == false && axisLockY == true && axisLockZ == false)
                        movePlane = new Plane(Vector3.Cross(transform.TransformDirection(Vector3.right), transform.TransformDirection(Vector3.forward)), transform.position);
                    else if (axisLockX == false && axisLockY == false && axisLockZ == true)
                        movePlane = new Plane(Vector3.Cross(transform.TransformDirection(Vector3.right), transform.TransformDirection(Vector3.up)), transform.position);

                    if (movePlane.Raycast(rayToNewPos, out float enter))
                    {
                        moveTo = rayToNewPos.origin + (rayToNewPos.direction * enter);
                    }
                }

                #endregion

                Vector3 worldPos = (moveTo);

                window.cvsAccessory[window.nSlotNo].FuncUpdateAcsPosAdd(window.correctNo, 0, false, worldPos.x);
                window.cvsAccessory[window.nSlotNo].FuncUpdateAcsPosAdd(window.correctNo, 1, false, worldPos.y);
                window.cvsAccessory[window.nSlotNo].FuncUpdateAcsPosAdd(window.correctNo, 2, false, worldPos.z);
            }
            #endif
        }

        public static void ConfirmTranslation()
        {
            if (!translateMode) return;

            if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
            {
                if (_oldPosValues.Count != 0)
                {
                    GuideObjectTransformInfo[] translateCommands = new GuideObjectTransformInfo[_oldPosValues.Count];
                    int i = 0;
                    foreach (var kvp in _oldPosValues)
                    {
                        if (kvp.Key == null) continue;

                        translateCommands[i] = new GuideObjectTransformInfo(kvp.Key, kvp.Value, kvp.Key.transformTarget.position);
                        ++i;
                    }

                    if (translateCommands.Length != 0)
                        UndoRedoManager.Instance.Push(new TransformCommand(translateCommands, null, null));

                    _oldPosValues.Clear();
                }
            }

            translateMode = false;
        }

        private static void CancelTranslation()
        {
            if (!translateMode) return;

            if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
            {
                foreach (var kvp in _oldPosValues)
                {
                    if (kvp.Key == null || kvp.Key.enablePos == false) continue;
                    kvp.Key.transformTarget.position = kvp.Value;
                    kvp.Key.changeAmount.m_Pos = kvp.Key.transformTarget.localPosition;
                }

                _oldPosValues.Clear();
            }
            #if KK
            else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
            {
                var window = MakerGuideObjectManager.customAcsMoveWindow;
                if (window != null)
                {
                    window.cvsAccessory[window.nSlotNo].FuncUpdateAcsPosAdd(window.correctNo, 0, false, _makerAccsOldPos.x);
                    window.cvsAccessory[window.nSlotNo].FuncUpdateAcsPosAdd(window.correctNo, 1, false, _makerAccsOldPos.y);
                    window.cvsAccessory[window.nSlotNo].FuncUpdateAcsPosAdd(window.correctNo, 2, false, _makerAccsOldPos.z);
                }
            }
            #endif

            translateMode = false;
        }

#endregion

        #region ROTATION

        public static void StartRotation()
        {
            if (rotateMode) return;

            CancelTranslation();
            CancelRotation();
            CancelScale();

            if (camera == null) camera = Camera.main;

            cursorPosition = Input.mousePosition;
            originalMousePos = cursorPosition;
            originalAvgPos = GetGuideObjectsCenter();

            if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
            {
                selectedGuideObjects = Singleton<GuideObjectManager>.Instance.selectObjects;
                if (selectedGuideObjects == null || selectedGuideObjects.Length == 0) return;

                _oldPosValues.Clear();
                _oldRotValues.Clear();
                _oldPosValues = selectedGuideObjects.ToDictionary(x => x, x => x.transformTarget.position);
                _oldRotValues = selectedGuideObjects.ToDictionary(x => x.dicKey, x => x.changeAmount.rot);
            }
            #if KK
            else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
            {
                var window = MakerGuideObjectManager.customAcsMoveWindow;
                if (window != null && window.tglReference.isOn)
                {
                    _makerAccsOldPos = window.accessory.parts[window.nSlotNo].addMove[window.correctNo, 0];
                    _makerAccsOldRot = window.accessory.parts[window.nSlotNo].addMove[window.correctNo, 1];
                }
                else
                    return;
            }
            #endif

            rotateMode = true;
        }

        public static void UpdateRotation()
        {
            if (!rotateMode) return;

            Vector2 mousePos = cursorPosition;
            Vector2 toNewMouse = mousePos - originalMousePos;

            if (toNewMouse.sqrMagnitude == 0) return;

            Vector2 inSP = camera.WorldToScreenPoint(originalAvgPos);

            float angle = Vector2.Angle(originalMousePos - inSP, mousePos - inSP);
            Vector3 axis = Vector3.zero;

            if (Vector3.Cross(originalMousePos - inSP, mousePos - inSP).z < 0)
            {
                angle = 360 - angle;
            }

            
            Vector3 toCam = camera.transform.position - originalAvgPos;


            // SINGLE AND DOUBLE AXIS
            if ((axisLockX == false && axisLockY == true && axisLockZ == true) || (axisLockX == true && axisLockY == false && axisLockZ == false))
            {
                if (toCam.x < 0)
                {
                    axis = Vector3.right;
                }
                else
                {
                    axis = Vector3.left;
                }
            }
            else if ((axisLockX == true && axisLockY == false && axisLockZ == true) || (axisLockX == false && axisLockY == true && axisLockZ == false))
            {
                if (toCam.y < 0)
                {
                    axis = Vector3.up;
                }
                else
                {
                    axis = Vector3.down;
                }
            }
            else if ((axisLockX == true && axisLockY == true && axisLockZ == false) || (axisLockX == false && axisLockY == false && axisLockZ == true))
            {
                if (toCam.z < 0)
                {
                    axis = Vector3.forward;
                }
                else
                {
                    axis = Vector3.back;
                }
            }
            else
            {
                // FREE AXIS
                axis = camera.ScreenPointToRay(inSP).direction;
            }


            RotateAllGuideObjects(originalAvgPos, axis, angle);       

            originalMousePos = mousePos;
        }

        public static void UpdateLocalRotation(GizmoTransform targetObject)
        {
            if (!rotateMode) return;

            Vector2 mousePos = cursorPosition;
            Vector2 toNewMouse = mousePos - originalMousePos;

            if (toNewMouse.sqrMagnitude == 0) return;

            Vector2 inSP = camera.WorldToScreenPoint(originalAvgPos);

            float angle = Vector2.Angle(originalMousePos - inSP, mousePos - inSP);
            Vector3 axis = Vector3.zero;

            if (Vector3.Cross(originalMousePos - inSP, mousePos - inSP).z < 0)
            {
                angle = 360 - angle;
            }


            Vector3 toCam = camera.transform.position - originalAvgPos;


            // SINGLE AND DOUBLE AXIS
            if ((axisLockX == false && axisLockY == true && axisLockZ == true) || (axisLockX == true && axisLockY == false && axisLockZ == false))
            {
                if (toCam.x < 0)
                {
                    axis = targetObject.Right;
                }
                else
                {
                    axis = targetObject.Left;

                }
            }
            else if ((axisLockX == true && axisLockY == false && axisLockZ == true) || (axisLockX == false && axisLockY == true && axisLockZ == false))
            {
                if (toCam.y < 0)
                {
                    axis = targetObject.Up;
                }
                else
                {
                    axis = targetObject.Down;
                }
            }
            else if ((axisLockX == true && axisLockY == true && axisLockZ == false) || (axisLockX == false && axisLockY == false && axisLockZ == true))
            {
                if (toCam.z < 0)
                {
                    axis = targetObject.Forward;
                }
                else
                {
                    axis = targetObject.Back;
                }
            }
            else
            {
                // FREE AXIS
                axis = -camera.ScreenPointToRay(inSP).direction;
            }


            RotateAllGuideObjects(originalAvgPos, axis, angle);

            originalMousePos = mousePos;
        }

        public static void ConfirmRotation()
        {
            if (!rotateMode) return;

            if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
            {
                if (_oldPosValues.Count != 0 || _oldRotValues.Count != 0)
                {
                    GuideObjectTransformInfo[] translateCommands = new GuideObjectTransformInfo[_oldPosValues.Count];
                    int i = 0;
                    foreach (var kvp in _oldPosValues)
                    {
                        if (kvp.Key == null) continue;

                        translateCommands[i] = new GuideObjectTransformInfo(kvp.Key, kvp.Value, kvp.Key.transformTarget.position);
                        ++i;
                    }
                    GuideCommand.EqualsInfo[] rotateCommands = new GuideCommand.EqualsInfo[_oldRotValues.Count];
                    i = 0;
                    foreach (KeyValuePair<int, Vector3> kvp in _oldRotValues)
                    {
                        if (!Studio.Studio.Instance.dicChangeAmount.ContainsKey(kvp.Key)) continue;
                        rotateCommands[i] = new GuideCommand.EqualsInfo()
                        {
                            dicKey = kvp.Key,
                            oldValue = kvp.Value,
                            newValue = Studio.Studio.Instance.dicChangeAmount[kvp.Key].rot
                        };
                        ++i;
                    }

                    if (translateCommands.Length != 0 || rotateCommands.Length != 0)
                        UndoRedoManager.Instance.Push(new TransformCommand(translateCommands, rotateCommands, null));
                    _oldPosValues.Clear();
                    _oldRotValues.Clear();
                }
            }

            rotateMode = false;
        }

        public static void CancelRotation()
        {
            if (!rotateMode) return;

            if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
            {
                foreach (var kvp in _oldPosValues)
                {
                    if (kvp.Key == null || kvp.Key.enablePos == false) continue;
                    kvp.Key.transformTarget.position = kvp.Value;
                    kvp.Key.changeAmount.m_Pos = kvp.Key.transformTarget.localPosition;
                }

                foreach (KeyValuePair<int, Vector3> kvp in _oldRotValues)
                {
                    if (!Studio.Studio.Instance.dicChangeAmount.ContainsKey(kvp.Key)) continue;
                    Studio.Studio.Instance.dicChangeAmount[kvp.Key].rot = kvp.Value;
                }

                _oldPosValues.Clear();
                _oldRotValues.Clear();
            }
            #if KK
            else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
            {
                var window = MakerGuideObjectManager.customAcsMoveWindow;
                if (window != null)
                {
                    window.cvsAccessory[window.nSlotNo].FuncUpdateAcsPosAdd(window.nSlotNo, 0, false, _makerAccsOldPos.x);
                    window.cvsAccessory[window.nSlotNo].FuncUpdateAcsPosAdd(window.nSlotNo, 1, false, _makerAccsOldPos.y);
                    window.cvsAccessory[window.nSlotNo].FuncUpdateAcsPosAdd(window.nSlotNo, 2, false, _makerAccsOldPos.z);

                    window.cvsAccessory[window.nSlotNo].FuncUpdateAcsRotAdd(window.nSlotNo, 0, false, _makerAccsOldRot.x);
                    window.cvsAccessory[window.nSlotNo].FuncUpdateAcsRotAdd(window.nSlotNo, 1, false, _makerAccsOldRot.y);
                    window.cvsAccessory[window.nSlotNo].FuncUpdateAcsRotAdd(window.nSlotNo, 2, false, _makerAccsOldRot.z);
                }
            }
            #endif

            rotateMode = false;
        }

#endregion

        #region SCALE

        public static void StartScale()
        {
            if (scaleMode) return;

            CancelTranslation();
            CancelRotation();
            CancelScale();

            if (camera == null) camera = Camera.main;

            cursorPosition = Input.mousePosition;
            originalMousePos = cursorPosition;
            originalAvgPos = GetGuideObjectsCenter();
            originalDistance = Vector2.Distance(cursorPosition, camera.WorldToScreenPoint(originalAvgPos));

            if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
            {
                selectedGuideObjects = Singleton<GuideObjectManager>.Instance.selectObjects;
                if (selectedGuideObjects == null || selectedGuideObjects.Length == 0) return;

                _oldScaleValues.Clear();
                _oldPosValues.Clear();
                _oldScaleValues = selectedGuideObjects.ToDictionary(x => x.dicKey, x => x.changeAmount.scale);
                _oldPosValues = selectedGuideObjects.ToDictionary(x => x, x => x.transformTarget.position);
            }
            #if KK
            else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
            {
                var window = MakerGuideObjectManager.customAcsMoveWindow;
                if (window != null && window.tglReference.isOn)
                {
                    _makerAccsOldPos = window.accessory.parts[window.nSlotNo].addMove[window.correctNo, 0];
                    _makerAccsOldScale = window.accessory.parts[window.nSlotNo].addMove[window.correctNo, 2];
                }
                else
                    return;
            }
            #endif

            scaleMode = true;   
        }

        public static void UpdateScale()
        {
            if (!scaleMode) return;

            Vector2 mousePos = cursorPosition;
            Vector3 scaleBy = Vector3.one;
            Vector2 avgInSP = camera.WorldToScreenPoint(originalAvgPos);

            float newDistance = Vector2.Distance(mousePos, avgInSP);

            if (newDistance - originalDistance == 0) return;
            
            float scaleFactor = newDistance / originalDistance;

            scaleBy *= scaleFactor;

            // SINGLE AXIS
            if (axisLockX == false && axisLockY == true && axisLockZ == true)
            {
                scaleBy.x *= scaleFactor;
            }
            else if (axisLockX == true && axisLockY == false && axisLockZ == true)
            {
                scaleBy.y *= scaleFactor;

            }
            else if (axisLockX == true && axisLockY == true && axisLockZ == false)
            {
                scaleBy.z *= scaleFactor;
            }
            //

            // DOUBLE AXIS
            if (axisLockX == true && axisLockY == false && axisLockZ == false)
            {
                scaleBy.x = 1;
            }
            else if (axisLockX == false && axisLockY == true && axisLockZ == false)
            {
                scaleBy.y = 1;
            }
            else if (axisLockX == false && axisLockY == false && axisLockZ == true)
            {
                scaleBy.z = 1;
            }
            //

            ScaleAllGuideObjects(scaleBy);

            originalDistance = Vector2.Distance(cursorPosition, camera.WorldToScreenPoint(originalAvgPos));
        }

        public static void ConfirmScale()
        {
            if (!scaleMode) return;

            if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
            {
                if (_oldScaleValues.Count != 0)
                {
                    GuideCommand.EqualsInfo[] scaleCommands = new GuideCommand.EqualsInfo[_oldScaleValues.Count];
                    int i = 0;
                    foreach (KeyValuePair<int, Vector3> kvp in _oldScaleValues)
                    {
                        if (!Studio.Studio.Instance.dicChangeAmount.ContainsKey(kvp.Key)) continue;
                        scaleCommands[i] = new GuideCommand.EqualsInfo()
                        {
                            dicKey = kvp.Key,
                            oldValue = kvp.Value,
                            newValue = Studio.Studio.Instance.dicChangeAmount[kvp.Key].scale
                        };
                        ++i;
                    }
                    GuideObjectTransformInfo[] translateCommands = new GuideObjectTransformInfo[_oldPosValues.Count];
                    i = 0;
                    foreach (var kvp in _oldPosValues)
                    {
                        if (kvp.Key == null) continue;

                        translateCommands[i] = new GuideObjectTransformInfo(kvp.Key, kvp.Value, kvp.Key.transformTarget.position);
                        ++i;
                    }

                    if (translateCommands.Length != 0 || scaleCommands.Length != 0)
                        UndoRedoManager.Instance.Push(new TransformCommand(translateCommands, null, scaleCommands));
                    _oldScaleValues.Clear();
                    _oldPosValues.Clear();
                }
            }

            scaleMode = false;
        }

        public static void CancelScale()
        {
            if (!scaleMode) return;

            if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
            {
                foreach (var kvp in _oldPosValues)
                {
                    if (kvp.Key == null || kvp.Key.enablePos == false) continue;
                    kvp.Key.transformTarget.position = kvp.Value;
                    kvp.Key.changeAmount.m_Pos = kvp.Key.transformTarget.localPosition;
                }

                foreach (var kvp in _oldScaleValues)
                {
                    if (!Studio.Studio.Instance.dicChangeAmount.ContainsKey(kvp.Key)) continue;
                    Studio.Studio.Instance.dicChangeAmount[kvp.Key].scale = kvp.Value;
                }

                _oldPosValues.Clear();
                _oldScaleValues.Clear();
            }
            #if KK
            else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
            {
                var window = MakerGuideObjectManager.customAcsMoveWindow;
                if (window != null)
                {
                    window.cvsAccessory[window.nSlotNo].FuncUpdateAcsPosAdd(window.nSlotNo, 0, false, _makerAccsOldPos.x);
                    window.cvsAccessory[window.nSlotNo].FuncUpdateAcsPosAdd(window.nSlotNo, 1, false, _makerAccsOldPos.y);
                    window.cvsAccessory[window.nSlotNo].FuncUpdateAcsPosAdd(window.nSlotNo, 2, false, _makerAccsOldPos.z);

                    window.cvsAccessory[window.nSlotNo].FuncUpdateAcsSclAdd(window.nSlotNo, 0, false, _makerAccsOldScale.x);
                    window.cvsAccessory[window.nSlotNo].FuncUpdateAcsSclAdd(window.nSlotNo, 1, false, _makerAccsOldScale.y);
                    window.cvsAccessory[window.nSlotNo].FuncUpdateAcsSclAdd(window.nSlotNo, 2, false, _makerAccsOldScale.z);
                }
            }
            #endif

            scaleMode = false;
        }

#endregion

        private static void LockAxis()
        {
            if (!translateMode && !rotateMode && !scaleMode) return;

            if (Input.GetKeyDown(KeyCode.X))
            {
                if (rotateMode)
                {
                    if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
                    {
                        foreach (var kvp in _oldPosValues)
                        {
                            if (kvp.Key == null) continue;
                            kvp.Key.transformTarget.position = kvp.Value;
                            kvp.Key.changeAmount.m_Pos = kvp.Key.transformTarget.localPosition;
                        }

                        foreach (KeyValuePair<int, Vector3> kvp in _oldRotValues)
                        {
                            if (!Studio.Studio.Instance.dicChangeAmount.ContainsKey(kvp.Key)) continue;
                            Studio.Studio.Instance.dicChangeAmount[kvp.Key].rot = kvp.Value;
                        }
                    }
                }


                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    if (axisLockX == true && axisLockY == false && axisLockZ == false)
                    {
                        ResetAxisLock();
                    }
                    else
                    {
                        axisLockX = true;
                        axisLockY = false;
                        axisLockZ = false;
                    }
                }
                else
                {
                    if (axisLockX == false && axisLockY == true && axisLockZ == true)
                    {
                        ResetAxisLock();
                    }
                    else
                    {
                        axisLockX = false;
                        axisLockY = true;
                        axisLockZ = true;
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Y))
            {
                if (rotateMode)
                {
                    if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
                    {
                        foreach (var kvp in _oldPosValues)
                        {
                            if (kvp.Key == null) continue;
                            kvp.Key.transformTarget.position = kvp.Value;
                            kvp.Key.changeAmount.m_Pos = kvp.Key.transformTarget.localPosition;
                        }

                        foreach (KeyValuePair<int, Vector3> kvp in _oldRotValues)
                        {
                            if (!Studio.Studio.Instance.dicChangeAmount.ContainsKey(kvp.Key)) continue;
                            Studio.Studio.Instance.dicChangeAmount[kvp.Key].rot = kvp.Value;
                        }
                    }
                }
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    if (axisLockX == false && axisLockY == true && axisLockZ == false)
                    {
                        ResetAxisLock();
                    }
                    else
                    {
                        axisLockX = false;
                        axisLockY = true;
                        axisLockZ = false;
                    }
                }
                else
                {
                    if (axisLockX == true && axisLockY == false && axisLockZ == true)
                    {
                        ResetAxisLock();
                    }
                    else
                    {
                        axisLockX = true;
                        axisLockY = false;
                        axisLockZ = true;
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Z))
            {
                if (rotateMode)
                {
                    if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
                    {
                        foreach (var kvp in _oldPosValues)
                        {
                            if (kvp.Key == null) continue;
                            kvp.Key.transformTarget.position = kvp.Value;
                            kvp.Key.changeAmount.m_Pos = kvp.Key.transformTarget.localPosition;
                        }

                        foreach (KeyValuePair<int, Vector3> kvp in _oldRotValues)
                        {
                            if (!Studio.Studio.Instance.dicChangeAmount.ContainsKey(kvp.Key)) continue;
                            Studio.Studio.Instance.dicChangeAmount[kvp.Key].rot = kvp.Value;
                        }
                    }
                }
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    if (axisLockX == false && axisLockY == false && axisLockZ == true)
                    {
                        ResetAxisLock();
                    }
                    else
                    {
                        axisLockX = false;
                        axisLockY = false;
                        axisLockZ = true;
                    }
                }
                else
                {
                    if (axisLockX == true && axisLockY == true && axisLockZ == false)
                    {
                        ResetAxisLock();
                    }
                    else
                    {
                        axisLockX = true;
                        axisLockY = true;
                        axisLockZ = false;
                    }
                }
            }
        }

        public static void ResetAxisLock()
        {
            axisLockX = false;
            axisLockY = false;
            axisLockZ = false;
        }

        private static Vector3 GetGuideObjectsCenter()
        {
            if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
            {
                GuideObject[] selectObjects = Singleton<GuideObjectManager>.Instance.selectObjects;
                if (selectObjects == null || selectObjects.Length == 0) return Vector3.zero;

                if (StudioGuideObjectManager._objectMoveGizmo.TransformPivot == GizmoObjectTransformPivot.ObjectMeshPivot)
                {
                    if (StudioGuideObjectManager._workGizmo._targetPivotObject != null && StudioGuideObjectManager._workGizmo._targetPivotObject.transformTarget != null)
                        return StudioGuideObjectManager._workGizmo._targetPivotObject.transformTarget.position;
                }

                List<Vector3> posList = new List<Vector3>();

                foreach (GuideObject go in selectObjects)
                    posList.Add(go.transformTarget.position);

                Vector3 sumPos = Vector3.zero;

                foreach (Vector3 vector in posList)
                    sumPos += vector;

                Vector3 centerPos = sumPos / posList.Count;

                return centerPos;
            }

            else
            {
                #if KK
                var window = MakerGuideObjectManager.customAcsMoveWindow;
                if (window != null && window.tglReference.isOn)
                    return window.accessory.parts[window.nSlotNo].addMove[window.correctNo, 0];
                else
                    return Vector3.zero;
                #else
                return Vector3.zero;
                #endif
            }
        }

        private static void MoveAllGuideObjects(Vector3 position)
        {
            if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
            {
                for (int i = 0; i < selectedGuideObjects.Length; i++)
                {
                    if (selectedGuideObjects[i] == null) continue;

                    if (!selectedGuideObjects[i].enablePos) continue;

                    Vector3 worldPos = (position + guideObjectsOffsets[i]);

                    if (isSnapping)
                        worldPos = new Vector3(Mathf.Floor(worldPos.x / translateSnapIncrement.Value) * translateSnapIncrement.Value, Mathf.Floor(worldPos.y / translateSnapIncrement.Value) * translateSnapIncrement.Value, Mathf.Floor(worldPos.z / translateSnapIncrement.Value) * translateSnapIncrement.Value);

                    //Lock axis
                    if (axisLockX)
                        worldPos.x = _oldPosValues[selectedGuideObjects[i]].x;

                    if (axisLockY)
                        worldPos.y = _oldPosValues[selectedGuideObjects[i]].y;

                    if (axisLockZ)
                        worldPos.z = _oldPosValues[selectedGuideObjects[i]].z;

                    selectedGuideObjects[i].transformTarget.position = worldPos;
                    selectedGuideObjects[i].m_ChangeAmount.pos = selectedGuideObjects[i].transformTarget.localPosition;
                    selectedGuideObjects[i].m_ChangeAmount.onChangePos?.Invoke();
                    selectedGuideObjects[i].m_ChangeAmount.onChangePosAfter?.Invoke();
                }
            }
            #if KK
            else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
            {
                var window = MakerGuideObjectManager.customAcsMoveWindow;
                if (window != null && window.tglReference.isOn)
                {
                    Vector3 worldPos = position;

                    if (isSnapping)
                        worldPos = new Vector3(Mathf.Floor(worldPos.x / translateSnapIncrement.Value) * translateSnapIncrement.Value, Mathf.Floor(worldPos.y / translateSnapIncrement.Value) * translateSnapIncrement.Value, Mathf.Floor(worldPos.z / translateSnapIncrement.Value) * translateSnapIncrement.Value);

                    //Lock axis
                    if (axisLockX)
                        worldPos.x = _makerAccsOldPos.x;

                    if (axisLockY)
                        worldPos.y = _makerAccsOldPos.y;

                    if (axisLockZ)
                        worldPos.z = _makerAccsOldPos.z;

                    window.cvsAccessory[window.nSlotNo].FuncUpdateAcsPosAdd(window.correctNo, 0, false, worldPos.x);
                    window.cvsAccessory[window.nSlotNo].FuncUpdateAcsPosAdd(window.correctNo, 1, false, worldPos.y);
                    window.cvsAccessory[window.nSlotNo].FuncUpdateAcsPosAdd(window.correctNo, 2, false, worldPos.z);
                }
            }
            #endif
        }

        private static void RotateAllGuideObjects(Vector3 point, Vector3 axis, float angle)
        {
            Quaternion quaternion = Quaternion.AngleAxis(angle, axis);

            if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
            {
                for (int i = 0; i < selectedGuideObjects.Length; i++)
                {
                    if (selectedGuideObjects[i] == null) continue;
                    selectedGuideObjects[i].RotateAroundPivot(quaternion, point);
                }
            }
#if KK
            else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
            {
                var window = MakerGuideObjectManager.customAcsMoveWindow;
                if (window != null && window.tglReference.isOn)
                {
                    window.cvsAccessory[window.nSlotNo].FuncUpdateAcsRotAdd(window.correctNo, 0, false, quaternion.eulerAngles.x);
                    window.cvsAccessory[window.nSlotNo].FuncUpdateAcsRotAdd(window.correctNo, 1, false, quaternion.eulerAngles.y);
                    window.cvsAccessory[window.nSlotNo].FuncUpdateAcsRotAdd(window.correctNo, 2, false, quaternion.eulerAngles.z);
                }
            }
#endif
        }

        private static void ScaleAllGuideObjects(Vector3 scale)
        {
            Vector3 center = GetGuideObjectsCenter();

            if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
            {
                for (int i = 0; i < selectedGuideObjects.Length; i++)
                {
                    if (selectedGuideObjects[i] == null) continue;

                    if (selectedGuideObjects[i].enableScale)
                    {
                        Vector3 scaleVec = selectedGuideObjects[i].changeAmount.scale;

                        scaleVec.x *= scale.x;
                        scaleVec.y *= scale.y;
                        scaleVec.z *= scale.z;

                        if (axisLockX)
                            scaleVec.x = _oldScaleValues[selectedGuideObjects[i].dicKey].x;

                        if (axisLockY)
                            scaleVec.y = _oldScaleValues[selectedGuideObjects[i].dicKey].y;

                        if (axisLockZ)
                            scaleVec.z = _oldScaleValues[selectedGuideObjects[i].dicKey].z;


                        selectedGuideObjects[i].changeAmount.scale = scaleVec;
                        selectedGuideObjects[i].m_ChangeAmount.onChangeScale?.Invoke(scaleVec);
                    }

                    if (selectedGuideObjects[i].enablePos)
                    {
                        Vector3 direction = selectedGuideObjects[i].transformTarget.position - center;
                        float distance = direction.magnitude;

                        Vector3 worldPos = center + direction.normalized * distance * scale.x;

                        if (axisLockX)
                            worldPos.x = _oldPosValues[selectedGuideObjects[i]].x;

                        if (axisLockY)
                            worldPos.y = _oldPosValues[selectedGuideObjects[i]].y;

                        if (axisLockZ)
                            worldPos.z = _oldPosValues[selectedGuideObjects[i]].z;

                        selectedGuideObjects[i].changeAmount.pos = selectedGuideObjects[i].transformTarget.parent == null ? worldPos : selectedGuideObjects[i].transformTarget.parent.InverseTransformPoint(worldPos);
                        selectedGuideObjects[i].m_ChangeAmount.onChangePos?.Invoke();
                        selectedGuideObjects[i].m_ChangeAmount.onChangePosAfter?.Invoke();
                    }
                }
            }
            #if KK
            else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
            {
                var window = MakerGuideObjectManager.customAcsMoveWindow;
                if (window != null && window.tglReference.isOn)
                {
                    Vector3 scaleVec = window.accessory.parts[window.nSlotNo].addMove[window.correctNo, 2];

                    scaleVec.x *= scale.x;
                    scaleVec.y *= scale.y;
                    scaleVec.z *= scale.z;

                    if (axisLockX)
                        scaleVec.x = _makerAccsOldScale.x;

                    if (axisLockY)
                        scaleVec.y = _makerAccsOldScale.y;

                    if (axisLockZ)
                        scaleVec.z =_makerAccsOldScale.z;

                    window.cvsAccessory[window.nSlotNo].FuncUpdateAcsSclAdd(window.correctNo, 0, false, scaleVec.x);
                    window.cvsAccessory[window.nSlotNo].FuncUpdateAcsSclAdd(window.correctNo, 1, false, scaleVec.y);
                    window.cvsAccessory[window.nSlotNo].FuncUpdateAcsSclAdd(window.correctNo, 2, false, scaleVec.z);
                }
            }
            #endif

        }

    }

}
