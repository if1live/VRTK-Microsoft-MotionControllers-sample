using System.Collections.Generic;
using UnityEngine;
#if VRTK_DEFINE_SDK_WINDOWSMR
using UnityEngine.XR.WSA.Input;
#endif

namespace VRTK {
    [SDK_Description(typeof(SDK_WindowsMRSystem))]
    public class SDK_WindowsMRController
#if VRTK_DEFINE_SDK_WINDOWSMR
        : SDK_BaseController
#else
        : SDK_FallbackController 
#endif 
        {
#if VRTK_DEFINE_SDK_WINDOWSMR

        private VRTK_TrackedController cachedLeftController;
        private VRTK_TrackedController cachedRightController;

        protected const string RIGHT_HAND_CONTROLLER_NAME = "RightController";
        protected const string LEFT_HAND_CONTROLLER_NAME = "LeftController";

        /// <summary>
        /// UnityEngine.XR.WSA.Input의 값을 enum올 접근할수 있도록 하려고
        /// </summary>
        enum MotionControllerButtonTypes {
            /// <summary>
            /// select pressed 
            /// </summary>
            Trigger,

            /// <summary>
            /// menu pressed
            /// </summary>
            Menu,

            /// <summary>
            /// Grasped
            /// </summary>
            Grip,

            /// <summary>
            /// ThumbstickPressed
            /// </summary>
            Thumbstick,

            /// <summary>
            /// TouchpadPressed + TouchpadTouched
            /// </summary>
            Touchpad,

            ButtonOne,
            ButtonTwo,
        }

        /// <summary>
        /// HoloToolkit, DebugPanelControllerInfo에서 가져옴
        /// </summary>
        private class ControllerState {
            public InteractionSourceHandedness Handedness;
            public Vector3 PointerPosition;
            public Quaternion PointerRotation;
            public Vector3 GripPosition;
            public Quaternion GripRotation;
            public bool Grasped;
            public bool MenuPressed;
            public bool SelectPressed;
            public float SelectPressedAmount;
            public bool ThumbstickPressed;
            public Vector2 ThumbstickPosition;
            public bool TouchpadPressed;
            public bool TouchpadTouched;
            public Vector2 TouchpadPosition;
        }

        private Dictionary<uint, ControllerState> controllers;

        private void Awake() {
            controllers = new Dictionary<uint, ControllerState>();

            InteractionManager.InteractionSourceDetected += InteractionManager_InteractionSourceDetected;
            InteractionManager.InteractionSourceLost += InteractionManager_InteractionSourceLost;
            InteractionManager.InteractionSourceUpdated += InteractionManager_InteractionSourceUpdated;
        }

        private void OnDestroy() {
            InteractionManager.InteractionSourceDetected -= InteractionManager_InteractionSourceDetected;
            InteractionManager.InteractionSourceLost -= InteractionManager_InteractionSourceLost;
            InteractionManager.InteractionSourceUpdated -= InteractionManager_InteractionSourceUpdated;
        }

        private void InteractionManager_InteractionSourceDetected(InteractionSourceDetectedEventArgs obj) {
            Debug.LogFormat("{0} {1} Detected", obj.state.source.handedness, obj.state.source.kind);

            if (obj.state.source.kind == InteractionSourceKind.Controller && !controllers.ContainsKey(obj.state.source.id)) {
                controllers.Add(obj.state.source.id, new ControllerState { Handedness = obj.state.source.handedness });
            }
        }

        private void InteractionManager_InteractionSourceLost(InteractionSourceLostEventArgs obj) {
            Debug.LogFormat("{0} {1} Lost", obj.state.source.handedness, obj.state.source.kind);

            controllers.Remove(obj.state.source.id);
        }

        private void InteractionManager_InteractionSourceUpdated(InteractionSourceUpdatedEventArgs obj) {
            ControllerState controllerState;
            if (controllers.TryGetValue(obj.state.source.id, out controllerState)) {
                obj.state.sourcePose.TryGetPosition(out controllerState.PointerPosition, InteractionSourceNode.Pointer);
                obj.state.sourcePose.TryGetRotation(out controllerState.PointerRotation, InteractionSourceNode.Pointer);
                obj.state.sourcePose.TryGetPosition(out controllerState.GripPosition, InteractionSourceNode.Grip);
                obj.state.sourcePose.TryGetRotation(out controllerState.GripRotation, InteractionSourceNode.Grip);

                controllerState.Grasped = obj.state.grasped;
                controllerState.MenuPressed = obj.state.menuPressed;
                controllerState.SelectPressed = obj.state.selectPressed;
                controllerState.SelectPressedAmount = obj.state.selectPressedAmount;
                controllerState.ThumbstickPressed = obj.state.thumbstickPressed;
                controllerState.ThumbstickPosition = obj.state.thumbstickPosition;
                controllerState.TouchpadPressed = obj.state.touchpadPressed;
                controllerState.TouchpadTouched = obj.state.touchpadTouched;
                controllerState.TouchpadPosition = obj.state.touchpadPosition;
            }
        }        

        public override Transform GenerateControllerPointerOrigin(GameObject parent) {
            // oculus 구현을 가져옴
            return null;
        }

        public override Vector3 GetAngularVelocityOnIndex(uint index) {
            // TODO
            return Vector3.zero;
        }

        public override GameObject GetControllerByIndex(uint index, bool actual = false) {
            SetTrackedControllerCaches();
            var sdkManager = VRTK_SDKManager.instance;
            if (sdkManager != null) {
                if (cachedLeftController != null && cachedLeftController.index == index) {
                    return (actual ? sdkManager.actualLeftController : sdkManager.scriptAliasLeftController);
                }

                if (cachedRightController != null && cachedRightController.index == index) {
                    return (actual ? sdkManager.actualRightController : sdkManager.scriptAliasRightController);
                }
            }
            return null;
        }

        public override string GetControllerDefaultColliderPath(ControllerHand hand) {
            throw new System.NotImplementedException();
        }

        public override string GetControllerElementPath(ControllerElements element, ControllerHand hand, bool fullPath = false) {
            throw new System.NotImplementedException();
        }

        public override uint GetControllerIndex(GameObject controller) {
            VRTK_TrackedController trackedObject = GetTrackedObject(controller);
            return (trackedObject != null ? trackedObject.index : uint.MaxValue);
        }

        public override GameObject GetControllerLeftHand(bool actual = false) {
            var controller = GetSDKManagerControllerLeftHand(actual);
            if (!controller && actual) {
                controller = VRTK_SharedMethods.FindEvenInactiveGameObject<Transform>("MotionControllers/LeftController");
            }
            return controller;
        }

        public override GameObject GetControllerModel(GameObject controller) {
            return GetControllerModelFromController(controller);
        }

        public override GameObject GetControllerModel(ControllerHand hand) {
            var model = GetSDKManagerControllerModelForHand(hand);
            if (!model) {
                GameObject controller = null;
                switch (hand) {
                    case ControllerHand.Left:
                        controller = GetControllerLeftHand(true);
                        break;
                    case ControllerHand.Right:
                        controller = GetControllerRightHand(true);
                        break;
                }

                if (controller != null) {
                    model = (model != null && model.transform.childCount > 0 ? model.transform.GetChild(0).gameObject : null);
                }
            }
            return model;
        }

        public override Transform GetControllerOrigin(GameObject controller) {
            return VRTK_SDK_Bridge.GetPlayArea();
        }

        public override GameObject GetControllerRenderModel(GameObject controller) {
            // TODO ?
            return null;
        }

        public override GameObject GetControllerRightHand(bool actual = false) {
            var controller = GetSDKManagerControllerRightHand(actual);
            if (!controller && actual) {
                controller = VRTK_SharedMethods.FindEvenInactiveGameObject<Transform>("MotionControllers/RightController");
            }
            return controller;
        }

        public override Vector2 GetGripAxisOnIndex(uint index) {
            var controller = controllers[index];
            if(controller.Grasped) {
                return new Vector2(1, 0);
            } else {
                return Vector2.zero;
            }
        }

        public override float GetGripHairlineDeltaOnIndex(uint index) {
            // TODO ???
            return 0.0f;
        }

        public override SDK_ControllerHapticModifiers GetHapticModifiers() {
            var modifiers = new SDK_ControllerHapticModifiers();
            //modifiers.durationModifier = 0.8f;
            //modifiers.intervalModifier = 1f;
            return modifiers;
        }

        public override Vector2 GetTouchpadAxisOnIndex(uint index) {
            ControllerState state;
            var found = controllers.TryGetValue(index, out state);
            if (!found) {
                return Vector2.zero;
            }
            var v = state.TouchpadPosition;
            return v;
        }

        public override Vector2 GetTriggerAxisOnIndex(uint index) {
            ControllerState state;
            var found = controllers.TryGetValue(index, out state);
            if(!found) {
                return Vector2.zero;
            }
            var v = state.SelectPressedAmount;
            return new Vector2(v, 0);
        }

        public override float GetTriggerHairlineDeltaOnIndex(uint index) {
            // TODO ???
            return 0.0f;
        }

        public override Vector3 GetVelocityOnIndex(uint index) {
            // TODO 속도?
            return Vector3.zero;
        }

        public override void HapticPulseOnIndex(uint index, float strength = 0.5F) {
            // TODO 진동 구현
        }

        public override bool IsButtonOnePressedDownOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.PressDown, MotionControllerButtonTypes.ButtonOne);
        }

        public override bool IsButtonOnePressedOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.Press, MotionControllerButtonTypes.ButtonOne);
        }

        public override bool IsButtonOnePressedUpOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.PressUp, MotionControllerButtonTypes.ButtonOne);
        }

        public override bool IsButtonOneTouchedDownOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.TouchDown, MotionControllerButtonTypes.ButtonOne);
        }

        public override bool IsButtonOneTouchedOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.Touch, MotionControllerButtonTypes.ButtonOne);
        }

        public override bool IsButtonOneTouchedUpOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.TouchUp, MotionControllerButtonTypes.ButtonOne);
        }

        public override bool IsButtonTwoPressedDownOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.PressDown, MotionControllerButtonTypes.ButtonTwo);
        }

        public override bool IsButtonTwoPressedOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.Press, MotionControllerButtonTypes.ButtonTwo);
        }

        public override bool IsButtonTwoPressedUpOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.PressUp, MotionControllerButtonTypes.ButtonTwo);
        }

        public override bool IsButtonTwoTouchedDownOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.TouchDown, MotionControllerButtonTypes.ButtonTwo);
        }

        public override bool IsButtonTwoTouchedOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.Touch, MotionControllerButtonTypes.ButtonTwo);
        }

        public override bool IsButtonTwoTouchedUpOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.TouchUp, MotionControllerButtonTypes.ButtonTwo);
        }

        public override bool IsControllerLeftHand(GameObject controller) {
            return CheckActualOrScriptAliasControllerIsLeftHand(controller);
        }

        public override bool IsControllerLeftHand(GameObject controller, bool actual) {
            return CheckControllerLeftHand(controller, actual);
        }

        public override bool IsControllerRightHand(GameObject controller) {
            return CheckActualOrScriptAliasControllerIsRightHand(controller);
        }

        public override bool IsControllerRightHand(GameObject controller, bool actual) {
            return CheckControllerRightHand(controller, actual);
        }

        public override bool IsGripPressedDownOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.PressDown, MotionControllerButtonTypes.Grip);
        }

        public override bool IsGripPressedOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.Press, MotionControllerButtonTypes.Grip);
        }

        public override bool IsGripPressedUpOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.PressUp, MotionControllerButtonTypes.Grip);
        }

        public override bool IsGripTouchedDownOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.TouchDown, MotionControllerButtonTypes.Grip);
        }

        public override bool IsGripTouchedOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.Touch, MotionControllerButtonTypes.Grip);
        }

        public override bool IsGripTouchedUpOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.TouchUp, MotionControllerButtonTypes.Grip);
        }

        public override bool IsHairGripDownOnIndex(uint index) {
            return false;
        }

        public override bool IsHairGripUpOnIndex(uint index) {
            return false;
        }

        public override bool IsHairTriggerDownOnIndex(uint index) {
            // TODO ???
            return false;
        }

        public override bool IsHairTriggerUpOnIndex(uint index) {
            // TODO ???
            return false;
        }

        public override bool IsStartMenuPressedDownOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.PressDown, MotionControllerButtonTypes.Menu);
        }

        public override bool IsStartMenuPressedOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.Press, MotionControllerButtonTypes.Menu);
        }

        public override bool IsStartMenuPressedUpOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.PressUp, MotionControllerButtonTypes.Menu);
        }

        public override bool IsStartMenuTouchedDownOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.TouchDown, MotionControllerButtonTypes.Menu);
        }

        public override bool IsStartMenuTouchedOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.Touch, MotionControllerButtonTypes.Menu);
        }

        public override bool IsStartMenuTouchedUpOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.TouchUp, MotionControllerButtonTypes.Menu);
        }

        public override bool IsTouchpadPressedDownOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.PressDown, MotionControllerButtonTypes.Touchpad);
        }

        public override bool IsTouchpadPressedOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.Press, MotionControllerButtonTypes.Touchpad);
        }

        public override bool IsTouchpadPressedUpOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.PressUp, MotionControllerButtonTypes.Touchpad);
        }

        public override bool IsTouchpadTouchedDownOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.TouchDown, MotionControllerButtonTypes.Touchpad);
        }

        public override bool IsTouchpadTouchedOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.Touch, MotionControllerButtonTypes.Touchpad);
        }

        public override bool IsTouchpadTouchedUpOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.TouchUp, MotionControllerButtonTypes.Touchpad);
        }

        public override bool IsTriggerPressedDownOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.PressDown, MotionControllerButtonTypes.Trigger);
        }

        public override bool IsTriggerPressedOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.Press, MotionControllerButtonTypes.Trigger);
        }

        public override bool IsTriggerPressedUpOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.PressUp, MotionControllerButtonTypes.Trigger);
        }

        public override bool IsTriggerTouchedDownOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.TouchDown, MotionControllerButtonTypes.Trigger);
        }

        public override bool IsTriggerTouchedOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.Touch, MotionControllerButtonTypes.Trigger);
        }

        public override bool IsTriggerTouchedUpOnIndex(uint index) {
            return IsButtonPressed(index, ButtonPressTypes.TouchUp, MotionControllerButtonTypes.Trigger);
        }

        public override void ProcessFixedUpdate(uint index, Dictionary<string, object> options) {
        }

        public override void ProcessUpdate(uint index, Dictionary<string, object> options) {
        }

        public override void SetControllerRenderModelWheel(GameObject renderModel, bool state) {
        }

        private bool IsButtonPressed(uint index, ButtonPressTypes type, MotionControllerButtonTypes button) {
            ControllerState state;
            var found = controllers.TryGetValue(index, out state);
            if(!found) {
                return false;
            }
            
            // TODO 적절히 구현하기
            switch(type) {
                case ButtonPressTypes.Press:
                case ButtonPressTypes.PressDown:
                case ButtonPressTypes.PressUp:
                case ButtonPressTypes.Touch:
                case ButtonPressTypes.TouchDown:
                case ButtonPressTypes.TouchUp:
                    break;
            }
            return false;
        }

        private void SetTrackedControllerCaches(bool forceRefresh = false) {
            if (forceRefresh) {
                cachedLeftController = null;
                cachedRightController = null;
            }

            var sdkManager = VRTK_SDKManager.instance;
            if (sdkManager != null) {
                if (cachedLeftController == null && sdkManager.actualLeftController) {
                    cachedLeftController = sdkManager.actualLeftController.GetComponent<VRTK_TrackedController>();
                    cachedLeftController.index = 0;
                }
                if (cachedRightController == null && sdkManager.actualRightController) {
                    cachedRightController = sdkManager.actualRightController.GetComponent<VRTK_TrackedController>();
                    cachedRightController.index = 1;
                }
            }
        }

        private VRTK_TrackedController GetTrackedObject(GameObject controller) {
            SetTrackedControllerCaches();
            VRTK_TrackedController trackedObject = null;

            if (IsControllerLeftHand(controller)) {
                trackedObject = cachedLeftController;
            } else if (IsControllerRightHand(controller)) {
                trackedObject = cachedRightController;
            }
            return trackedObject;
        }
#endif
    }
}
