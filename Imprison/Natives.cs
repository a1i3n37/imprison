using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Imprison
{
    internal class Natives
    {
        [DllImport("advapi32.dll")]
        public static extern int LogonUser(String lpszUserName,
            String lpszDomain,
            String lpszPassword,
            int dwLogonType,
            int dwLogonProvider,
            ref IntPtr phToken);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int DuplicateToken(IntPtr hToken,
            int impersonationLevel, ref IntPtr hNewToken);

        ///
        /// A process should call the RevertToSelf function after finishing
        /// any impersonation begun by using the DdeImpersonateClient,
        /// ImpersonateDdeClientWindow, ImpersonateLoggedOnUser,
        /// ImpersonateNamedPipeClient, ImpersonateSelf,
        /// ImpersonateAnonymousToken or SetThreadToken function.
        /// If RevertToSelf fails, your application continues to run in the context
        /// of the client, which is not appropriate.
        /// You should shut down the process if RevertToSelf fails.
        /// RevertToSelf Function:
        /// http://msdn.microsoft.com/en-us/library/aa379317(VS.85).aspx
        ///
        /// A boolean value indicates the function succeeded or not.
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool RevertToSelf();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        [StructLayout(LayoutKind.Sequential)]
        public struct ProfileInfo
        {
            ///
            /// Specifies the size of the structure, in bytes.
            ///
            public int dwSize;

            ///
            /// This member can be one of the following flags: PI_NOUI or PI_APPLYPOLICY
            ///
            public int dwFlags;

            ///
            /// Pointer to the name of the user.
            /// This member is used as the base name
            /// of the directory in which to store a new profile.
            ///
            public string lpUserName;

            ///
            /// Pointer to the roaming user profile path.
            /// If the user does not have a roaming profile, this member can be NULL.
            ///
            public string lpProfilePath;

            ///
            /// Pointer to the default user profile path. This member can be NULL.
            ///
            public string lpDefaultPath;

            ///
            /// Pointer to the name of the validating domain controller, in NetBIOS format.
            /// If this member is NULL, the Windows NT 4.0-style policy will not be applied.
            ///
            public string lpServerName;

            ///
            /// Pointer to the path of the Windows NT 4.0-style policy file. This member can be NULL.
            ///
            public string lpPolicyPath;

            ///
            /// Handle to the HKEY_CURRENT_USER registry key.
            ///
            public IntPtr hProfile;
        }

        [DllImport("userenv.dll",
            SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern bool LoadUserProfile(IntPtr hToken,
            ref ProfileInfo lpProfileInfo);

        [DllImport("Userenv.dll",
            CallingConvention = CallingConvention.Winapi,
            SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern bool UnloadUserProfile(IntPtr hToken,
            IntPtr lpProfileInfo);

        public enum LogonType
        {
            LOGON32_LOGON_INTERACTIVE = 2,
            LOGON32_LOGON_NETWORK = 3,
            LOGON32_LOGON_BATCH = 4,
            LOGON32_LOGON_SERVICE = 5,
            LOGON32_LOGON_UNLOCK = 7,
            LOGON32_LOGON_NETWORK_CLEARTEXT = 8, // Win2K or higher
            LOGON32_LOGON_NEW_CREDENTIALS = 9 // Win2K or higher
        };

        public enum LogonProvider
        {
            LOGON32_PROVIDER_DEFAULT = 0,
            LOGON32_PROVIDER_WINNT35 = 1,
            LOGON32_PROVIDER_WINNT40 = 2,
            LOGON32_PROVIDER_WINNT50 = 3
        };

        public enum ImpersonationLevel
        {
            SecurityAnonymous = 0,
            SecurityIdentification = 1,
            SecurityImpersonation = 2,
            SecurityDelegation = 3
        }
    }
}
