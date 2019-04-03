using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Diagnostics;

namespace AAEmu.Launcher.LauncherBase
{
    public partial class AAEmuLauncherBase
    {
        public string userName { get; set; }
        public string gameExeFilePath { get; set; }
        public string loginServerAdress { get; set; }
        public UInt16 loginServerPort { get; set; }
        public string locale { get; set; }
        public string hShieldArgs { get; set; }
        public string extraArguments { get; set; }
        /// <summary>
        /// launchArguments should be generated by InitializeForLaunch()
        /// </summary>
        public string launchArguments { get; set; }
        public Process runningProcess { get; protected set; }
        protected string _passwordHash { get; set; }
        protected string launchVerb { get; set; }

        public AAEmuLauncherBase()
        {
            userName = "";
            _passwordHash = "";
            gameExeFilePath = "C:\\ArcheAge\\Working\\Bin32\\ArcheAge.exe";
            launchArguments = "";
            extraArguments = "";
            hShieldArgs = "";
            launchVerb = "runas";
            loginServerAdress = "127.0.0.1";
            loginServerPort = 1237;
            locale = "";
            runningProcess = null;
        }

        public virtual void Dispose()
        {
            // Dispose
        }

        ~AAEmuLauncherBase()
        {
            Dispose();
        }

        public virtual bool SetPassword(string userPassword)
        {
            try
            {
                byte[] data = Encoding.Default.GetBytes(userPassword);
                var passwordHashData = new SHA256Managed().ComputeHash(data);
                _passwordHash = BitConverter.ToString(passwordHashData).Replace("-", "").ToLower();
            }
            catch
            {
                return false;
            }
            return true;
        }

        public virtual bool InitializeForLaunch()
        {
            return true;
        }

        public virtual bool Launch()
        {
            ProcessStartInfo GameClientProcessInfo;
            var fullArgs = launchArguments;
            if (extraArguments != "")
                fullArgs += " " + extraArguments;
            if (hShieldArgs != "")
                fullArgs += " " + hShieldArgs;
            GameClientProcessInfo = new ProcessStartInfo(gameExeFilePath, fullArgs);
            GameClientProcessInfo.UseShellExecute = true;
            GameClientProcessInfo.Verb = launchVerb;
            bool startOK = false;
            try
            {
                runningProcess = Process.Start(GameClientProcessInfo);
                startOK = true;
            }
            catch
            {
                startOK = false;
            }

            return startOK;
        }

        public virtual bool FinalizeLaunch()
        {
            return true;
        }

    }

    internal class Win32
    {
        public static IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        public static int FILE_MAP_READ = 0x0004;
        public static uint PAGE_READWRITE = 0x04;
        public static uint FILE_MAP_ALL_ACCESS = 0x04;

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);


