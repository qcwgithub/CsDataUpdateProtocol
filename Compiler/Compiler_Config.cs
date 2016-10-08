using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeDomCs2
{
    class Compiler_Config
    {
        public const string CLASS_PREFIX = "dp";
        public const string BASE_NAME = "dpBase";
        public const string ENUM_NAME = "Oper";

        public static string GetTypeGenClassName(Type type)
        {
            return Helper.GetTypeGenClassName(CLASS_PREFIX, type);
        }
    }
}
