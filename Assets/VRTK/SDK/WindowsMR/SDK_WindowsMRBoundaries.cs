using UnityEngine;
#if VRTK_DEFINE_SDK_WINDOWSMR
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.Boundary;
#endif

namespace VRTK {
    [SDK_Description(typeof(SDK_WindowsMRSystem))]
    public class SDK_WindowsMRBoundaries
#if VRTK_DEFINE_SDK_WINDOWSMR
        : SDK_BaseBoundaries
#else
        : SDK_FallbackBoundaries
#endif
        {
#if VRTK_DEFINE_SDK_WINDOWSMR

        protected BoundaryManager cachedBoundary;

        public override void InitBoundaries() {
        }

        public override Transform GetPlayArea() {
            cachedPlayArea = GetSDKManagerPlayArea();
            if(cachedPlayArea == null) {
                var comp = VRTK_SharedMethods.FindEvenInactiveComponent<MixedRealityTeleport>();
                if(comp != null) {
                    cachedPlayArea = comp.transform;
                }
            }
            return cachedPlayArea;
        }
        
        public override Vector3[] GetPlayAreaVertices(GameObject playArea) {
            // TODO 영역 잡는 법 더 괜찮은거 없나?
            // TODO boundary.BoundaryBounds를 public변수로 얻어올 방법은 없나?
            var boundary = GetCachedBoundaryManager();
            if(boundary != null) {
                var quad = boundary.FloorQuad;
                var meshfilter = quad.GetComponent<MeshFilter>();
                var mesh = meshfilter.sharedMesh;
                return mesh.vertices;
            }
            return null;
        }

        public override float GetPlayAreaBorderThickness(GameObject playArea) {
            return 0.1f;
        }

        public override bool IsPlayAreaSizeCalibrated(GameObject playArea) {
            // TODO oculus 버전처럼 간단하게 구현
            return true;
        }

        public override bool GetDrawAtRuntime() {
            var boundary = GetCachedBoundaryManager();
            return (boundary != null ? boundary.RenderBoundary : false);
        }

        public override void SetDrawAtRuntime(bool value) {
            var boundary = GetCachedBoundaryManager();
            if(boundary != null) {
                boundary.RenderBoundary = value;
                boundary.enabled = true;
            }
        }

        protected virtual BoundaryManager GetCachedBoundaryManager() {
            if(cachedBoundary == null) {
                var checkPlayArea = GetPlayArea();
                if(checkPlayArea != null) {
                    cachedBoundary = checkPlayArea.GetComponent<BoundaryManager>();
                }
            }
            return cachedBoundary;
        }
#endif
    }
}