        [DllImport("kernel32.dll")]
        internal static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr CreateEventW(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, [MarshalAs(UnmanagedType.LPWStr)]  string lpName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr CreateFileMapping(
            IntPtr hFile,
            IntPtr lpFileMappingAttributes,
            FileMapProtection flProtect,
            uint dwMaximumSizeHigh,
            uint dwMaximumSizeLow,
            [MarshalAs(UnmanagedType.LPStr)] string lpName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr CreateFileMappingW(
            IntPtr hFile,
            IntPtr lpFileMappingAttributes,
            FileMapProtection flProtect,
            uint dwMaximumSizeHigh,
            uint dwMaximumSizeLow,
            [MarshalAs(UnmanagedType.LPWStr)] string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr MapViewOfFile(IntPtr hFileMapping, FileMapAccess dwDesiredAccess, UInt32 dwFileOffsetHigh,
            UInt32 dwFileOffsetLow, UInt32 dwNumberOfBytesToMap);

        [DllImport("kernel32.dll")]
        internal static extern bool FlushViewOfFile(IntPtr lpBaseAddress, Int32 dwNumberOfBytesToFlush);

        [DllImport("kernel32")]
        internal static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("kernel32", SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr hFile);

        [DllImport("kernel32", SetLastError = true)]
        internal static extern int WaitForSingleObject(IntPtr hHandle,uint dwMilliseconds);

        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static extern IntPtr MemCpy(IntPtr dest, IntPtr src, uint count);

        /// <summary>
        /// Helper function to debug memorymappedfiles
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="dumpFilename"></param>
        /// <returns></returns>
        public static string DumpMemFile(int handle, string dumpFilename)
        {
            try
            {
                IntPtr testMemPtr = MapViewOfFile((IntPtr)handle, FileMapAccess.FileMapRead, 0, 0, 4096);
                if (testMemPtr == IntPtr.Zero)
                {
                    return "Win32Error: 0x" + Marshal.GetLastWin32Error().ToString("X8");
                }
                List<byte> bytes = new List<byte>();
                for(int i = 0;i < 1028;i++)
                {
                    var b = Marshal.ReadByte(testMemPtr, i);
                    bytes.Add(b);
                }
                UnmapViewOfFile(testMemPtr);
                File.WriteAllBytes(dumpFilename, bytes.ToArray());
            }
            catch (Exception x)
            {
                return "EXCEPTION: "+x.Message;
            }
            return "";
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SECURITY_ATTRIBUTES
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        public int bInheritHandle;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct STARTUPINFO
    {
        public Int32 cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public Int32 dwX;
        public Int32 dwY;
        public Int32 dwXSize;
        public Int32 dwYSize;
        public Int32 dwXCountChars;
        public Int32 dwYCountChars;
        public Int32 dwFillAttribute;
        public Int32 dwFlags;
        public Int16 wShowWindow;
        public Int16 cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    [Flags]
    internal enum FileMapProtection : uint
    {
        PageReadonly = 0x02,
        PageReadWrite = 0x04,
        PageWriteCopy = 0x08,
        PageExecuteRead = 0x20,
        PageExecuteReadWrite = 0x40,
        SectionCommit = 0x8000000,
        SectionImage = 0x1000000,
        SectionNoCache = 0x10000000,
        SectionReserve = 0x4000000,
    }

    [Flags]
    internal enum FileMapAccess : uint
    {
        FileMapCopy = 0x0001,
        FileMapWrite = 0x0002,
        FileMapRead = 0x0004,
        FileMapAllAccess = 0x001f,
        FileMapExecute = 0x0020,

        FileMapAllAccessFull = 0xf001f // TODO ...
    }

    public static class RC4
    {
        public static string Encrypt(string key, string data)
        {
            Encoding unicode = Encoding.Unicode;

            return Convert.ToBase64String(Encrypt(unicode.GetBytes(key), unicode.GetBytes(data)));
        }

        public static string Decrypt(string key, string data)
        {
            Encoding unicode = Encoding.Unicode;

            return unicode.GetString(Encrypt(unicode.GetBytes(key), Convert.FromBase64String(data)));
        }

        public static byte[] Encrypt(byte[] key, byte[] data)
        {
            return EncryptOutput(key, data).ToArray();
        }

        public static byte[] Decrypt(byte[] key, byte[] data)
        {
            return EncryptOutput(key, data).ToArray();
        }

        private static byte[] EncryptInitalize(byte[] key)
        {
            byte[] s = Enumerable.Range(0, 256)
              .Select(i => (byte)i)
              .ToArray();

            for (int i = 0, j = 0; i < 256; i++)
            {
                j = (j + key[i % key.Length] + s[i]) & 255;

                Swap(s, i, j);
            }

            return s;
        }

        private static IEnumerable<byte> EncryptOutput(byte[] key, IEnumerable<byte> data)
        {
            byte[] s = EncryptInitalize(key);

            int i = 0;
            int j = 0;

            return data.Select((b) =>
            {
                i = (i + 1) & 255;
                j = (j + s[i]) & 255;

                Swap(s, i, j);

                return (byte)(b ^ s[(s[i] + s[j]) & 255]);
            });
        }

        private static void Swap(byte[] s, int i, int j)
        {
            byte c = s[i];

            s[i] = s[j];
            s[j] = c;
        }
    }


}