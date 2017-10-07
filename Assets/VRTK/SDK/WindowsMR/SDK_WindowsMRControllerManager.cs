using System.Collections.Generic;
using UnityEngine;

#if UNITY_WSA
using System.Collections;
using UnityEngine.XR.WSA.Input;
using HoloToolkit.Unity.InputModule;
#if !UNITY_EDITOR
using GLTF;
using Windows.Foundation;
using Windows.Storage.Streams;
#endif
#endif

namespace VRTK {
    /// <summary>
    /// Motion Controller visualizer와 유사하지만
    /// 컨트롤러 객체를 동적으로 생성/삭제하지 않는다
    /// 컨트롤러 객체를 런타임에 생성/삭제하는것은 VRTK의 전제조건과 맞지 않기때문
    /// 
    /// TODO MotionControllerVisualizer와 비교하면서 작업하기
    /// </summary>
    class SDK_WindowsMRControllerManager : MonoBehaviour {
        [Tooltip("This setting will be used to determine if the model, override or otherwise, should attempt to be animated based on the user's input.")]
        public bool AnimateControllerModel = true;

        [Tooltip("Use a model with the tip in the positive Z direction and the front face in the positive Y direction. This will override the platform left controller model.")]
        [SerializeField]
        protected GameObject LeftControllerOverride;
        [Tooltip("Use a model with the tip in the positive Z direction and the front face in the positive Y direction. This will override the platform right controller model.")]
        [SerializeField]
        protected GameObject RightControllerOverride;
        [Tooltip("Use this to override the indicator used to show the user's touch location on the touchpad. Default is a sphere.")]
        [SerializeField]
        protected GameObject TouchpadTouchedOverride;

        [Tooltip("This material will be used on the loaded glTF controller model. This does not affect the above overrides.")]
        [SerializeField]
        protected UnityEngine.Material GLTFMaterial;

        [SerializeField]
        GameObject leftControllerParent = null;
        [SerializeField]
        GameObject rightControllerParent = null;

        // This will be used to keep track of our controllers, indexed by their unique source ID.
        private Dictionary<uint, MotionControllerInfo> controllerDictionary;

        private void Start() {
            Application.onBeforeRender += Application_onBeforeRender;

            controllerDictionary = new Dictionary<uint, MotionControllerInfo>();

#if UNITY_WSA
            if (!Application.isEditor) {
                if (GLTFMaterial == null) {
                    if (LeftControllerOverride == null && RightControllerOverride == null) {
                        Debug.Log("If using glTF, please specify a material on " + name + ". Otherwise, please specify controller overrides.");
                    } else if (LeftControllerOverride == null || RightControllerOverride == null) {
                        Debug.Log("Only one override is specified, and no material is specified for the glTF model. Please set the material or the " + ((LeftControllerOverride == null) ? "left" : "right") + " controller override on " + name + ".");
                    }
                }
            } else {
                // Since we're using non-Unity APIs, glTF will only load in a UWP app.
                if (LeftControllerOverride == null && RightControllerOverride == null) {
                    Debug.Log("Running in the editor won't render the glTF models, and no controller overrides are set. Please specify them on " + name + ".");
                } else if (LeftControllerOverride == null || RightControllerOverride == null) {
                    Debug.Log("Running in the editor won't render the glTF models, and only one controller override is specified. Please set the " + ((LeftControllerOverride == null) ? "left" : "right") + " override on " + name + ".");
                }
            }

            InteractionManager.InteractionSourceDetected += InteractionManager_InteractionSourceDetected;
            InteractionManager.InteractionSourceLost += InteractionManager_InteractionSourceLost;
#endif
        }

