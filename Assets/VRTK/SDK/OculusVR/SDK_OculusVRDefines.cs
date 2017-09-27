// OculusVR Defines|SDK_OculusVR|001
namespace VRTK
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Handles all the scripting define symbols for the OculusVR and Avatar SDKs.
    /// </summary>
    public static class SDK_OculusVRDefines
    {
        /// <summary>
        /// The scripting define symbol for the OculusVR SDK.
        /// </summary>
        public const string ScriptingDefineSymbol = SDK_ScriptingDefineSymbolPredicateAttribute.RemovableSymbolPrefix + "SDK_OCULUSVR";
        /// <summary>
        /// The scripting define symbol for the OculusVR Avatar SDK.
        /// </summary>
        public const string AvatarScriptingDefineSymbol = SDK_ScriptingDefineSymbolPredicateAttribute.RemovableSymbolPrefix + "SDK_OCULUSVR_AVATAR";

        private const string BuildTargetGroupName = "Standalone";

        [SDK_ScriptingDefineSymbolPredicate(ScriptingDefineSymbol, BuildTargetGroupName)]
        [SDK_ScriptingDefineSymbolPredicate(SDK_ScriptingDefineSymbolPredicateAttribute.RemovableSymbolPrefix + "OCULUSVR_UTILITIES_1_12_0_OR_NEWER", BuildTargetGroupName)]
        private static bool IsUtilitiesVersion1120OrNewer()
        {
            Version wrapperVersion = GetOculusWrapperVersion();
            return wrapperVersion != null && wrapperVersion >= new Version(1, 12, 0);
        }

        [SDK_ScriptingDefineSymbolPredicate(ScriptingDefineSymbol, BuildTargetGroupName)]
        [SDK_ScriptingDefineSymbolPredicate(SDK_ScriptingDefineSymbolPredicateAttribute.RemovableSymbolPrefix + "OCULUSVR_UTILITIES_1_11_0_OR_OLDER", BuildTargetGroupName)]
        private static bool IsUtilitiesVersion1110OrOlder()
        {
            Version wrapperVersion = GetOculusWrapperVersion();
            return wrapperVersion != null && wrapperVersion < new Version(1, 12, 0);
        }

        [SDK_ScriptingDefineSymbolPredicate(AvatarScriptingDefineSymbol, BuildTargetGroupName)]
        private static bool IsAvatarAvailable()
        {
#if NETFX_CORE
            return (IsUtilitiesVersion1120OrNewer() || IsUtilitiesVersion1110OrOlder())
                   && typeof(SDK_OculusVRDefines).GetTypeInfo().Assembly.GetType("OvrAvatar") != null;
#else
            return (IsUtilitiesVersion1120OrNewer() || IsUtilitiesVersion1110OrOlder())
                   && typeof(SDK_OculusVRDefines).Assembly.GetType("OvrAvatar") != null;
#endif
        }

        private static Version GetOculusWrapperVersion()
        {
#if NETFX_CORE
            Type pluginClass = typeof(SDK_OculusVRDefines).GetTypeInfo().Assembly.GetType("OVRPlugin");
#else
            Type pluginClass = typeof(SDK_OculusVRDefines).Assembly.GetType("OVRPlugin");
#endif
            if (pluginClass == null)
            {
                return null;
            }

            FieldInfo versionField = pluginClass.GetField("wrapperVersion", BindingFlags.Public | BindingFlags.Static);
            if (versionField == null)
            {
                return null;
            }

            var version = (Version)versionField.GetValue(null);
            return version;
        }

        private static Version GetOculusRuntimeVersion()
        {
#if NETFX_CORE
            Type pluginClass = typeof(SDK_OculusVRDefines).GetTypeInfo().Assembly.GetType("OVRPlugin");
#else
            Type pluginClass = typeof(SDK_OculusVRDefines).Assembly.GetType("OVRPlugin");
#endif
            if (pluginClass == null)
            {
                return null;
            }

            PropertyInfo versionProperty = pluginClass.GetProperty("version", BindingFlags.Public | BindingFlags.Static);
            if (versionProperty == null)
            {
                return null;
            }

            var version = (Version)versionProperty.GetGetMethod().Invoke(null, null);
            return version;
        }
    }
}