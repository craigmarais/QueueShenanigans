using Microsoft.Win32.SafeHandles;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace QueueShenanigans
{
    [SuppressUnmanagedCodeSecurity]
    public class WinSafeThreadHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetThreadAffinityMask(WinSafeThreadHandle handle, HandleRef mask);
        [DllImport("kernel32")]
        public static extern int GetCurrentThreadId();
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern WinSafeThreadHandle OpenThread(int access, bool inherit, int threadId);
        [DllImport("Kernel32.dll")]
        public static extern int GetCurrentProcessorNumber();

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success), DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern bool CloseHandle(IntPtr handle);

        public WinSafeThreadHandle() : base(true)
        {

        }

        protected override bool ReleaseHandle()
        {
            return CloseHandle(handle);
        }

        public static void SetProcessorAffinity(int coreMask)
        {
            int threadId = GetCurrentThreadId();
            WinSafeThreadHandle handle = null;
            var tempHandle = new object();
            try
            {
                handle = OpenThread(0x60, false, threadId);
                if (SetThreadAffinityMask(handle, new HandleRef(tempHandle, (IntPtr)coreMask)) == IntPtr.Zero)
                {
                    throw new Exception("Failed to set processor affinity for thread");
                }
            }
            finally
            {
                if (handle != null)
                {
                    handle.Close();
                }
            }
        }
    }
}