        private void Update() {
#if UNITY_WSA
            // NOTE: The controller's state is being updated here in order to provide a good position and rotation
            // for any child GameObjects that might want to raycast or otherwise reason about their location in the world.
            foreach (var sourceState in InteractionManager.GetCurrentReading()) {
                MotionControllerInfo currentController;
                if (sourceState.source.kind == InteractionSourceKind.Controller && controllerDictionary.TryGetValue(sourceState.source.id, out currentController)) {
                    if (AnimateControllerModel) {
                        currentController.AnimateSelect(sourceState.selectPressedAmount);

                        if (sourceState.source.supportsGrasp) {
                            currentController.AnimateGrasp(sourceState.grasped);
                        }

                        if (sourceState.source.supportsMenu) {
                            currentController.AnimateMenu(sourceState.menuPressed);
                        }

                        if (sourceState.source.supportsThumbstick) {
                            currentController.AnimateThumbstick(sourceState.thumbstickPressed, sourceState.thumbstickPosition);
                        }

                        if (sourceState.source.supportsTouchpad) {
                            currentController.AnimateTouchpad(sourceState.touchpadPressed, sourceState.touchpadTouched, sourceState.touchpadPosition);
                        }
                    }

                    Vector3 newPosition;
                    if (sourceState.sourcePose.TryGetPosition(out newPosition, InteractionSourceNode.Grip)) {
                        currentController.ControllerParent.transform.localPosition = newPosition;
                    }

                    Quaternion newRotation;
                    if (sourceState.sourcePose.TryGetRotation(out newRotation, InteractionSourceNode.Grip)) {
                        currentController.ControllerParent.transform.localRotation = newRotation;
                    }
                }
            }
#endif
        }

        private void OnDestroy() {
            Application.onBeforeRender -= Application_onBeforeRender;
        }

        private void Application_onBeforeRender() {
#if UNITY_WSA
            // NOTE: This work is being done here to present the most correct rendered location of the controller each frame.
            // Any app logic depending on the controller state should happen in Update() or using InteractionManager's events.
            foreach (var sourceState in InteractionManager.GetCurrentReading()) {
                MotionControllerInfo currentController;
                if (sourceState.source.kind == InteractionSourceKind.Controller && controllerDictionary.TryGetValue(sourceState.source.id, out currentController)) {
                    if (AnimateControllerModel) {
                        currentController.AnimateSelect(sourceState.selectPressedAmount);

                        if (sourceState.source.supportsGrasp) {
                            currentController.AnimateGrasp(sourceState.grasped);
                        }

                        if (sourceState.source.supportsMenu) {
                            currentController.AnimateMenu(sourceState.menuPressed);
                        }

                        if (sourceState.source.supportsThumbstick) {
                            currentController.AnimateThumbstick(sourceState.thumbstickPressed, sourceState.thumbstickPosition);
                        }

                        if (sourceState.source.supportsTouchpad) {
                            currentController.AnimateTouchpad(sourceState.touchpadPressed, sourceState.touchpadTouched, sourceState.touchpadPosition);
                        }
                    }

                    Vector3 newPosition;
                    if (sourceState.sourcePose.TryGetPosition(out newPosition, InteractionSourceNode.Grip)) {
                        currentController.ControllerParent.transform.localPosition = newPosition;
                    }

                    Quaternion newRotation;
                    if (sourceState.sourcePose.TryGetRotation(out newRotation, InteractionSourceNode.Grip)) {
                        currentController.ControllerParent.transform.localRotation = newRotation;
                    }
                }
            }
#endif
        }

#if UNITY_WSA
        private void InteractionManager_InteractionSourceDetected(InteractionSourceDetectedEventArgs obj) {
            // We only want to attempt loading a model if this source is actually a controller.
            if (obj.state.source.kind == InteractionSourceKind.Controller && !controllerDictionary.ContainsKey(obj.state.source.id)) {
                StartCoroutine(LoadControllerModel(obj.state.source));
            }
        }

        /// <summary>
        /// When a controller is lost, the model is destroyed and the controller object
        /// is removed from the tracking dictionary.
        /// </summary>
        /// <param name="obj">The source event args to be used to determine the controller model to be removed.</param>
        private void InteractionManager_InteractionSourceLost(InteractionSourceLostEventArgs obj) {
            InteractionSource source = obj.state.source;
            if (source.kind == InteractionSourceKind.Controller) {
                MotionControllerInfo controller;
                if (controllerDictionary != null && controllerDictionary.TryGetValue(source.id, out controller)) {
                    controllerDictionary.Remove(source.id);

                    // 컨트롤러 객체 재사용하고싶으니 파괴하진 않는다
                    controller.ControllerParent.SetActive(false);
                    for(var i = 0; i < controller.ControllerParent.transform.childCount; i++) {
                        var child = controller.ControllerParent.transform.GetChild(0);
                        Destroy(child.gameObject);
                    }
                    // Destroy(controller.ControllerParent);
                }
            }
        }

