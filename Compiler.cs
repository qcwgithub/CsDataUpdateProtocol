using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Runtime;
using System.Runtime.InteropServices;
using System.CodeDom;
using System.CodeDom.Compiler;
using Nova;

namespace CodeDomCs2
{
    class Compiler
    {
        static BindingFlags BindingFlagsField =
        BindingFlags.Public
            | BindingFlags.GetField
            | BindingFlags.SetField
            | BindingFlags.Instance;

        static BindingFlags BindingFlagsProperty =
            BindingFlags.Public
                | BindingFlags.GetProperty
                | BindingFlags.SetProperty
                | BindingFlags.Instance
            //| BindingFlags.DeclaredOnly
                ;

        bool IsTypeSerializableAndBasic(Type type)
        {
            return type.IsEnum ||
                type == typeof(bool) ||
                type == typeof(char) ||
                type == typeof(byte) ||
                type == typeof(short) ||
                type == typeof(ushort) ||
                type == typeof(int) ||
                type == typeof(uint) ||
                type == typeof(long) ||
                type == typeof(ulong) ||
                type == typeof(float) ||
                type == typeof(double) ||
                type == typeof(string);
        }
        bool IsTypeSerializable(Type type)
        {
            return IsTypeSerializableAndBasic(type) ||
                typeof(ISerializable).IsAssignableFrom(type);
        }


        HashSet<Type> hsAllTypes = null;
        void TravelGetTypes(Type type)
        {
            if (hsAllTypes.Contains(type))
                return;

            if (Helper.TypeIsDict(type))
            {
                Type[] arr = type.GetGenericArguments();
                bool keyOk = IsTypeSerializableAndBasic(arr[0]);
                bool valueOk = IsTypeSerializable(arr[1]);
                if (keyOk && valueOk)
                {
                    hsAllTypes.Add(type);
                    TravelGetTypes(arr[1]);
                }
            }
            else if (Helper.TypeIsList(type))
            {
                Type typeOfT = type.GetGenericArguments()[0];
                if (IsTypeSerializable(typeOfT))
                {
                    hsAllTypes.Add(type);
                    TravelGetTypes(typeOfT);
                }
            }
            else if (IsTypeSerializable(type))
            {
                if (typeof(ISerializable).IsAssignableFrom(type))
                {
                    hsAllTypes.Add(type);

                    FieldInfo[] fields = type.GetFields(BindingFlagsField);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        FieldInfo field = fields[i];
                        Type varType = field.FieldType;
                        string varName = field.Name;

                        // readonly
                        if (field.IsInitOnly)
                            continue;

                        TravelGetTypes(varType);
                    }

                    PropertyInfo[] properties = type.GetProperties(BindingFlagsProperty);
                    for (int i = 0; i < properties.Length; i++)
                    {
                        PropertyInfo pro = properties[i];
                        Type varType = pro.PropertyType;
                        string varName = pro.Name;

                        if (!pro.CanRead || !pro.CanWrite)
                        {
                            continue;
                        }

                        TravelGetTypes(varType);
                    }
                }
            }
        }

        public void Compile(Type type, string outputDir)
        {
            hsAllTypes = new HashSet<Type>();
            TravelGetTypes(type);
            foreach (var t in hsAllTypes)
            {
                CompileOneType(t, outputDir);
            }
        }

