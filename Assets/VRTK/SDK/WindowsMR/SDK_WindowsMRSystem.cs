namespace VRTK {
    [SDK_Description("WindowsMR", SDK_WindowsMRDefines.ScriptingDefineSymbol)]
    public class SDK_WindowsMRSystem
#if VRTK_DEFINE_SDK_WINDOWSMR
        : SDK_BaseSystem
#else
        : SDK_FallbackSystem
#endif
        {
#if VRTK_DEFINE_SDK_WINDOWSMR
        public override void ForceInterleavedReprojectionOn(bool force) {
            // TODO oculus 구현을 선택
        }

        public override bool IsDisplayOnDesktop() {
            return false;
        }

        public override bool ShouldAppRenderWithLowResources() {
            return false;
        }
#endif
    }
}
