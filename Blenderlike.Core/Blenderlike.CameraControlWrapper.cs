using UnityEngine;

namespace Blenderlike
{
    public static class CameraControlWrapper
    {
        public static Vector3 TargetPos
        {
            get
            {
                if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
                {
                    return Studio.Studio.Instance.cameraCtrl.targetPos;
                }
                else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
                {
                    return KKAPI.Maker.MakerAPI.GetMakerBase().customCtrl.camCtrl.TargetPos;
                }

                return Vector3.zero;
            }

            set
            {
                if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
                {
                    Studio.Studio.Instance.cameraCtrl.targetPos = value;
                }
                else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
                {
                    KKAPI.Maker.MakerAPI.GetMakerBase().customCtrl.camCtrl.TargetPos = value;
                }
            }
        }

        public static Vector3 Pos
        {
            get
            {
                if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
                {
                    return Studio.Studio.Instance.cameraCtrl.cameraData.pos;
                }
                else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
                {
                    return KKAPI.Maker.MakerAPI.GetMakerBase().customCtrl.camCtrl.CamDat.Pos;
                }
                return Vector3.zero;
            }
            set
            {
                if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
                {
                    Studio.Studio.Instance.cameraCtrl.cameraData.pos = value;
                }
                else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
                {
                    KKAPI.Maker.MakerAPI.GetMakerBase().customCtrl.camCtrl.CamDat.Pos = value;
                }
            }
        }

        public static Vector3 Rot
        {
            get
            {
                if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
                {
                    return Studio.Studio.Instance.cameraCtrl.cameraData.rotate;
                }
                else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
                {
                    return KKAPI.Maker.MakerAPI.GetMakerBase().customCtrl.camCtrl.CamDat.Rot;
                }
                return Vector3.zero;
            }

            set
            {
                if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
                {
                    Studio.Studio.Instance.cameraCtrl.cameraData.rotate = value;
                }
                else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
                {
                    KKAPI.Maker.MakerAPI.GetMakerBase().customCtrl.camCtrl.CamDat.Rot = value;
                }
            }
        }

        public static Vector3 Dist
        {
            get
            {
                if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
                {
                    return Studio.Studio.Instance.cameraCtrl.cameraData.distance;
                }
                else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
                {
                    return KKAPI.Maker.MakerAPI.GetMakerBase().customCtrl.camCtrl.CamDat.Dir;
                }
                return Vector3.zero;
            }

            set
            {
                if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
                {
                    Studio.Studio.Instance.cameraCtrl.cameraData.distance = value;
                }
                else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
                {
                    KKAPI.Maker.MakerAPI.GetMakerBase().customCtrl.camCtrl.CamDat.Dir = value;
                }
            }
        }


        public static float FieldOfView
        {
            get
            {
                if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
                {
                    return Studio.Studio.Instance.cameraCtrl.fieldOfView;
                }
                else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
                {
                    return KKAPI.Maker.MakerAPI.GetMakerBase().customCtrl.camCtrl.CameraFov;
                }
                return 0f;
            }

            set
            {
                if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
                {
                    Studio.Studio.Instance.cameraCtrl.fieldOfView = value;
                }
                else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
                {
                    KKAPI.Maker.MakerAPI.GetMakerBase().customCtrl.camCtrl.CameraFov = value;
                }
            }
        }



        public static Transform CameraTransform
        {
            get
            {
                if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Studio)
                {
                    return Studio.Studio.Instance.cameraCtrl.mainCmaera.transform;
                }
#if KK
                else if (KKAPI.KoikatuAPI.GetCurrentGameMode() == KKAPI.GameMode.Maker)
                {
                    return KKAPI.Maker.MakerAPI.GetMakerBase().customCtrl.camCtrl.thisCmaera.transform;
                }
#endif
                return null;
            }
        }
    }
}