        private IEnumerator LoadControllerModel(InteractionSource source) {
            GameObject controllerModelGameObject;
            GameObject parentGameObject;

            if (source.handedness == InteractionSourceHandedness.Left) {
                parentGameObject = leftControllerParent;
            } else if (source.handedness == InteractionSourceHandedness.Right) {
                parentGameObject = rightControllerParent;
            } else {
                Debug.Assert(false, "do not reach");
                parentGameObject = null;
            }

            if (source.handedness == InteractionSourceHandedness.Left && LeftControllerOverride != null) {
                controllerModelGameObject = Instantiate(LeftControllerOverride);
            } else if (source.handedness == InteractionSourceHandedness.Right && RightControllerOverride != null) {
                controllerModelGameObject = Instantiate(RightControllerOverride);
            } else {
#if !UNITY_EDITOR
                if (GLTFMaterial == null)
                {
                    Debug.Log("If using glTF, please specify a material on " + name + ".");
                    yield break;
                }

                // This API returns the appropriate glTF file according to the motion controller you're currently using, if supported.
                IAsyncOperation<IRandomAccessStreamWithContentType> modelTask = source.TryGetRenderableModelAsync();

                if (modelTask == null)
                {
                    Debug.Log("Model task is null.");
                    yield break;
                }

                while (modelTask.Status == AsyncStatus.Started)
                {
                    yield return null;
                }

                IRandomAccessStreamWithContentType modelStream = modelTask.GetResults();

                if (modelStream == null)
                {
                    Debug.Log("Model stream is null.");
                    yield break;
                }

                if (modelStream.Size == 0)
                {
                    Debug.Log("Model stream is empty.");
                    yield break;
                }

                byte[] fileBytes = new byte[modelStream.Size];

                using (DataReader reader = new DataReader(modelStream))
                {
                    DataReaderLoadOperation loadModelOp = reader.LoadAsync((uint)modelStream.Size);

                    while (loadModelOp.Status == AsyncStatus.Started)
                    {
                        yield return null;
                    }

                    reader.ReadBytes(fileBytes);
                }

                controllerModelGameObject = new GameObject();
                GLTFComponentStreamingAssets gltfScript = controllerModelGameObject.AddComponent<GLTFComponentStreamingAssets>();
                gltfScript.ColorMaterial = GLTFMaterial;
                gltfScript.NoColorMaterial = GLTFMaterial;
                gltfScript.GLTFData = fileBytes;

                yield return gltfScript.LoadModel();
#else
                yield break;
#endif
            }

            FinishControllerSetup(parentGameObject, controllerModelGameObject, source.handedness.ToString(), source.id);
        }
#endif

        private void FinishControllerSetup(GameObject parentGameObject, GameObject controllerModelGameObject, string handedness, uint id) {
            var defaultPos = controllerModelGameObject.transform.localPosition;
            var defaultRot = controllerModelGameObject.transform.localRotation;

            parentGameObject.SetActive(true);
            parentGameObject.transform.parent = transform;
            controllerModelGameObject.transform.parent = parentGameObject.transform;

            controllerModelGameObject.transform.localPosition = defaultPos;
            controllerModelGameObject.transform.localRotation = defaultRot;

            var newControllerInfo = new MotionControllerInfo() { ControllerParent = parentGameObject };
            // TODO 컨트롤러 애니메이션은 실제 게임에서 거의 필요없을것이다
            // 왜냐하면 집게는 별도로 구현했으니
            //if (AnimateControllerModel) {
            //    newControllerInfo.LoadInfo(controllerModelGameObject.GetComponentsInChildren<Transform>(), this);
            //}
            controllerDictionary.Add(id, newControllerInfo);
        }

        public GameObject SpawnTouchpadVisualizer(Transform parentTransform) {
            GameObject touchVisualizer;
            if (TouchpadTouchedOverride != null) {
                touchVisualizer = Instantiate(TouchpadTouchedOverride);
            } else {
                touchVisualizer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                touchVisualizer.transform.localScale = new Vector3(0.0025f, 0.0025f, 0.0025f);
                touchVisualizer.GetComponent<Renderer>().sharedMaterial = GLTFMaterial;
            }

            Destroy(touchVisualizer.GetComponent<Collider>());
            touchVisualizer.transform.parent = parentTransform;
            touchVisualizer.transform.localPosition = Vector3.zero;
            touchVisualizer.transform.localRotation = Quaternion.identity;
            touchVisualizer.SetActive(false);
            return touchVisualizer;
        }
    }
}
