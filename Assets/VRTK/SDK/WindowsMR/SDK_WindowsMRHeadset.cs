using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
#if VRTK_DEFINE_SDK_WINDOWSMR
using HoloToolkit.Unity.InputModule;
#endif

namespace VRTK {
    [SDK_Description(typeof(SDK_WindowsMRSystem))]
    public class SDK_WindowsMRHeadset
#if VRTK_DEFINE_SDK_WINDOWSMR
        : SDK_BaseHeadset
#else
        : SDK_FallbackHeadset 
#endif
        {
#if VRTK_DEFINE_SDK_WINDOWSMR
        private Quaternion previousHeadsetRotation;
        private Quaternion currentHeadsetRotation;

        public override void AddHeadsetFade(Transform camera) {
            if (camera && !camera.GetComponent<VRTK_ScreenFade>()) {
                camera.gameObject.AddComponent<VRTK_ScreenFade>();
            }
        }

        public override Transform GetHeadset() {
            cachedHeadset = GetSDKManagerHeadset();
            if(cachedHeadset == null) {
                cachedHeadset = VRTK_SharedMethods.FindEvenInactiveGameObject<MixedRealityCameraManager>().transform;
            }
            return cachedHeadset;
        }

        public override Vector3 GetHeadsetAngularVelocity() {
            var deltaRotation = currentHeadsetRotation * Quaternion.Inverse(previousHeadsetRotation);
            return new Vector3(Mathf.DeltaAngle(0, deltaRotation.eulerAngles.x), Mathf.DeltaAngle(0, deltaRotation.eulerAngles.y), Mathf.DeltaAngle(0, deltaRotation.eulerAngles.z));
        }

        public override Transform GetHeadsetCamera() {
            cachedHeadsetCamera = GetSDKManagerHeadset();
            if(cachedHeadsetCamera == null) {
                cachedHeadsetCamera = GetHeadset();
            }
            return cachedHeadsetCamera;
        }

        public override Vector3 GetHeadsetVelocity() {
            // TODO 속도 어떻게 얻지?
            return Vector3.zero;
        }

        public override bool HasHeadsetFade(Transform obj) {
            // oculus 구현을 선택
            // TODO FadeScript라는게 HoloToolkit에 있다. 이것을 기반으로 다시 구현하면 될거같기도한데
            // 어차피 vrtk기반 fade를 안쓰니까 상관없기도 하고
            if(obj.GetComponentInChildren<VRTK_ScreenFade>()) {
                return true;
            }
            return false;
        }

        public override void HeadsetFade(Color color, float duration, bool fadeOverlay = false) {
            VRTK_ScreenFade.Start(color, duration);
        }

        public override void ProcessFixedUpdate(Dictionary<string, object> options) {
            CalculateAngularVelocity();
        }

        public override void ProcessUpdate(Dictionary<string, object> options) {
            CalculateAngularVelocity();
        }

        private void CalculateAngularVelocity() {
            previousHeadsetRotation = currentHeadsetRotation;
            currentHeadsetRotation = GetHeadset().transform.rotation;
        }

#endif
    }
}
