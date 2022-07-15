using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnemyIK
{
    internal static class Logger
    {
        private static readonly ManualLogSource _Logger;

        static Logger()
        {
            _Logger = new ManualLogSource("EnemyIK");
            BepInEx.Logging.Logger.Sources.Add(_Logger);
        }

        private static string Format(object msg) => msg.ToString();
        public static void Info(object data) => _Logger.LogMessage(Format(data));
        public static void Verbose(object data)
        {
#if DEBUG
            _Logger.LogDebug(Format(data));
#endif
        }
        public static void Debug(object data) => _Logger.LogDebug(Format(data));
        public static void Error(object data) => _Logger.LogError(Format(data));
    }
}
