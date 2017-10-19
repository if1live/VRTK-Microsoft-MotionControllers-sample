﻿using UnityEngine;

namespace VRTK {
    class WinMR_MathHelper {
        static bool IsVectorHasNaN(Vector3 v) {
            for (var i = 0; i < 3; i++) {
                if (float.IsNaN(v[i])) {
                    return true;
                }
            }
            return false;
        }
        static bool IsQuaternionHasNaN(Quaternion q) {
            for (var i = 0; i < 4; i++) {
                if (float.IsNaN(q[i])) {
                    return true;
                }
            }
            return false;
        }

        public static bool IsValidVector(Vector3 v) {
            if (IsVectorHasNaN(v)) {
                return false;
            }
            return true;
        }
        public static bool IsValidQuaternion(Quaternion q) {
            if (IsQuaternionHasNaN(q)) {
                return false;
            }
            return true;
        }

        public static void ApplyMatrix(Transform transform, Matrix4x4 mat) {
            // http://answers.unity3d.com/answers/1134836/view.html
            transform.localPosition = mat.GetColumn(3);
            transform.localScale = new Vector3(mat.GetColumn(0).magnitude, mat.GetColumn(1).magnitude, mat.GetColumn(2).magnitude);
            float w = Mathf.Sqrt(1.0f + mat.m00 + mat.m11 + mat.m22) / 2.0f;
            transform.localRotation = new Quaternion((mat.m21 - mat.m12) / (4.0f * w), (mat.m02 - mat.m20) / (4.0f * w), (mat.m10 - mat.m01) / (4.0f * w), w);
        }
    }
}
