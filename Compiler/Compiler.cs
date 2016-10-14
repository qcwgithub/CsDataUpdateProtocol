using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Runtime;
using System.Runtime.InteropServices;
using System.CodeDom;
using System.CodeDom.Compiler;
using Nova;
using Swift;

namespace dp
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

        bool TypeIsBasic(Type type)
        {
            return type.IsEnum ||
                type == typeof(bool) ||
                type == typeof(char) ||
                type == typeof(byte) ||
                type == typeof(sbyte) ||
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

        bool TypeIsISerializable___(Type type)
        {
            return typeof(ISerializable).IsAssignableFrom(type);
        }
        
        bool TypeIsSerializable(Type type)
        {
            return TypeIsBasic(type) || TypeIsISerializable___(type);
        }

        bool TypeCanBeExported(Type type)
        {
            if (TypeIsSerializable(type))
                return true;

            if (Helper.TypeIsDict(type))
            {
                Type[] arr = type.GetGenericArguments();
                bool keyOk = TypeIsBasic(arr[0]);
                bool valueOk = TypeIsSerializable(arr[1]);
                return (keyOk && valueOk);
            }
            else if (Helper.TypeIsList(type))
            {
                Type typeOfT = type.GetGenericArguments()[0];
                return (TypeIsSerializable(typeOfT));
            }
            return false;
        }

        List<object[]> GetTypeSupportFieldsAndPros(Type type)
        {
            List<object[]> l = new List<object[]>();

            FieldInfo[] fields = type.GetFields(BindingFlagsField);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];

                // readonly
                if (!field.IsInitOnly &&
                    TypeCanBeExported(field.FieldType)
                    )
                {
                    l.Add(new object[]{ field.FieldType, field.Name });
                }
            }

            PropertyInfo[] properties = type.GetProperties(BindingFlagsProperty);
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo pro = properties[i];

                if (pro.CanRead && pro.CanWrite &&
                    TypeCanBeExported(pro.PropertyType)
                    )
                {
                    l.Add(new object[] { pro.PropertyType, pro.Name });
                }
            }

            return l;
        }

        HashSet<Type> hsAllTypes = null;
        void TravelGetTypes(Type type)
        {
            if (hsAllTypes.Contains(type))
                return;

            if (!TypeCanBeExported(type))
                return;

            if (Helper.TypeIsDict(type))
            {
                hsAllTypes.Add(type);
                TravelGetTypes(type.GetGenericArguments()[1]);
            }
            else if (Helper.TypeIsList(type))
            {
                hsAllTypes.Add(type);
                TravelGetTypes(type.GetGenericArguments()[0]);
            }
            else if (TypeIsISerializable___(type))
            {
                hsAllTypes.Add(type);

                List<object[]> l = GetTypeSupportFieldsAndPros(type);
                foreach (var a in l)
                {
                    TravelGetTypes((Type)a[0]);
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

            string keyFullname = Helper.GetTypeFullName(typeOfKey);
            string valueFullname = Helper.GetTypeFullName(typeOfValue);

            {
                enumNames.Add("Clear");
                tf.AddS("public void Clear()")
                    .BraceIn()
                    .AddS("Write(Ops, {0}.Clear);", Compiler_Config.ENUM_NAME)
                    .BraceOut();

                tf.AddS("public static void Clear_Sync(dpHook hook, IReadableBuffer r, {0} rv)", Helper.GetTypeFullName(typeOfDict))
                    .BraceIn()
                    .AddS("hook.DoAction1();")
                    //.In()
                        .AddS("rv.Clear();")
                    //.Out()
                    .AddS("hook.DoAction2();")
                    .BraceOut();

                tf.AddLine();
            }

            {
                enumNames.Add("Remove");
                tf.AddS("public void Remove({0} key)", keyFullname)
                    .BraceIn()
                    .AddS("Write(Ops, {0}.Remove, key);", Compiler_Config.ENUM_NAME)
                    .BraceOut();

                tf.AddS("public static void Remove_Sync(dpHook hook, IReadableBuffer r, {0} rv)", Helper.GetTypeFullName(typeOfDict))
                    .BraceIn()
                    .AddS("{0} key = {1}();", keyFullname, Helper.Type2ReadMethod("r", typeOfKey))
                    .AddS("hook.Push(key);")
                    .AddS("hook.DoAction1();")
                    //.In()
                    .AddS("rv.Remove(key);")
                    //.Out()
                    .AddS("hook.DoAction2();")
                    .BraceOut();

                tf.AddLine();
            }

            {
                enumNames.Add("Set");
                tf.AddS("public void Set({0} key, {1} value_)", keyFullname, valueFullname)
                    .BraceIn()
                        .AddS("Write(Ops, {0}.Set, key, value_);", Compiler_Config.ENUM_NAME)
                    .BraceOut();

                tf.AddS("public static void Set_Sync(dpHook hook, IReadableBuffer r, {0} rv)", Helper.GetTypeFullName(typeOfDict))
                    .BraceIn()
                    .AddS("{0} key = {1}();", Helper.GetTypeFullName(typeOfKey), Helper.Type2ReadMethod("r", typeOfKey), Helper.Type2ReadMethod("r", typeOfValue))
                    .AddS("{0} value_ = {1}();", valueFullname, Helper.Type2ReadMethod("r", typeOfValue), Helper.Type2ReadMethod("r", typeOfValue))
                    .AddS("hook.Push(key);")
                    .AddS("hook.Push(value_);")
                    .AddS("hook.DoAction1();")
                    //.In()
                        .AddS("rv.Remove(key);")
                        .AddS("rv.Add(key, value_);")
                    //.Out()
                    .AddS("hook.DoAction2();")
                    .BraceOut();

                tf.AddLine();
            }
        }

        void CreateListFuncs(TextFile tf, Type typeOfList, List<string> enumNames)
        {
            Type typeOfT = typeOfList.GetGenericArguments()[0];

            // Clear
            {
                tf.AddS("// Clear");
                enumNames.Add("Clear");
                tf.AddS("public void Clear()")
                    .BraceIn()
                    .AddS("Write(Ops, {0}.Clear);", Compiler_Config.ENUM_NAME)
                    .BraceOut();

                tf.AddS("public static void Clear_Sync(dpHook hook, IReadableBuffer r, {0} rv)", Helper.GetTypeFullName(typeOfList))
                    .BraceIn()
                    .AddS("hook.DoAction1();")
                    //.In()
                        .AddS("rv.Clear();")
                    //.Out()
                    .AddS("hook.DoAction2();")
                    .BraceOut();

                tf.AddLine();
            }

            // Add
            {
                enumNames.Add("Add");
                tf.AddS("// 在调用这个函数之前，服务器必须已经Add完毕");
                TextFile tfAdd = tf.AddS("public void Add_Count(int count)")
                    .BraceIn()
                        .AddS("Write(Ops, {0}.Add, count);", Compiler_Config.ENUM_NAME)
                        .AddS("for (int i = v.Count - count; i < v.Count; i++)")
                        .BraceIn()
                            .AddS("Write(v[i]);")
                        .BraceOut()
                    .BraceOut();

                tf.AddS("// Add一个指定值");
                tf.AddS("public void Add_Value({0} value_)", Helper.GetTypeFullName(typeOfT))
                    .BraceIn()
                        .AddS("Write(Ops, {0}.Add, 1);", Compiler_Config.ENUM_NAME)
                        .AddS("Write(value_);")
                    .BraceOut();

                tf.AddS("public static void Add_Sync(dpHook hook, IReadableBuffer r, {0} rv)", Helper.GetTypeFullName(typeOfList))
                    .BraceIn()
                        .AddS("int count = {0}();", Helper.Type2ReadMethod("r", typeof(int)))
                        .AddS("hook.Push(count);")
                        .AddS("hook.DoAction1();")
                        //.In()
                            .AddS("for (int i = 0; i < count; i++)")
                            .BraceIn()
                                .AddS("rv.Add({0}());", Helper.Type2ReadMethod("r", typeOfT))
                            .BraceOut()
                        //.Out()
                        .AddS("hook.DoAction2();")
                    .BraceOut();

                tf.AddLine();
            }

            // Insert
            {
                tf.AddS("// Insert");
                enumNames.Add("Insert");
                tf.AddS("public void Insert(int index)")
                    .BraceIn()
                    .AddS("Write(Ops, {0}.Insert, index, v[index]);", Compiler_Config.ENUM_NAME)
                    .BraceOut();


                tf.AddS("public void Insert(int index, {0} value_)", Helper.GetTypeFullName(typeOfT))
                    .BraceIn()
                    .AddS("Write(Ops, {0}.Insert, index, value_);", Compiler_Config.ENUM_NAME)
                    .BraceOut();

                tf.AddS("public static void Insert_Sync(dpHook hook, IReadableBuffer r, {0} rv)", Helper.GetTypeFullName(typeOfList))
                    .BraceIn()
                        .AddS("int index = {0}();", Helper.Type2ReadMethod("r", typeof(int)))
                        .AddS("hook.Push(index);")
                        .AddS("hook.DoAction1();")
                        //.In()
                            .AddS("rv.Insert(index, {0}());", Helper.Type2ReadMethod("r", typeOfT))
                        //.Out()
                        .AddS("hook.DoAction2();")
                    .BraceOut();

                tf.AddLine();
            }

            // RemoveAt
            {
                tf.AddS("// 移除第i个元素");
                enumNames.Add("RemoveAt");
                tf.AddS("public void RemoveAt(int index)")
                    .BraceIn()
                    .AddS("Write(Ops, {0}.RemoveAt, index);", Compiler_Config.ENUM_NAME)
                    .BraceOut();

                tf.AddS("public static void RemoveAt_Sync(dpHook hook, IReadableBuffer r, {0} rv)", Helper.GetTypeFullName(typeOfList))
                    .BraceIn()
                        .AddS("int index = {0}();", Helper.Type2ReadMethod("r", typeof(int)))
                        .AddS("hook.Push(index);")
                        .AddS("hook.DoAction1();")
                        //.In()
                            .AddS("rv.RemoveAt(index);")
                        //.Out()
                        .AddS("hook.DoAction2();")
                    .BraceOut();

                tf.AddLine();
            }

            if (TypeIsISerializable___(typeOfT))
            {
                tf.AddS("// GetByIndex");
                enumNames.Add("GetByIndex");
                tf.AddS("public {0} GetByIndex(int index)", Compiler_Config.GetTypeGenClassName(typeOfT))
                    .BraceIn()
                    .AddS("return new {0}(w, v[index], this, {1}.GetByIndex, index);", Compiler_Config.GetTypeGenClassName(typeOfT), Compiler_Config.ENUM_NAME)
                    .BraceOut();
                tf.AddS("public {0} this[int index]", Compiler_Config.GetTypeGenClassName(typeOfT))
                    .BraceIn()
                        .ProGetIn()
                        .AddS("return this.GetByIndex(index);")
                        .ProGetOut()
                    .BraceOut();

                tf.AddS("public static void GetByIndex_Sync(dpHook hook, IReadableBuffer r, {0} rv)", Helper.GetTypeFullName(typeOfList))
                    .BraceIn()
                    .AddS("int index = {0}();", Helper.Type2ReadMethod("r", typeof(int)))
                    .AddS("hook.Push(index);")
                    .AddS("{0}.Sync(hook, r, rv[index]);", Compiler_Config.GetTypeGenClassName(typeOfT))
                    .BraceOut();

                tf.AddLine();
            }

            if (typeof(SerializableDataWithID).IsAssignableFrom(typeOfT))
            {
                tf.AddS("// GetByUid");
                enumNames.Add("GetByUid");
                tf.AddS("public {0} GetByUid(ulong uid)", Compiler_Config.GetTypeGenClassName(typeOfT))
                    .BraceIn()
                    .AddS("{0} _y = v.Find((_x) => _x.UniqueID == uid);", Helper.GetTypeFullName(typeOfT))
                    .AddS("return new {0}(w, _y, this, {1}.GetByUid, uid);", Compiler_Config.GetTypeGenClassName(typeOfT), Compiler_Config.ENUM_NAME)
                    .BraceOut();
                tf.AddS("public {0} this[ulong uid]", Compiler_Config.GetTypeGenClassName(typeOfT))
                    .BraceIn()
                    .AddS("get")
                    .BraceIn()
                    .AddS("return this.GetByUid(uid);")
                    .BraceOut()
                    .BraceOut();

                tf.AddS("public static void GetByUid_Sync(dpHook hook, IReadableBuffer r, {0} rv)", Helper.GetTypeFullName(typeOfList))
                    .BraceIn()
                    .AddS("ulong uid = {0}();", Helper.Type2ReadMethod("r", typeof(ulong)))
                    .AddS("hook.Push(uid);")
                    .AddS("{0} _y = rv.Find((_x) => _x.UniqueID == uid);", Helper.GetTypeFullName(typeOfT))
                    .AddS("{0}.Sync(hook, r, _y);", Compiler_Config.GetTypeGenClassName(typeOfT))
                    .BraceOut();

                tf.AddLine();

                tf.AddS("// RemoveByUid");
                enumNames.Add("RemoveByUid");
                tf.AddS("public void RemoveByUid(ulong uid)")
                    .BraceIn()
                    .AddS("Write(Ops, {0}.RemoveByUid, uid);", Compiler_Config.ENUM_NAME)
                    .BraceOut();

                tf.AddS("public static void RemoveByUid_Sync(dpHook hook, IReadableBuffer r, {0} rv)", Helper.GetTypeFullName(typeOfList))
                    .BraceIn()
                        .AddS("ulong uid = {0}();", Helper.Type2ReadMethod("r", typeof(ulong)))
                        .AddS("hook.Push(uid);")
                        .AddS("hook.DoAction1();")
                        //.In()
                            .AddS("{0} _y = rv.Find((_x) => _x.UniqueID == uid);", Helper.GetTypeFullName(typeOfT))
                            .AddS("rv.Remove(_y);")
                        //.Out()
                        .AddS("hook.DoAction2();")
                    .BraceOut();

                tf.AddLine();
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
                    .AddS("using Swift;")
                    .AddLine();
            }

            // namespace
            TextFile tfNamespace;
            {
                tfNamespace = tfFile.AddS("namespace dp")
                    .BraceIn();
                tfNamespace.BraceOut().AddLine();
            }

            List<string> enumNames = new List<string>();

            TextFile tfClass = tfNamespace.AddS("public class {0} : {1}", Compiler_Config.GetTypeGenClassName(type), Compiler_Config.BASE_NAME)
                .BraceIn();
            tfClass.BraceOut();

            // constructor
            {
                //tfClass.AddS("public {0}()", Compiler_Config.GetTypeGenClassName(type))
                //    .BraceIn().BraceOut();


                tfClass.AddS("public {0}(IWriteableBuffer w, {1} v, {2} parent, params object[] ops) : base(w, parent, ops)",
                    Compiler_Config.GetTypeGenClassName(type), Helper.GetTypeFullName(type), Compiler_Config.BASE_NAME)

                    .BraceIn()
                    .AddS("this.v = v;")
                    //.AddS("Init(parent, ops);")
                    .BraceOut();
            }

            // v
            {
                tfClass.AddLine().AddS("{0} v;", Helper.GetTypeFullName(type)).AddLine();
            }

            if (Helper.TypeIsList(type) && TypeCanBeExported(type))
            {
                CreateListFuncs(tfClass, type, enumNames);
            }
            else if (Helper.TypeIsDict(type) && TypeCanBeExported(type))
            {
                CreateDictFuncs(tfClass, type, enumNames);
            }
            else
            {
                // fields & properties
                {
                    List<object[]> lst = GetTypeSupportFieldsAndPros(type);
                    foreach (var l in lst)
                    {
                        Type mType = (Type)l[0];
                        string mName = (string)l[1];

                        if (TypeIsSerializable(mType))
                        {
                            // 可序列化对象产生 Update 函数

                            string enumName = mName + "_Update";
                            enumNames.Add(enumName);

                            tfClass.AddS("public void {0}_Update()", mName)
                            .BraceIn()
                            .AddS("Write(Ops, {0}.{1}, v.{2});", Compiler_Config.ENUM_NAME, enumName, mName)
                            .BraceOut();

                            tfClass.AddS("public void {0}_Set({1} value_)", mName, Helper.GetTypeFullName(mType))
                            .BraceIn()
                            .AddS("Write(Ops, {0}.{1}, value_);", Compiler_Config.ENUM_NAME, enumName)
                            .BraceOut();

                            tfClass.AddS("public static void {0}_Update_Sync(dpHook hook, IReadableBuffer r, {1} rv)", mName, Helper.GetTypeFullName(type))
                                .BraceIn()
                                .AddS("var nv = {0}();", Helper.Type2ReadMethod("r", mType))
                                .AddS("hook.Push(nv);")
                                .AddLine()
                                .AddS("hook.DoAction1();")
                                .AddS("rv.{0} = nv;", mName)
                                .AddS("hook.DoAction2();")
                                .BraceOut();

                            tfClass.AddLine();
                        }

                        if (!TypeIsBasic(mType))
                        {
                            enumNames.Add(mName);
                            // 不要用函数形式了
                            //tfClass.AddS("public {0} {1}()", Compiler_Config.GetTypeGenClassName(mType), mName)
                            //    .BraceIn()
                            //    .AddS("return new {0}(w, v.{1}, this, {2}.{1});",
                            //        Compiler_Config.GetTypeGenClassName(mType),
                            //        mName, Compiler_Config.ENUM_NAME)
                            //    .BraceOut();
                            tfClass.AddS("public {0} {1}", Compiler_Config.GetTypeGenClassName(mType), mName)
                                .BraceIn()
                                .ProGetIn()
                                .AddS("return new {0}(w, v.{1}, this, {2}.{1});",
                                    Compiler_Config.GetTypeGenClassName(mType),
                                    mName, Compiler_Config.ENUM_NAME)
                                .ProGetOut()
                                .BraceOut();

                            tfClass.AddS("public static void {0}_Sync(dpHook hook, IReadableBuffer r, {1} rv)", mName, Helper.GetTypeFullName(type))
                                .BraceIn()
                                .AddS("{0}.Sync(hook, r, rv.{1});", Compiler_Config.GetTypeGenClassName(mType), mName)
                                .BraceOut();

                            tfClass.AddLine();
                        }
                    }
                }
            }

            if (enumNames.Count > 0)
            {
                // 枚举定义

                TextFile tfEnum = tfClass.AddS("public enum {0}", Compiler_Config.ENUM_NAME)
                    .BraceIn();
                int enumV = 0;
                foreach (var n in enumNames)
                {
                    tfEnum.AddS("{0} = {1},", n, enumV);
                    enumV++;
                }
                tfEnum.BraceOut();

                tfClass.AddLine();

                // Sync函数

                tfClass.AddS("// 客户端使用");
                TextFile tfSync = tfClass.AddS("public static void Sync(dpHook hook, IReadableBuffer r, {0} rv)", Helper.GetTypeFullName(type))
                    .BraceIn();
                TextFile tfSwitch = tfSync.AddS("{0} op = ({0}){1}();", Compiler_Config.ENUM_NAME, Helper.Type2ReadMethod("r", typeof(int)))
                    .AddS("hook.Push(op);")
                    .AddS("switch (op)")
                    .AddS("{");
                    //.BraceIn();

                foreach (var n in enumNames)
                {
                    tfSwitch
                        .AddS("case {0}.{1}:", Compiler_Config.ENUM_NAME, n)
                        .In()
                        .AddS("{0}_Sync(hook, r, rv);", n)
                        .AddS("break;")
                        .Out();
                }
                tfSwitch.AddS("default:").In().AddS("break;").Out();

                tfSwitch.AddS("}");
                //tfSwitch.BraceOut();

                tfSync.BraceOut();
            }

            Console.Write(tfFile.Format(-1));
            Directory.CreateDirectory(outputDir);
            File.WriteAllText(outputDir + "\\" + Compiler_Config.GetTypeGenClassName(type) + ".cs", tfFile.Format(-1));
        }
    }
}
