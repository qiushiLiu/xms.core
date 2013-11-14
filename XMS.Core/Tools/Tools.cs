using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace XMS.Core
{
    public  class XMSTools
    {
        public static string GetSingleParaString(string name, object input)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                return String.Empty;
            }
            return name  + XMS.Core.Formatter.PlainObjectFormatter.Simplified.Format(input);
           
        }

        public static string GetParaString(ParameterInfo[] parameters, object[] inputs)
        {
            if (parameters == null || inputs == null)
                return String.Empty;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < parameters.Length; i++)
            {
                //sb.Append(parameters[i].ParameterType.Name).Append(" ");
                sb.Append(parameters[i].Name);
                try
                {
                    sb.Append("=");

                    XMS.Core.Formatter.PlainObjectFormatter.Simplified.Format(inputs.Length > i ? inputs[i] : null, sb);
                }
                catch { }
                if (i < parameters.Length - 1)
                {
                    sb.Append(", ");
                }
            }

            return sb.ToString();
        }

    }
}
