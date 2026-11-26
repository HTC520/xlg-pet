using System;
using System.IO;
using System.Reflection;

namespace StardewDesktopPet
{
    public static class AutoStartHelper
    {
        private static string GetShortcutPath()
        {
            string startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            return Path.Combine(startupDir, "StardewDesktopPet.lnk");
        }

        public static bool IsEnabled()
        {
            return File.Exists(GetShortcutPath());
        }

        public static bool SetEnabled(bool enabled)
        {
            try
            {
                string lnkPath = GetShortcutPath();
                if (!enabled)
                {
                    if (File.Exists(lnkPath)) File.Delete(lnkPath);
                    return true;
                }

                string exePath = Assembly.GetExecutingAssembly().Location;
                string workDir = Path.GetDirectoryName(exePath);

                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return false;

                object shell = Activator.CreateInstance(shellType);
                object shortcut = shellType.InvokeMember(
                    "CreateShortcut",
                    BindingFlags.InvokeMethod,
                    null,
                    shell,
                    new object[] { lnkPath });

                Type scType = shortcut.GetType();
                scType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { exePath });
                scType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { workDir });
                scType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, new object[] { "星露谷桌面宠物" });
                scType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
