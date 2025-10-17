using Studio;
using UniRx.Triggers;
using UnityEngine;

namespace Blenderlike
{
    public class TransformCommand : ICommand
    {
        private readonly GuideObjectTransformInfo[] _positionChangeAmountInfo;
        private readonly GuideCommand.EqualsInfo[] _rotationChangeAmountInfo;
        private readonly GuideCommand.EqualsInfo[] _scaleChangeAmountInfo;

        public TransformCommand(GuideObjectTransformInfo[] positionChangeAmountInfo, GuideCommand.EqualsInfo[] rotationChangeAmountInfo, GuideCommand.EqualsInfo[] scaleChangeAmountInfo)
        {
            _positionChangeAmountInfo = positionChangeAmountInfo;
            _rotationChangeAmountInfo = rotationChangeAmountInfo;
            _scaleChangeAmountInfo = scaleChangeAmountInfo;
        }

        public void Do()
        {
            var dic = Studio.Studio.Instance.dicChangeAmount;

            if (_positionChangeAmountInfo != null)
            {
                foreach (GuideObjectTransformInfo info in _positionChangeAmountInfo)
                {
                    if (info == null || info.guideObject == null || info.guideObject.changeAmount == null || info.guideObject.enablePos == false) continue;

                    info.guideObject.transformTarget.position = info.newValue;
                    info.guideObject.changeAmount.m_Pos = info.guideObject.transformTarget.localPosition;
                    info.guideObject.changeAmount.onChangePos?.Invoke();
                    info.guideObject.changeAmount.onChangePosAfter?.Invoke();
                }
            }

            if (_rotationChangeAmountInfo != null)
            {
                foreach (GuideCommand.EqualsInfo info in _rotationChangeAmountInfo)
                {
                    if (info == null || !dic.ContainsKey(info.dicKey)) continue;
                    ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(info.dicKey);
                    if (changeAmount != null)
                    {
                        changeAmount.rot = info.newValue;
                        changeAmount.onChangeRot?.Invoke();
                    }
                }
            }

            if (_scaleChangeAmountInfo != null)
            {
                foreach (GuideCommand.EqualsInfo info in _scaleChangeAmountInfo)
                {
                    if (info == null || !dic.ContainsKey(info.dicKey)) continue;
                    ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(info.dicKey);
                    if (changeAmount != null)
                    {
                        changeAmount.scale = info.newValue;
                        changeAmount.onChangeScale?.Invoke(changeAmount.scale);
                    }
                }
            }
        }

        public void Redo()
        {
            Do();
        }

        public void Undo()
        {
            var dic = Studio.Studio.Instance.dicChangeAmount;

            if (_positionChangeAmountInfo != null)
            {
                foreach (GuideObjectTransformInfo info in _positionChangeAmountInfo)
                {
                    if (info == null || info.guideObject == null || info.guideObject.changeAmount == null || info.guideObject.enablePos == false) continue;

                    info.guideObject.transformTarget.position = info.oldValue;
                    info.guideObject.changeAmount.m_Pos = info.guideObject.transformTarget.localPosition;
                    info.guideObject.changeAmount.onChangePos?.Invoke();
                    info.guideObject.changeAmount.onChangePosAfter?.Invoke();
                }
            }

            if (_rotationChangeAmountInfo != null)
            {
                foreach (GuideCommand.EqualsInfo info in _rotationChangeAmountInfo)
                {
                    if (info == null || !dic.ContainsKey(info.dicKey)) continue;
                    ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(info.dicKey);
                    if (changeAmount != null)
                    {
                        changeAmount.rot = info.oldValue;
                        changeAmount.onChangeRot?.Invoke();
                    }
                }
            }

            if (_scaleChangeAmountInfo != null)
            {
                foreach (GuideCommand.EqualsInfo info in _scaleChangeAmountInfo)
                {
                    if (info == null || !dic.ContainsKey(info.dicKey)) continue;
                    ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(info.dicKey);
                    if (changeAmount != null)
                    {
                        changeAmount.scale = info.oldValue;
                        changeAmount.onChangeScale?.Invoke(changeAmount.scale);
                    }
                }
            }
        }
    }

    public class GuideObjectTransformInfo
    {
        public GuideObject guideObject;

        public Vector3 oldValue;

        public Vector3 newValue;

        public GuideObjectTransformInfo(GuideObject guideObject, Vector3 oldValue, Vector3 newValue)
        {
            this.guideObject = guideObject;
            this.oldValue = oldValue;
            this.newValue = newValue;
        }
    }
}