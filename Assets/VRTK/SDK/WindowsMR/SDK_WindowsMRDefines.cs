namespace VRTK {
    public static class SDK_WindowsMRDefines {
        public const string ScriptingDefineSymbol = SDK_ScriptingDefineSymbolPredicateAttribute.RemovableSymbolPrefix + "SDK_WINDOWSMR";

        const string BuildTargetGroupName = "WSA";

        [SDK_ScriptingDefineSymbolPredicate(ScriptingDefineSymbol, BuildTargetGroupName)]
        private static bool IsWindowsMRAvailable() {
#if UNITY_WSA
            return true;
#else
            return false;
#endif
        }
    }
}
