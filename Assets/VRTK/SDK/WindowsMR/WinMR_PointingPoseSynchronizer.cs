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
            Anything,
            Left,
            Right,
        }
        public Side side;

        Quaternion initial_rot;
        Vector3 initial_pos;

        [SerializeField]
        Vector3 rel_pos;
        [SerializeField]
        Quaternion rel_rot;

        private void Start() {
            initial_rot = transform.localRotation;
            initial_pos = transform.localPosition;
        }

#if UNITY_WSA
        void Update() {
            var success = Synchronize();
            // 좌표 동기화에 성공하면 더이상 갱신할 필요가 없다
            if (success) {
                enabled = false;
            }
        }
#endif

#if UNITY_WSA
        bool IsMatchingHand(Side side, InteractionSourceHandedness hand) {
            switch(side) {
                case Side.Anything:
                    return true;
                case Side.Left:
                    return hand == InteractionSourceHandedness.Left;
                case Side.Right:
                    return hand == InteractionSourceHandedness.Right;
                default:
                    return false;
            }
        }
#endif

        public bool Synchronize() {
#if UNITY_WSA
            foreach (var sourceState in InteractionManager.GetCurrentReading()) {
                if (sourceState.source.kind != InteractionSourceKind.Controller) {
                    continue;
                }
                if(!IsMatchingHand(side, sourceState.source.handedness)) {
                    continue;
                }

                // transform.localPosition assign attempt for 'RightController' is not valid. Input localPosition is { 0.000000, NaN, 0.000000 }.
                // transform.localRotation assign attempt for 'RightController' is not valid. Input rotation is { 0.000000, NaN, 0.000000, NaN }.
                // 컨트롤러의 값을 갖고왔다고 항상 믿을수 있는게 아니더라
                // 이 에러가 나중에 고쳐질거같진한데 언제 고쳐질지 믿을수없다
                // 게다가 좌표 동기화는 한번밖에 안하니
                // 진짜로 작동하는 순간에 잘못된 값이 들어오면 골치아프다
                // 값 검증을 거치자

                Vector3 grip_pos;
                if(!sourceState.sourcePose.TryGetPosition(out grip_pos, InteractionSourceNode.Grip)) {
                    continue;
                }
                if (!WinMR_MathHelper.IsValidVector(grip_pos)) { continue; }

                Quaternion grip_rot;
                if (!sourceState.sourcePose.TryGetRotation(out grip_rot, InteractionSourceNode.Grip)) {
                    continue;
                }
                if(!WinMR_MathHelper.IsValidQuaternion(grip_rot)) { continue; }


                Vector3 pointing_pos;
                if (!sourceState.sourcePose.TryGetPosition(out pointing_pos, InteractionSourceNode.Pointer)) {
                    continue;
                }
                if(!WinMR_MathHelper.IsValidVector(pointing_pos)) { continue; }

                Quaternion pointing_rot;
                if (!sourceState.sourcePose.TryGetRotation(out pointing_rot, InteractionSourceNode.Pointer)) {
                    continue;
                }
                if(!WinMR_MathHelper.IsValidQuaternion(pointing_rot)) { continue; }


                var mat_grip = Matrix4x4.TRS(grip_pos, grip_rot, Vector3.one);
                var mat_pointing = Matrix4x4.TRS(pointing_pos, pointing_rot, Vector3.one);

                // 컨트롤러의 자식으로 pointing을 갖고싶다
                // mat_grip * mat_unknown = mat_pointing
                // mat_unknown = inv_mat_grip * mat_pointing
                var m = mat_grip.inverse * mat_pointing;
                WinMR_MathHelper.ApplyMatrix(transform, m);

                rel_pos = transform.localPosition;
                rel_rot = transform.localRotation;

                // 최초 상태를 적용
                var q = rel_rot * initial_rot;
                transform.localRotation = q;

                var diff = transform.localRotation * initial_pos;
                transform.localPosition += diff;

                return true;
            }
            return false;
#else
            return false;
#endif
        }
    }
}
