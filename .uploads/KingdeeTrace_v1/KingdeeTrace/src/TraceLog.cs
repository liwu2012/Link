using System;
using System.Diagnostics;

namespace KingdeeTrace
{
    internal static class TraceLog
    {
        public static void Error(string tag, Exception ex)
        {
            // 生产环境请替换为你自己的日志组件（如金蝶 BOS 日志或 log4net）
            Trace.WriteLine("[KingdeeTrace][" + tag + "] " + ex);
        }
    }
}