        void CreateDictFuncs(TextFile tf, Type typeOfDict, List<string> enumNames)
        {
            Type typeOfKey = typeOfDict.GetGenericArguments()[0];
            Type typeOfValue = typeOfDict.GetGenericArguments()[1];

            {
                enumNames.Add("Clear");
                tf.AddS("public void Clear(IWriteableBuffer w)")
                    .BraceIn()
                    .AddS("Write(w, Ops, {0}.Clear);", Compiler_Config.ENUM_NAME)
                    .BraceOut();

                tf.AddS("public static void Clear_sync(IReadableBuffer r, {0} rv)", Helper.GetTypeFullName(typeOfDict))
                    .BraceIn()
                    .AddS("rv.Clear();")
                    .BraceOut();

                tf.AddLine();
            }

            {
                enumNames.Add("Remove");
                tf.AddS("public void Remove(IWriteableBuffer w, {0} key)", Helper.GetTypeFullName(typeOfKey))
                    .BraceIn()
                    .AddS("Write(w, Ops, {0}.Remove, key);", Compiler_Config.ENUM_NAME)
                    .BraceOut();

                tf.AddS("public static void Remove_sync(IReadableBuffer r, {0} rv)", Helper.GetTypeFullName(typeOfDict))
                    .BraceIn()
                    .AddS("{0} key = r.{1}();", Helper.GetTypeFullName(typeOfKey), Helper.Type2ReadMethod(typeOfKey))
                    .AddS("rv.Remove(key);")
                    .BraceOut();

                tf.AddLine();
            }

            {
                enumNames.Add("Set");
                tf.AddS("public void Set(IWriteableBuffer w, {0} key, {1} value_)", Helper.GetTypeFullName(typeOfKey), Helper.GetTypeFullName(typeOfValue))
                    .BraceIn()
                    .AddS("Write(w, Ops, {0}.Set, key, value_);", Compiler_Config.ENUM_NAME)
                    .BraceOut();

                tf.AddS("public static void Set_sync(IReadableBuffer r, {0} rv)", Helper.GetTypeFullName(typeOfDict))
                    .BraceIn()
                    .AddS("{0} key = r.{1}();", Helper.GetTypeFullName(typeOfKey), Helper.Type2ReadMethod(typeOfKey), Helper.Type2ReadMethod(typeOfValue))
                    .AddS("{0} value_ = r.{1}();", Helper.GetTypeFullName(typeOfKey), Helper.Type2ReadMethod(typeOfKey), Helper.Type2ReadMethod(typeOfValue))
                    .AddS("rv.Remove(key);")
                    .AddS("rv.Add(key, value_);")
                    .BraceOut();

                tf.AddLine();
            }
        }

