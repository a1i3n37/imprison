using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace Imprison
{
    class Program
    {
        private static bool IsRunAsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(id);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static bool SetupDefaultShell()
        {
            var regKey =
                Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", true);

            if (regKey == null)
                return false;

            regKey.SetValue("Shell", "explorer.exe");
            regKey.Close();

            return true;
        }

        private static bool MakeShellUserSpecific()
        {
            var regKey =
                Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\IniFileMapping\system.ini\boot", true);

            if (regKey == null)
                return false;

            regKey.SetValue("Shell", @"USR:Software\Microsoft\Windows NT\CurrentVersion\Winlogon");
            regKey.Close();

            return true;
        }

        private static bool ImprisonUser(string userSid, string appPath)
        {
            var regCurrentUser = Registry.Users.OpenSubKey(userSid, true);

            var regKey =
                regCurrentUser?.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", true);

            if (regKey == null)
                return false;

            regKey.SetValue("Shell", appPath);
            regKey.Close();

            regKey =
                regCurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", true);

            if (regKey == null)
                regKey = regCurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", true);

            if (regKey == null)
                return false;

            regKey.SetValue("DisableTaskMgr", 1);
            regKey.SetValue("DisableRegistryTools", 1);
            regKey.Close();

            regCurrentUser.Close();
            return true;
        }

        private static bool EnableAutomaticLogin(string username, string password)
        {
            var regLocalMachine = RegistryKey.OpenBaseKey(
                RegistryHive.LocalMachine,
                Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32
            );

            var regKey =
                regLocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", true);

            if (regKey == null)
                return false;

            regKey.SetValue("DefaultUserName", username);
            regKey.SetValue("DefaultPassword", password);
            regKey.SetValue("AutoAdminLogon", 1);
            regKey.Close();

            return true;
        }

        private static bool SetLimitBlankPasswordUse(bool enable)
        {
            var regKey =
                Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\Lsa", true);

            if (regKey == null)
                return false;

            regKey.SetValue("LimitBlankPasswordUse", enable ? 1 : 0);
            regKey.Close();

            return true;
        }

        static void Main(string[] args)
        {
            string appPath;
            string userName;
            string userPassword;

            var userManager = new LocalUserManager();

            if (!IsRunAsAdmin())
            {
                Console.WriteLine("\n\tYou need to run this as Administrator!\n");
                return;
            }

            if (args.Length < 2)
            {
                var moduleFileName = Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location);

                Console.WriteLine("\n\tAuthor:\t\tDipl.-Ing. Phillip Djurdjevic");
                Console.WriteLine("\tVersion:\t1.0");

                Console.WriteLine("\n\tUsage: ");
                Console.WriteLine($"\t\t{moduleFileName} UserName /pw:password AppPath\n");

                return;
            }

            if (args.Length == 3)
            {
                userName = args[0];
                userPassword = args[1].Remove(0, 4);
                appPath = args[2];
            }
            else
            {
                userName = args[0];
                userPassword = "";
                appPath = args[1];
            }

            Console.Write($"\nLooking for user '{userName}' ... ");
            if (!userManager.UserExists(userName))
            {
                Console.Write("Not found. \nCreating user ... ");
                userManager.CreateUser(userName, userPassword);
                Console.Write("Success.");
            }
            else
                Console.Write("Success.");

            var usersGroup = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);

            Console.Write($"\nChecking user groups ... ");
            if (!userManager.IsUserInGroup(userName, usersGroup.ToString()))
            {
                Console.Write($"Failed.\nAdding user to group '{usersGroup.Translate(typeof(NTAccount)).Value}' ... ");
                bool success = userManager.AddUserToGroup(userName, usersGroup.ToString());
                Console.Write(success ? "Success." : "Failed.");

                if (!success)
                    return;
            }
            else
                Console.Write("Success.");

            Console.Write("\nSetting up default shell ... ");
            if (!SetupDefaultShell())
            {
                Console.Write("Failed.");
                return;
            }
            else
                Console.Write("Success.");

            Console.Write("\nMaking shell user specific ... ");
            if (!MakeShellUserSpecific())
            {
                Console.Write("Failed.");
                return;
            }
            else
                Console.Write("Success.");

            var logonToken = IntPtr.Zero;
            var logonTokenDuplicate = IntPtr.Zero;

            Console.Write("\nLogging on user ... ");
            if (userPassword.Length == 0)
                SetLimitBlankPasswordUse(false);

            if (Natives.LogonUser(
                    userName,
                    Environment.MachineName,
                    userPassword,
                    (int)Natives.LogonType.LOGON32_LOGON_INTERACTIVE,
                    (int)Natives.LogonProvider.LOGON32_PROVIDER_DEFAULT,
                    ref logonToken)
                == 0)
            {
                Console.Write("Failed.");

                if (userPassword.Length == 0)
                    SetLimitBlankPasswordUse(true);

                return;
            }
            else
                Console.Write("Success.");

            if (userPassword.Length == 0)
                SetLimitBlankPasswordUse(true);

            Console.Write("\nDuplicating user logon token ... ");
            if (Natives.DuplicateToken(
                    logonToken,
                    (int)Natives.ImpersonationLevel.SecurityImpersonation,
                    ref logonTokenDuplicate)
                == 0)
            {
                Console.Write("Failed.");
                return;
            }
            else
                Console.Write("Success.");

            Console.Write("\nLoading user profile ... ");
            var profileInfo = new Natives.ProfileInfo();
            profileInfo.dwSize = Marshal.SizeOf(profileInfo);
            profileInfo.lpUserName = userName;
            profileInfo.dwFlags = 1;
            bool loadProfileSuccess = Natives.LoadUserProfile(logonTokenDuplicate, ref profileInfo);

            if (!loadProfileSuccess || profileInfo.hProfile == IntPtr.Zero)
            {
                Console.Write("Failed. Error: " + Marshal.GetLastWin32Error());
                return;
            }
            else
                Console.Write("Success.");

            var user = userManager.GetUser(userName);
        
            Console.Write("\nImprisioning user ... ");
            if (!ImprisonUser(user.Sid.ToString(), appPath))
            {
                Console.Write("Failed.");
                return;
            }
            else
                Console.Write("Success.");

            Console.Write("\nUnloading user profile ... ");
            if (!Natives.UnloadUserProfile(logonTokenDuplicate, profileInfo.hProfile))
            {
                Console.Write("Failed.");
                return;
            }
            else
                Console.Write("Success.");

            if (logonToken != IntPtr.Zero)
                Natives.CloseHandle(logonToken);

            if (logonTokenDuplicate != IntPtr.Zero)
                Natives.CloseHandle(logonTokenDuplicate);

            Console.Write("\nEnabling automatic login ... ");
            if (!EnableAutomaticLogin(userName, userPassword))
            {
                Console.Write("Failed.");
                return;
            }
            else
                Console.Write("Success.");

            Console.WriteLine();
        }

        private static bool SetupUserShell()
        {
            return false;
        }
    }
}
