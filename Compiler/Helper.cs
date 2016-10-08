using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeDomCs2
{
    class Helper
    {
        public static string Type2ReadMethod(Type type)
        {
            if (type == typeof(byte)) return "ReadByte";
            else if (type == typeof(bool)) return "ReadBool";
            else if (type == typeof(bool[])) return "ReadBoolArr";
            else if (type == typeof(short)) return "ReadShort";
            else if (type == typeof(short[])) return "ReadShortArr";
            else if (type == typeof(int)) return "ReadInt";
            else if (type == typeof(int[])) return "ReadIntArr";
            else if (type == typeof(long)) return "ReadLong";
            else if (type == typeof(long[])) return "ReadLongArr";
            else if (type == typeof(ulong)) return "ReadULong";
            else if (type == typeof(ulong[])) return "ReadULongArr";
            else if (type == typeof(float)) return "ReadFloat";
            else if (type == typeof(float[])) return "ReadFloatArr";
            else if (type == typeof(double)) return "ReadDouble";
            else if (type == typeof(double[])) return "ReadDoubleArr";
            else if (type == typeof(char)) return "ReadChar";
            else if (type == typeof(char[])) return "ReadCharArr";
            else if (type == typeof(string)) return "ReadString";
            else if (type == typeof(string[])) return "ReadStringArr";
            else if (type.IsEnum)
            {
                return "(" + GetTypeFullName(type) + ")ReadInt";
            }
            else if (typeof(Nova.ISerializable).IsAssignableFrom(type))
            {
                return "Read<" + GetTypeFullName(type) + ">";
            }
            else
                throw new Exception("..........");
        }

        public static bool TypeIsList(Type type)
        {
            return (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>));
        }
        public static bool TypeIsDict(Type type)
        {
            return (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>));
        }
        public static bool TypeIsBasic(Type type)
        {
            return (type.IsPrimitive ||
                    type.IsEnum ||
                    type == typeof(string));
        }
        public static bool TypeIsClass(Type type)
        {
            if (type.IsPrimitive)
                return false;

            if (type == typeof(string) ||
                type.IsEnum)
                return false;

            if (type.IsGenericType)
                return false;

            if (type.IsClass)
                return true;

            if (type.IsValueType)
                return false;
            return true;
        }

        static string[] GenTSuffix = new string[] { "`1", "`2", "`3", "`4", "`5" };
        static string[] GenTSuffixReplaceCS = new string[] { "<>", "<,>", "<,,>", "<,,,>", "<,,,,>" };
        static string[] GenTSuffixReplaceJS = new string[] { "$1", "$2", "$3", "$4", "$5" };

        public static string GetTypeGenClassName(string prefix, Type type)
        {
            string s = GetTypeFullName(type).Replace('<', '_').Replace('>', '_').Replace('.', '_').Replace(',', '_').Replace(' ', '_');
            if (s[s.Length - 1] == '_')
                s = s.Substring(0, s.Length - 1);
            return prefix + s;
        }

        static string TypeShortName(Type type)
        {
            if (type == typeof(bool)) 
                return "bool";

            else if (type == typeof(char))
                return "char";
            else if (type == typeof(byte))
                return "byte";

            else if (type == typeof(short)) 
                return "short";
            else if (type == typeof(ushort)) 
                return "ushort";

            else if (type == typeof(int)) 
                return "int";
            else if (type == typeof(uint)) 
                return "uint";

            else if (type == typeof(string)) 
                return "string";

            else if (type == typeof(long)) 
                return "long";
            else if (type == typeof(ulong)) 
                return "ulong";

            else if (type == typeof(float))
                return "float";
            else if (type == typeof(double))
                return "double";

            else return null;
        }

        public static string GetTypeFullName(Type type, bool withT = false)
        {
            if (type == null) return "";
            string sn = TypeShortName(type);
            if (sn != null)
                return sn;

            if (type.IsByRef)
                type = type.GetElementType();

            if (type.IsGenericParameter)
            {  // T
                return type.Name;
            }
            else if (!type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                string rt = type.FullName;
                if (rt == null)
                {
                    rt = ">>>>>>>>>>>?????????????????/";
                }
                rt = rt.Replace('+', '.');
                return rt;
            }
            else if (type.IsGenericTypeDefinition)
            {
                var t = type.IsGenericTypeDefinition ? type : type.GetGenericTypeDefinition();
                string ret = t.FullName;
                if (!withT)
                {
                    for (var i = 0; i < GenTSuffix.Length; i++)
                        ret = ret.Replace(GenTSuffix[i], GenTSuffixReplaceCS[i]);
                    return ret.Replace('+', '.');
                }
                else
                {
                    int length = ret.Length;
                    if (length > 2 && ret[length - 2] == '`')
                    {
                        ret = ret.Substring(0, length - 2);
                        Type[] ts = type.GetGenericArguments();
                        ret += "<";
                        for (int i = 0; i < ts.Length; i++)
                        {
                            ret += GetTypeFullName(ts[i]); // it's T
                            if (i != ts.Length - 1)
                            {
                                ret += ", ";
                            }
                        }
                        ret += ">";
                    }
                    return ret.Replace('+', '.');
                }

                // `1 or `2, `3, ...
                //            string rt = type.FullName;

                //            rt = rt.Substring(0, rt.Length - 2);
                //            rt += "<";
                //            int TCount = type.GetGenericArguments().Length;
                //            for (int i = 0; i < TCount - 1; i++)
                //            {
                //                //no space
                //                rt += ",";
                //            }
                //            rt += ">";
                //            rt = rt.Replace('+', '.');
                //            return rt;
            }
            else
            {
                string parentName = string.Empty;
                if (type.IsNested && type.DeclaringType != null)
                {
                    parentName = GetTypeFullName(type.DeclaringType, withT) + ".";
                }

                string Name = type.Name;
                int length = Name.Length;
                if (length > 2 && Name[length - 2] == '`')
                {
                    Name = Name.Substring(0, length - 2);
                    Type[] ts = type.GetGenericArguments();
                    Name += "<";
                    for (int i = 0; i < ts.Length; i++)
                    {
                        Name += GetTypeFullName(ts[i]); // it's T
                        if (i != ts.Length - 1)
                        {
                            Name += ", ";
                        }
                    }
                    Name += ">";
                }
                return (parentName + Name).Replace('+', '.');
            }
        }
    }
}