        void CreateListFuncs(TextFile tf, Type typeOfList, List<string> enumNames)
        {
            Type typeOfT = typeOfList.GetGenericArguments()[0];

            {
                enumNames.Add("Clear");
                tf.AddS("public void Clear(IWriteableBuffer w)")
                    .BraceIn()
                    .AddS("Write(w, Ops, {0}.Clear);", Compiler_Config.ENUM_NAME)
                    .BraceOut();

                tf.AddS("public static void Clear_sync(IReadableBuffer r, {0} rv)", Helper.GetTypeFullName(typeOfList))
                    .BraceIn()
                    .AddS("rv.Clear();")
                    .BraceOut();

                tf.AddLine();
            }

            {
                enumNames.Add("Add");
                TextFile tfAdd = tf.AddS("public void Add(IWriteableBuffer w, int count)")
                    .BraceIn()
                        .AddS("Write(w, Ops, {0}.Add, count);", Compiler_Config.ENUM_NAME)
                        .AddS("for (int i = v.Count - count; i < v.Count; i++)")
                        .BraceIn()
                            .AddS("Write(w, v[i]);")
                        .BraceOut()
                    .BraceOut();

                tf.AddS("public static void Add_sync(IReadableBuffer r, {0} rv)", Helper.GetTypeFullName(typeOfList))
                    .BraceIn()
                        .AddS("int count = r.{0}();", Helper.Type2ReadMethod(typeof(int)))
                        .AddS("for (int i = 0; i < count; i++)")
                        .BraceIn()
                            .AddS("rv.Add(r.{0}());", Helper.Type2ReadMethod(typeOfT))
                        .BraceOut()
                    .BraceOut();

                tf.AddLine();
            }

            {
                enumNames.Add("Insert");
                tf.AddS("public void Insert(IWriteableBuffer w, int index)")
                    .BraceIn()
                    .AddS("Write(w, Ops, {0}.Insert, index, v[index]);", Compiler_Config.ENUM_NAME)
                    .BraceOut();

                tf.AddS("public static void Insert_sync(IReadableBuffer r, {0} rv)", Helper.GetTypeFullName(typeOfList))
                    .BraceIn()
                        .AddS("int index = r.{0}();", Helper.Type2ReadMethod(typeof(int)))
                        .AddS("rv.Insert(index, r.{0}());", Helper.Type2ReadMethod(typeOfT))
                    .BraceOut();

                tf.AddLine();
            }

            {
                enumNames.Add("RemoveByIndex");
                tf.AddS("public void RemoveByIndex(IWriteableBuffer w, int index)")
                    .BraceIn()
                    .AddS("Write(w, Ops, {0}.RemoveByIndex, index);", Compiler_Config.ENUM_NAME)
                    .BraceOut();

                tf.AddS("public static void RemoveByIndex_sync(IReadableBuffer r, {0} rv)", Helper.GetTypeFullName(typeOfList))
                    .BraceIn()
                        .AddS("int index = r.{0}();", Helper.Type2ReadMethod(typeof(int)))
                        .AddS("rv.RemoveAt(index);")
                    .BraceOut();

                tf.AddLine();
            }


            //if (typeOfT.IsClass)
            if (typeof(ISerializable).IsAssignableFrom(typeOfT))
            {

                {
                    enumNames.Add("RemoveByUid");
                    tf.AddS("public void RemoveByUid(IWriteableBuffer w, ulong uid)")
                        .BraceIn()
                        .AddS("Write(w, Ops, {0}.RemoveByUid, uid);", Compiler_Config.ENUM_NAME)
                        .BraceOut();

                    tf.AddS("public static void RemoveByUid_sync(IReadableBuffer r, {0} rv)", Helper.GetTypeFullName(typeOfList))
                        .BraceIn()
                            .AddS("ulong uid = r.{0}();", Helper.Type2ReadMethod(typeof(ulong)))
                            .AddS("{0} _y = rv.Find((_x) => _x.UniqueID == uid);", Helper.GetTypeFullName(typeOfT))
                            .AddS("rv.Remove(_y);")
                        .BraceOut();

                    tf.AddLine();
                }

                {
                    enumNames.Add("GetByIndex");
                    tf.AddS("public {0} GetByIndex(int index)", Compiler_Config.GetTypeGenClassName(typeOfT))
                        .BraceIn()
                        .AddS("return new {0}(v[index], this, {1}.GetByIndex, index);", Compiler_Config.GetTypeGenClassName(typeOfT), Compiler_Config.ENUM_NAME)
                        .BraceOut();

                    tf.AddS("public static void GetByIndex_sync(IReadableBuffer r, {0} rv)", Helper.GetTypeFullName(typeOfList))
                        .BraceIn()
                        .AddS("int index = r.{0}();", Helper.Type2ReadMethod(typeof(int)))
                        .AddS("{0}.Sync(r, rv[index]);", Compiler_Config.GetTypeGenClassName(typeOfT))
                        .BraceOut();

                    tf.AddLine();
                }
            }
        }

