#if NETFX_CORE
using System.Reflection;
#endif

// Daydream Defines|SDK_Daydream|001
namespace VRTK
{
    /// <summary>
    /// Handles all the scripting define symbols for the Daydream SDK.
    /// </summary>
    public static class SDK_DaydreamDefines
    {
        /// <summary>
        /// The scripting define symbol for the Daydream SDK.
        /// </summary>
        public const string ScriptingDefineSymbol = SDK_ScriptingDefineSymbolPredicateAttribute.RemovableSymbolPrefix + "SDK_DAYDREAM";

        [SDK_ScriptingDefineSymbolPredicate(ScriptingDefineSymbol, "Android")]
        private static bool IsDaydreamAvailable()
        {
#if NETFX_CORE
            return typeof(SDK_DaydreamDefines).GetTypeInfo().Assembly.GetType("GvrController") != null;
#else
            return typeof(SDK_DaydreamDefines).Assembly.GetType("GvrController") != null;
#endif
        }
    }
}