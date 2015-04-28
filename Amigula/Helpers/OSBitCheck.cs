using System;

namespace Amigula.Helpers
{
    public sealed class OSBitCheck
    {
        /// <summary>
        ///     The function determines whether the current operating system is a
        ///     64-bit operating system.
        /// </summary>
        /// <returns>
        ///     The function returns true if the operating system is 64-bit;
        ///     otherwise, it returns false.
        /// </returns>
        public static bool Is64BitOperatingSystem()
        {
            if (IntPtr.Size == 8) // 64-bit programs run only on Win64
            {
                return true;
            }
            // Detect whether the current process is a 32-bit process 
            // running on a 64-bit system.
            bool flag;
            return ((DoesWin32MethodExist("kernel32.dll", "IsWow64Process") &&
                     SafeNativeMethods.IsWow64Process(SafeNativeMethods.GetCurrentProcess(), out flag)) && flag);
        }

        /// <summary>
        ///     The function determins whether a method exists in the export
        ///     table of a certain module.
        /// </summary>
        /// <param name="moduleName">The name of the module</param>
        /// <param name="methodName">The name of the method</param>
        /// <returns>
        ///     The function returns true if the method specified by methodName
        ///     exists in the export table of the module specified by moduleName.
        /// </returns>
        private static bool DoesWin32MethodExist(string moduleName, string methodName)
        {
            IntPtr moduleHandle = SafeNativeMethods.GetModuleHandle(moduleName);
            if (moduleHandle == IntPtr.Zero)
            {
                return false;
            }
            return (SafeNativeMethods.GetProcAddress(moduleHandle, methodName) != IntPtr.Zero);
        }
    }
}