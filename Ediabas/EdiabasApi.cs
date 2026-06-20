using System;
using System.Runtime.InteropServices;

namespace S54VanosTester.Ediabas
{
    /// <summary>
    /// Raw P/Invoke bindings for the EDIABAS C API exposed by <c>api32.dll</c>.
    ///
    /// <para>
    /// <c>api32.dll</c> is the runtime DLL that ships with an EDIABAS installation and is the
    /// counterpart of the <c>ediabas.lib</c> import library used when linking native C/C++ code.
    /// The DLL is 32-bit, therefore this application is compiled for the x86 platform.
    /// </para>
    ///
    /// <para>
    /// All strings are ANSI and the calling convention is <c>stdcall</c> (APIENTRY).
    /// </para>
    ///
    /// <para>
    /// IMPORTANT: every function is exported from <c>api32.dll</c> with a leading double-underscore
    /// (e.g. <c>__apiInitExt</c>), not the bare C name. Each <see cref="DllImportAttribute"/> below
    /// therefore pins the real export via <c>EntryPoint</c>; without it the loader fails with
    /// "Unable to find an entry point named 'apiInitExt' in DLL 'api32.dll'".
    /// </para>
    /// </summary>
    internal static class EdiabasApi
    {
        private const string Dll = "api32.dll";

        // --- apiState() return values -------------------------------------------------
        public const int APIREADY = 0; // last job finished successfully
        public const int APIBUSY = 1;  // a job is still running
        public const int APIERROR = 2; // last job finished with an error

        // --- Result value types returned by apiResultFormat() -------------------------
        public const int APIFORMAT_CHAR = 0;
        public const int APIFORMAT_BYTE = 1;
        public const int APIFORMAT_INTEGER = 2;
        public const int APIFORMAT_WORD = 3;
        public const int APIFORMAT_LONG = 4;
        public const int APIFORMAT_DWORD = 5;
        public const int APIFORMAT_REAL = 6;
        public const int APIFORMAT_TEXT = 7;
        public const int APIFORMAT_BINARY = 8;

        // --- Lifecycle ----------------------------------------------------------------

        /// <summary>Initialise EDIABAS using the configuration from EDIABAS.INI.</summary>
        [DllImport(Dll, EntryPoint = "__apiInit", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool apiInit();

        /// <summary>
        /// Initialise EDIABAS with an explicit interface, unit and application id.
        /// The <paramref name="configuration"/> argument accepts semicolon-separated
        /// EDIABAS.INI overrides (e.g. <c>"Interface=STD:OBD;ObdComPort=COM3"</c>) on
        /// builds of EDIABAS that support it. On older builds it is ignored, so we also
        /// persist settings to EDIABAS.INI before calling this.
        /// </summary>
        [DllImport(Dll, EntryPoint = "__apiInitExt", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool apiInitExt(string ifh, string unit, string application, string configuration);

        [DllImport(Dll, EntryPoint = "__apiEnd", CallingConvention = CallingConvention.StdCall)]
        public static extern void apiEnd();

        // --- Job execution ------------------------------------------------------------

        /// <summary>Run a diagnostic job. <paramref name="parameters"/> and <paramref name="resultFilter"/> may be empty strings.</summary>
        [DllImport(Dll, EntryPoint = "__apiJob", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool apiJob(string ecu, string job, string parameters, string resultFilter);

        /// <summary>Run a job passing a raw binary parameter buffer.</summary>
        [DllImport(Dll, EntryPoint = "__apiJobData", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool apiJobData(string ecu, string job, byte[] parameters, int parameterLength, string resultFilter);

        // --- State / errors -----------------------------------------------------------

        [DllImport(Dll, EntryPoint = "__apiState", CallingConvention = CallingConvention.StdCall)]
        public static extern int apiState();

        [DllImport(Dll, EntryPoint = "__apiErrorCode", CallingConvention = CallingConvention.StdCall)]
        public static extern ushort apiErrorCode();

        // apiErrorText returns a const char* owned by the DLL.
        [DllImport(Dll, EntryPoint = "__apiErrorText", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr apiErrorTextPtr();

        public static string apiErrorText()
        {
            IntPtr ptr = apiErrorTextPtr();
            return ptr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringAnsi(ptr);
        }

        // --- Result access ------------------------------------------------------------

        /// <summary>Number of result sets produced by the last job (set 0 is the system set).</summary>
        [DllImport(Dll, EntryPoint = "__apiResultSets", CallingConvention = CallingConvention.StdCall)]
        public static extern void apiResultSets(out ushort sets);

        /// <summary>Number of named results inside a given set.</summary>
        [DllImport(Dll, EntryPoint = "__apiResultNumber", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool apiResultNumber(out ushort number, ushort set);

        /// <summary>Determine the storage format of a result.</summary>
        [DllImport(Dll, EntryPoint = "__apiResultFormat", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool apiResultFormat(out int format, string result, ushort set);

        [DllImport(Dll, EntryPoint = "__apiResultInt", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool apiResultInt(out int buffer, string result, ushort set);

        [DllImport(Dll, EntryPoint = "__apiResultReal", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool apiResultReal(out double buffer, string result, ushort set);

        [DllImport(Dll, EntryPoint = "__apiResultText", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool apiResultText(byte[] buffer, string result, ushort set, string format);

        /// <summary>Read the name of the result at the given index (1-based) inside a set.</summary>
        [DllImport(Dll, EntryPoint = "__apiResultName", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool apiResultName(byte[] buffer, ushort index, ushort set);
    }
}
