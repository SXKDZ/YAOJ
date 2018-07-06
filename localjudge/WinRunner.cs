using System.Drawing;
using System.Runtime.InteropServices;

namespace localjudge
{
    // dotnet Wrapper for WinRunner
    enum WinRunnerStatus { OK, TLE, MLE, RE, ERR };

    [StructLayout(LayoutKind.Sequential)]
    class WinRunnerResult
    {
        public WinRunnerStatus status;
        public double usedTime;
        public double usedMemory;
    }

    class WinRunner
    {
        [DllImport("WINRUNNER.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern void StartRestrictedProcess([Out] WinRunnerResult result,
            [MarshalAs(UnmanagedType.LPWStr), In] string cmd,
            [MarshalAs(UnmanagedType.LPWStr), In] string arg,
            [MarshalAs(UnmanagedType.LPWStr), In] string infile,
            [MarshalAs(UnmanagedType.LPWStr), In] string outfile,
            [MarshalAs(UnmanagedType.LPWStr), In] string errfile,
            uint time, uint memory, bool restrictProcess, ulong affinity);

        [DllImport("WINRUNNER.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern void StartCompiler([Out] WinRunnerResult result,
            [MarshalAs(UnmanagedType.LPWStr), In] string cmd,
            [MarshalAs(UnmanagedType.LPWStr), In] string errfile,
            uint time, uint memory);

        [DllImport("WINRUNNER.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern void StartProcess([Out] WinRunnerResult result,
            [MarshalAs(UnmanagedType.LPWStr), In] string cmd,
            [MarshalAs(UnmanagedType.LPWStr), In] string infile,
            [MarshalAs(UnmanagedType.LPWStr), In] string outfile,
            uint time, uint memory, ulong affinity);
    }
}
