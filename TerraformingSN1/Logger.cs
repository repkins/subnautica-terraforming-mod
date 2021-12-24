using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Terraforming
{
    class Logger
    {
        private static string assemblyName = Assembly.GetCallingAssembly().GetName().Name;

        static public void Info(string message)
        {
            Console.WriteLine("[{0}/Info] {1}", assemblyName, message);
        }
        static public void Warning(string message)
        {
            Console.WriteLine("[{0}/Warn] {1}", assemblyName, message);
        }
        static public void Error(string message)
        {
            Console.WriteLine("[{0}/ERROR] {1}", assemblyName, message);
        }
        static public void Debug(string message)
        {
#if DEBUG
            var callingMethod = new StackFrame(1).GetMethod();
            var callingMethodName = callingMethod.Name;
            var callingMethodClassName = callingMethod.DeclaringType.Name;
            Console.WriteLine("[{0}/Debug] Frame {1}: {2}.{3}(): {4}", assemblyName, Time.frameCount, callingMethodClassName, callingMethodName, message);
#endif
        }
    }
}
