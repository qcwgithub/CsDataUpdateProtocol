using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace dp
{
    class Compiler_Config
    {
        public const string CLASS_PREFIX = "dp";
        public const string BASE_NAME = "dpBase";
        public const string ENUM_NAME = "Op";
        public static bool OUTPUT_ONE_FILE = false;
        public static string OUTPUT_FILE_NAME = "dpGen.cs";

        public static string GetTypeGenClassName(Type type)
        {
            return Helper.GetTypeGenClassName(CLASS_PREFIX, type);
        }
    }
}
