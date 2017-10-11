using System.Collections.Generic;
using UnityEngine;

#if UNITY_WSA
using System.Collections;
using UnityEngine.XR.WSA.Input;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity;
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
    public class MyMotionControllerVisualizer : MonoBehaviour {
        internal static MyMotionControllerVisualizer Instance { get; private set; }

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
        [SerializeField]
        GameObject editorControllerOverride = null;

#if UNITY_WSA
        // This will be used to keep track of our controllers, indexed by their unique source ID.
        private Dictionary<uint, MyMotionControllerInfo> controllerDictionary;

        uint _cachedLeftMotionControllerID = uint.MaxValue;
        uint _cachedRightMotionControllerID = uint.MaxValue;
        public uint LeftMotionControllerID { get { return _cachedLeftMotionControllerID; } }
        public uint RightMotionControllerID { get { return _cachedRightMotionControllerID; } }

        MyMotionControllerInfo _cachedLeftMotionController = null;
        MyMotionControllerInfo _cachedRightMotionController = null;
        public MyMotionControllerInfo LeftMotionController { get { return _cachedLeftMotionController; } }
        public MyMotionControllerInfo RightMotionController { get { return _cachedRightMotionController; } }

        InteractionSource _cachedLeftMotionControllerSource;
        InteractionSource _cachedRightMotionControllerSource;
        public InteractionSource LeftMotionControllerSource { get { return _cachedLeftMotionControllerSource; } }
        public InteractionSource RightMotionControllerSource { get { return _cachedRightMotionControllerSource; } }
#endif

        void Awake() {
            MyMotionControllerVisualizer.Instance = this;

            if(Application.isEditor) {
                if(LeftControllerOverride == null) {
                    LeftControllerOverride = editorControllerOverride;
                }
                if(RightControllerOverride == null) {
                    RightControllerOverride = editorControllerOverride;
                }
            }
        }

        private void Start() {
            Application.onBeforeRender += Application_onBeforeRender;

            controllerDictionary = new Dictionary<uint, MyMotionControllerInfo>();

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
                MyMotionControllerInfo currentController;
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
            MyMotionControllerVisualizer.Instance = null;
        }

        private void Application_onBeforeRender() {
#if UNITY_WSA
            // NOTE: This work is being done here to present the most correct rendered location of the controller each frame.
            // Any app logic depending on the controller state should happen in Update() or using InteractionManager's events.
            foreach (var sourceState in InteractionManager.GetCurrentReading()) {
                MyMotionControllerInfo currentController;
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
            Debug.LogFormat("{0} {1} Detected", obj.state.source.handedness, obj.state.source.kind);

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
            Debug.LogFormat("{0} {1} Lost", obj.state.source.handedness, obj.state.source.kind);

            InteractionSource source = obj.state.source;
            if (source.kind == InteractionSourceKind.Controller) {
                MyMotionControllerInfo controller;
                if (controllerDictionary != null && controllerDictionary.TryGetValue(source.id, out controller)) {
                    controllerDictionary.Remove(source.id);

                    // 컨트롤러 객체 재사용하고싶으니 파괴하진 않는다
                    controller.ControllerParent.SetActive(false);

                    // reset cache
                    if (source.handedness == InteractionSourceHandedness.Left) {
                        _cachedLeftMotionController = null;
                        _cachedLeftMotionControllerID = uint.MaxValue;
                        _cachedLeftMotionControllerSource = new InteractionSource();
                    } else if (source.handedness == InteractionSourceHandedness.Right) {
                        _cachedRightMotionController = null;
                        _cachedRightMotionControllerID = uint.MaxValue;
                        _cachedRightMotionControllerSource = new InteractionSource();
                    }
                }
            }
        }

        /// <summary>
        /// mixed reality toolkit은 객체 생성/삭제 기반으로 컨트롤러를 구현했지만
        /// VRTK는 객체 on/off를 전제로 구현되었다
        /// VRTK 뜯어고치는것보다는 mixed reality toolkit 손보는게 나을거같다
        /// </summary>
        GameObject leftControllerModelGameObject = null;
        GameObject rightControllerModelGameObject = null;

        private IEnumerator LoadControllerModel(InteractionSource source) {
            GameObject controllerModelGameObject = null;
            GameObject parentGameObject = null;

            if (source.handedness == InteractionSourceHandedness.Left) {
                parentGameObject = leftControllerParent;
            } else if (source.handedness == InteractionSourceHandedness.Right) {
                parentGameObject = rightControllerParent;
            }
            Debug.Assert(parentGameObject != null);
            parentGameObject.SetActive(true);

            if (source.handedness == InteractionSourceHandedness.Left && LeftControllerOverride != null) {
                if(leftControllerModelGameObject == null) {
                    leftControllerModelGameObject = Instantiate(LeftControllerOverride);
                }
                controllerModelGameObject = leftControllerModelGameObject;

            } else if (source.handedness == InteractionSourceHandedness.Right && RightControllerOverride != null) {
                if (rightControllerModelGameObject == null) {
                    rightControllerModelGameObject = Instantiate(RightControllerOverride);
                }
                controllerModelGameObject = rightControllerModelGameObject;
            } else {
#if !UNITY_EDITOR
                bool requireNewModel = false;
                if (source.handedness == InteractionSourceHandedness.Left && leftControllerModelGameObject == null) {
                    requireNewModel = true;
                }
                if (source.handedness == InteractionSourceHandedness.Right && rightControllerModelGameObject == null) {
                    requireNewModel = true;
                }

                if (requireNewModel) {
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

                    // 캐싱
                    if (source.handedness == InteractionSourceHandedness.Left) {
                        leftControllerModelGameObject = controllerModelGameObject;
                    } else if (source.handedness == InteractionSourceHandedness.Right) {
                        rightControllerModelGameObject = controllerModelGameObject;
                    }

                } else {
                    if (source.handedness == InteractionSourceHandedness.Left) {
                        controllerModelGameObject = leftControllerModelGameObject;
                    } else if (source.handedness == InteractionSourceHandedness.Right) {
                        controllerModelGameObject = rightControllerModelGameObject;
                    }
                }
#else
                // 더미 컨트롤러를 만들었기때문에 여기에는 진입하지 않는다
                Debug.Assert(false);
                yield break;
#endif
            }

            /*
            사용 가능한 mesh renderer 목록
            8개

            New Game Object/GLTFScene/GLTFNode/GLTFNode/GLTFNode/GLTFNode/GLTFNode/GLTFNode/GLTFMesh/Primitive
            New Game Object/GLTFScene/GLTFNode/GLTFNode/HOME/VALUE/GLTFNode/GLTFMesh/Primitive
            New Game Object/GLTFScene/GLTFNode/GLTFNode/MENU/VALUE/GLTFNode/GLTFMesh/Primitive
            New Game Object/GLTFScene/GLTFNode/GLTFNode/GRASP/VALUE/GLTFNode/GLTFMesh/Primitive
            New Game Object/GLTFScene/GLTFNode/GLTFNode/THUMBSTICK_PRESS/VALUE/THUMBSTICK_X/VALUE/THUMBSTICK_Y/VALUE/GLTFNode/GLTFMesh/Primitive
            New Game Object/GLTFScene/GLTFNode/GLTFNode/SELECT/VALUE/GLTFNode/GLTFMesh/Primitive
            New Game Object/GLTFScene/GLTFNode/GLTFNode/TOUCHPAD_PRESS/VALUE/TOUCHPAD_PRESS_X/VALUE/TOUCHPAD_PRESS_Y/VALUE/TOUCHPAD_TOUCH_X/GLTFNode/GLTFMesh/Primitive
            New Game Object/GLTFScene/GLTFNode/GLTFNode/GLTFNode/GLTFMesh/Primitive

            var renderers = controllerModelGameObject.GetComponentsInChildren<Renderer>();
            Debug.Log(renderers.Length);
            for (var i = 0 ; i < renderers.Length; i++) {
                var r = renderers[i];
                var n = SDK_WindowsMRController.GetElemPath(r.gameObject, null);
                Debug.LogFormat("{0} {1} {2}", i, n, r.GetType().ToString());
            }
            */

            var info = FinishControllerSetup(parentGameObject, controllerModelGameObject, source.handedness.ToString(), source.id);
            if(source.handedness == InteractionSourceHandedness.Left) {
                _cachedLeftMotionController = info;
                _cachedLeftMotionControllerID = source.id;
                _cachedLeftMotionControllerSource = source;
            } else if(source.handedness == InteractionSourceHandedness.Right) {
                _cachedRightMotionController = info;
                _cachedRightMotionControllerID = source.id;
                _cachedRightMotionControllerSource = source;
            }
            yield break;
        }
#endif

#if UNITY_WSA
        private MyMotionControllerInfo FinishControllerSetup(GameObject parentGameObject, GameObject controllerModelGameObject, string handedness, uint id) {
            var defaultPos = controllerModelGameObject.transform.localPosition;
            var defaultRot = controllerModelGameObject.transform.localRotation;

            parentGameObject.transform.parent = transform;
            controllerModelGameObject.transform.parent = parentGameObject.transform;

            controllerModelGameObject.transform.localPosition = defaultPos;
            controllerModelGameObject.transform.localRotation = defaultRot;

            var newControllerInfo = new MyMotionControllerInfo()
            {
                ControllerParent = parentGameObject,
                ControllerModelGameObject = controllerModelGameObject,
            };
            if (AnimateControllerModel) {
                newControllerInfo.LoadInfo(controllerModelGameObject.GetComponentsInChildren<Transform>(), this);
            }
            controllerDictionary.Add(id, newControllerInfo);

            return newControllerInfo;
        }
#endif

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
