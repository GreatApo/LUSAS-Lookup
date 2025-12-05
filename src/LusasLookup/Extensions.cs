using KSharedEnums;
using Lusas.Common.Attributes;
using Lusas.LPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LusasLookup {
    public static partial class Extensions {

        /// <summary>Returns the method value of the given object.</summary>
        /// <param name="method">Target method</param>
        /// <param name="obj">Target object</param>
        /// <param name="args">Method arguments</param>
        public static object GetValue(this MethodInfo method, object obj, object[] args = null) {
            object value;
            try {
                var getArgs = new object[method.GetParameters().Length];
                if (args != null) {
                    for (int i = 0; i < Math.Min(args.Length, getArgs.Length); i++) {
                        getArgs[i] = args[i];
                    }
                }
                value = method.Invoke(obj, getArgs);
            } catch {
                value = null;
            }
            return value;
        }

        /// <summary>Returns the method value of the given object.</summary>
        /// <param name="method">Target method</param>
        /// <param name="obj">Target object</param>
        /// <param name="arg">Method argument</param>
        public static object GetValue(this MethodInfo method, object obj, object arg) {
            return method.GetValue(obj, new object[] { arg });
        }

        /// <summary>Returns the object type as string.</summary>
        /// <param name="obj">Target object</param>
        public static string GetTypeAsString(this object obj) {
            string varType = obj?.GetType().ToString() ?? "Null";

            if (varType.StartsWith("System.")) {
                // Simplify system types
                varType = varType.Substring(7);

            } else if (varType == "__ComObject") {
                // Load COM object type
                var t_type = ComTypeHelper.GetCOMObjectType(obj);
                if (t_type != null) varType = t_type.ToString();
            }

            return varType;
        }
    }
}