        void CompileOneType(Type type, string outputDir)
        {
            TextFile tfFile = new TextFile(null, "// auto gen");
            tfFile.tag = "file root";

            // using
            {
                tfFile.AddS("using System;")
                    .AddS("using Nova;")
                    .AddS("using dp;")
                    .AddS("using System.Collections;")
                    .AddS("using System.Collections.Generic;")
                    .AddLine();
            }

            List<string> enumNames = new List<string>();

                TextFile tfClass = tfFile.AddS("public class {0} : {1}", Compiler_Config.GetTypeGenClassName(type), Compiler_Config.BASE_NAME)
                    .BraceIn();
                    tfClass.BraceOut();

            // constructor
            {
                tfClass.AddS("public {0}()", Compiler_Config.GetTypeGenClassName(type))
                    .BraceIn().BraceOut();


                tfClass.AddS("public {0}({1} v, {2} parent, params object[] ops)", 
                    Compiler_Config.GetTypeGenClassName(type), Helper.GetTypeFullName(type), Compiler_Config.BASE_NAME)
                    
                    .BraceIn()
                    .AddS("this.v = v;")
                    .AddS("Init(parent, ops);")
                    .BraceOut();
            }

            // v
            {
                tfClass.AddLine().AddS("{0} v;", Helper.GetTypeFullName(type)).AddLine();
            }

            if (Helper.TypeIsList(type))
            {
                CreateListFuncs(tfClass, type, enumNames);
            }
            else if (Helper.TypeIsDict(type))
            {
                CreateDictFuncs(tfClass, type, enumNames);
            }
            else
            {
                // fields
                {
                    FieldInfo[] fields = type.GetFields(BindingFlagsField);
                    foreach (var field in fields)
                    {
                        if (IsTypeSerializable(field.FieldType))
                        {
                            string enumName = field.Name + "_Update";
                            enumNames.Add(enumName);

                            tfClass.AddS("public void {0}_Update(IWriteableBuffer w)", field.Name)
                            .BraceIn()
                            .AddS("Write(w, Ops, {0}.{1}, v.{2});", Compiler_Config.ENUM_NAME, enumName, field.Name)
                            .BraceOut();

                            tfClass.AddS("public static void {0}_Update_sync(IReadableBuffer r, {1} rv)", field.Name, Helper.GetTypeFullName(type))
                                .BraceIn()
                                .AddS("rv.{0} = r.{1}();", field.Name, Helper.Type2ReadMethod(field.FieldType))
                                .BraceOut();

                            tfClass.AddLine();
                        }

                        //if (field.FieldType.IsClass)
                        if (typeof(ISerializable).IsAssignableFrom(field.FieldType)
                            || Helper.TypeIsList(field.FieldType))
                        {
                            enumNames.Add(field.Name);
                            tfClass.AddS("public {0} {1}()", Compiler_Config.GetTypeGenClassName(field.FieldType), field.Name)
                                .BraceIn()
                                .AddS("return new {0}(v.{1}, this, {2}.{1});",
                                    Compiler_Config.GetTypeGenClassName(field.FieldType),
                                    field.Name, Compiler_Config.ENUM_NAME)
                                .BraceOut();

                            tfClass.AddS("public static void {0}_sync(IReadableBuffer r, {1} rv)", field.Name, Helper.GetTypeFullName(type))
                                .BraceIn()
                                .AddS("{0}.Sync(r, rv.{1});", Compiler_Config.GetTypeGenClassName(field.FieldType), field.Name)
                                .BraceOut();

                            tfClass.AddLine();
                        }
                    }
                }
            }

            if (enumNames.Count > 0)
            {
                TextFile tfEnum = tfClass.AddS("public enum {0}", Compiler_Config.ENUM_NAME)
                    .BraceIn();
                foreach (var n in enumNames)
                {
                    tfEnum.AddS(n + ",");
                }
                tfEnum.BraceOut();

                tfClass.AddLine();

                TextFile tfSync = tfClass.AddS("public static void Sync(IReadableBuffer r, {0} rv)", Helper.GetTypeFullName(type))
                    .BraceIn();
                TextFile tfSwitch = tfSync.AddS("{0} op = ({0})r.{1}();", Compiler_Config.ENUM_NAME, Helper.Type2ReadMethod(typeof(int)))
                    .AddS("switch (op)")
                    .BraceIn();

                foreach (var n in enumNames)
                {
                    tfSwitch
                        .AddS("case {0}.{1}:", Compiler_Config.ENUM_NAME, n)
                        .In()
                        .AddS("{0}_sync(r, rv);", n)
                        .AddS("break;")
                        .Out();
                }
                tfSwitch.AddS("default:").In().AddS("break;").Out();

                tfSwitch.BraceOut();
                tfSync.BraceOut();
            }

            Console.Write(tfFile.Format(-1));
            File.WriteAllText(outputDir + "\\" + Compiler_Config.GetTypeGenClassName(type) + ".cs", tfFile.Format(-1));
        }
    }
}
