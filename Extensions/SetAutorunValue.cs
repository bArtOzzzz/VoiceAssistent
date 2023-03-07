using System.Reflection;
using Microsoft.Win32;
using System.IO;

namespace VoiceAssistent.Extensions
{
    public static class SetAutorunValue
    {
        /// <summary>
        /// Sets autorun for application if true.
        /// </summary>
        /// <param name="autorun"></param>
        /// <returns></returns>
        public static bool SetValue(bool autorun)
        {
            const string name = "Ori assistant";
            string ExePath = Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.ToString();
            RegistryKey reg;
            reg = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run\\");

            try
            {
                if (autorun)
                    reg.SetValue(name, ExePath);
                else
                    reg.DeleteValue(name);

                reg.Close();
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
