using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_WSA
using UnityEngine.XR.WSA.Input;
#endif

namespace VRTK {
    /// <summary>
    /// windows MR 의 기능으로는 컨트롤러 grip절대좌표와 pointing절대좌표를 얻을수 있다
    /// 게임 객체 계층구조가 controller-pointer로 구성될 경우에는 절대좌표는 그냥 못쓰고
    /// 상대 좌표로 변환해야된다
    /// </summary>
    public class WinMR_PointingPoseSynchronizer : MonoBehaviour {
        public enum Side {
            Left,
            Right,
        }
        public Side side;


        void Update() {
            if(_synchronize == false) {
                _synchronize = Synchronize();
            }
        }

        bool _synchronize = false;

        bool Synchronize() {
#if UNITY_WSA
            foreach (var sourceState in InteractionManager.GetCurrentReading()) {
                if (sourceState.source.kind != InteractionSourceKind.Controller) {
                    continue;
                }

                var targetHand = (side == Side.Left) ? InteractionSourceHandedness.Left : InteractionSourceHandedness.Right;
                if(sourceState.source.handedness != targetHand) {
                    continue;
                }

                Vector3 grip_pos;
                if(!sourceState.sourcePose.TryGetPosition(out grip_pos, InteractionSourceNode.Grip)) {
                    continue;
                }
                Quaternion grip_rot;
                if (!sourceState.sourcePose.TryGetRotation(out grip_rot, InteractionSourceNode.Grip)) {
                    continue;
                }

                Vector3 pointing_pos;
                if (!sourceState.sourcePose.TryGetPosition(out pointing_pos, InteractionSourceNode.Pointer)) {
                    continue;
                }
                Quaternion pointing_rot;
                if (!sourceState.sourcePose.TryGetRotation(out pointing_rot, InteractionSourceNode.Pointer)) {
                    continue;
                }

                var mat_grip = Matrix4x4.TRS(grip_pos, grip_rot, Vector3.one);
                var mat_pointing = Matrix4x4.TRS(pointing_pos, pointing_rot, Vector3.one);

                // 컨트롤러의 자식으로 pointing을 갖고싶다
                // mat_grip * mat_unknown = mat_pointing
                // mat_unknown = inv_mat_grip * mat_pointing
                var m = mat_grip.inverse * mat_pointing;
                ApplyMatrix(m);
                return true;
            }
            return false;
#else
            return false;
#endif
        }

        void ApplyMatrix(Matrix4x4 mat) {
            // http://answers.unity3d.com/answers/1134836/view.html
            transform.localPosition = mat.GetColumn(3);
            transform.localScale = new Vector3(mat.GetColumn(0).magnitude, mat.GetColumn(1).magnitude, mat.GetColumn(2).magnitude);
            float w = Mathf.Sqrt(1.0f + mat.m00 + mat.m11 + mat.m22) / 2.0f;
            transform.localRotation = new Quaternion((mat.m21 - mat.m12) / (4.0f * w), (mat.m02 - mat.m20) / (4.0f * w), (mat.m10 - mat.m01) / (4.0f * w), w);
        }
    }
}
