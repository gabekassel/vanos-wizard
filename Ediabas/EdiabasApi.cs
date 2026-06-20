using System;
using System.Runtime.InteropServices;
using System.Text;

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
    /// All strings are ANSI and the calling convention is <c>stdcall</c>. Every function is exported
    /// twice: as a decorated <c>___apiInit@4</c> stdcall symbol and as an undecorated <c>__apiInit</c>
    /// alias for the same address; we bind the undecorated alias via <c>EntryPoint</c>.
    /// </para>
    ///
    /// <para>
    /// IMPORTANT: this api32.dll uses the <b>handle-based</b> EDIABAS API. Every function takes an
    /// instance handle as its <b>first</b> parameter: <see cref="apiInit"/> / <see cref="apiInitExt"/>
    /// receive it (<c>out</c>), and every other call passes it back by value. The <c>@N</c> stdcall
    /// decorations in the export table prove this (e.g. <c>___apiInit@4</c> = one 4-byte argument,
    /// <c>___apiInitExt@20</c> = five). Omitting the handle makes <c>apiInit</c> dereference a garbage
    /// stack slot and the process dies with an AccessViolation inside api32.dll.
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

        /// <summary>Initialise EDIABAS using the configuration from EDIABAS.INI. Returns the new
        /// instance <paramref name="handle"/> that must be passed to every subsequent call.</summary>
        [DllImport(Dll, EntryPoint = "__apiInit", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool apiInit(out uint handle);

        /// <summary>
        /// Initialise EDIABAS with an explicit interface, unit and application id, returning the new
        /// instance <paramref name="handle"/>. The <paramref name="configuration"/> argument accepts
        /// semicolon-separated EDIABAS.INI overrides (e.g. <c>"Interface=STD:OBD;ObdComPort=COM3"</c>)
        /// on builds of EDIABAS that support it. On older builds it is ignored, so we also persist
        /// settings to EDIABAS.INI before calling this.
        /// </summary>
        [DllImport(Dll, EntryPoint = "__apiInitExt", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool apiInitExt(out uint handle, string ifh, string unit, string application, string configuration);

        [DllImport(Dll, EntryPoint = "__apiEnd", CallingConvention = CallingConvention.StdCall)]
        public static extern void apiEnd(uint handle);

        // --- Job execution ------------------------------------------------------------

        /// <summary>Run a diagnostic job. Returns the job id (non-zero when started). <paramref name="parameters"/>
        /// and <paramref name="resultFilter"/> may be empty strings.</summary>
        [DllImport(Dll, EntryPoint = "__apiJob", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint apiJob(uint handle, string ecu, string job, string parameters, string resultFilter);

        /// <summary>Run a job passing a raw binary parameter buffer. Returns the job id (non-zero when started).</summary>
        [DllImport(Dll, EntryPoint = "__apiJobData", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint apiJobData(uint handle, string ecu, string job, byte[] parameters, int parameterLength, string resultFilter);

        // --- State / errors -----------------------------------------------------------

        [DllImport(Dll, EntryPoint = "__apiState", CallingConvention = CallingConvention.StdCall)]
        public static extern int apiState(uint handle);

        [DllImport(Dll, EntryPoint = "__apiErrorCode", CallingConvention = CallingConvention.StdCall)]
        public static extern ushort apiErrorCode(uint handle);

        // apiErrorText writes the message into a caller-supplied buffer (handle, buffer, length); it
        // does NOT return a const char*. The DLL copies a default string even when the handle is
        // invalid, so this is safe to call on an init failure.
        [DllImport(Dll, EntryPoint = "__apiErrorText", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern void apiErrorTextNative(uint handle, byte[] buffer, ushort bufferLength);

        public static string apiErrorText(uint handle)
        {
            var buffer = new byte[1024];
            apiErrorTextNative(handle, buffer, (ushort)buffer.Length);
            int length = Array.IndexOf(buffer, (byte)0);
            if (length < 0)
                length = buffer.Length;
            return Encoding.ASCII.GetString(buffer, 0, length).Trim();
        }

        // --- Result access ------------------------------------------------------------

        /// <summary>Number of result sets produced by the last job (set 0 is the system set).</summary>
        [DllImport(Dll, EntryPoint = "__apiResultSets", CallingConvention = CallingConvention.StdCall)]
        public static extern void apiResultSets(uint handle, out ushort sets);

        /// <summary>Number of named results inside a given set.</summary>
        [DllImport(Dll, EntryPoint = "__apiResultNumber", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool apiResultNumber(uint handle, out ushort number, ushort set);

        /// <summary>Determine the storage format of a result.</summary>
        [DllImport(Dll, EntryPoint = "__apiResultFormat", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool apiResultFormat(uint handle, out int format, string result, ushort set);

        [DllImport(Dll, EntryPoint = "__apiResultInt", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool apiResultInt(uint handle, out int buffer, string result, ushort set);

        [DllImport(Dll, EntryPoint = "__apiResultReal", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool apiResultReal(uint handle, out double buffer, string result, ushort set);

        [DllImport(Dll, EntryPoint = "__apiResultText", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool apiResultText(uint handle, byte[] buffer, string result, ushort set, string format);

        /// <summary>Read the name of the result at the given index (1-based) inside a set.</summary>
        [DllImport(Dll, EntryPoint = "__apiResultName", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool apiResultName(uint handle, byte[] buffer, ushort index, ushort set);
    }
}
