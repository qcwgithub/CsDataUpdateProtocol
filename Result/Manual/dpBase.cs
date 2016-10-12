using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nova;
using Swift;

namespace dp
{
    public class dpBase
    {
        public List<object> Ops = new List<object>();

        protected void Init(dpBase parent, params object[] ops)
        {
            if (parent != null)
            {
                Ops.AddRange(parent.Ops);
            }

            if (ops != null)
            {
                for (int i = 0; i < ops.Length; i++)
                    Ops.Add(ops[i]);
            }
        }

        static Dictionary<Type, Action<IWriteableBuffer, object>> dw;
        static void Write(IWriteableBuffer w, Type t, object o)
        {
            if (dw == null)
            {
                dw = new Dictionary<Type, Action<IWriteableBuffer, object>>();

                dw[typeof(string)] = (_w, _o) => { _w.Write((string)_o); };
                dw[typeof(string[])] = (_w, _o) => { _w.Write((string[])_o); };

                dw[typeof(ISerializable)] = (_w, _o) => { _w.Write((ISerializable)_o); };
                dw[typeof(ISerializable[])] = (_w, _o) => { _w.Write((ISerializable[])_o); };

                dw[typeof(char)] = (_w, _o) => { _w.Write((char)_o); };
                dw[typeof(char[])] = (_w, _o) => { _w.Write((char[])_o); };

                dw[typeof(byte)] = (_w, _o) => { _w.Write((byte)_o); };
                dw[typeof(byte[])] = (_w, _o) => { _w.Write((byte[])_o); };

                dw[typeof(bool)] = (_w, _o) => { _w.Write((bool)_o); };
                dw[typeof(bool[])] = (_w, _o) => { _w.Write((bool[])_o); };

                dw[typeof(short)] = (_w, _o) => { _w.Write((short)_o); };
                dw[typeof(short[])] = (_w, _o) => { _w.Write((short[])_o); };

                dw[typeof(int)] = (_w, _o) => { _w.Write((int)_o); };
                dw[typeof(int[])] = (_w, _o) => { _w.Write((int[])_o); };

                dw[typeof(long)] = (_w, _o) => { _w.Write((long)_o); };
                dw[typeof(long[])] = (_w, _o) => { _w.Write((long[])_o); };

                dw[typeof(ulong)] = (_w, _o) => { _w.Write((ulong)_o); };
                dw[typeof(ulong[])] = (_w, _o) => { _w.Write((ulong[])_o); };

                dw[typeof(float)] = (_w, _o) => { _w.Write((float)_o); };
                dw[typeof(float[])] = (_w, _o) => { _w.Write((float[])_o); };

                dw[typeof(double)] = (_w, _o) => { _w.Write((double)_o); };
                dw[typeof(double[])] = (_w, _o) => { _w.Write((double[])_o); };
            }

            if (t.IsEnum)
                dw[typeof(int)](w, o);
            else if (o is ISerializable)
                dw[typeof(ISerializable)](w, o);
            else if (o is ISerializable[])  // OK?
                dw[typeof(ISerializable[])](w, o);
            else if (dw.ContainsKey(t))
                dw[t](w, o);
            else
                throw new Exception("dpBase unknown type " + t.Name);
        }

        protected void Write(IWriteableBuffer w, object o)
        {
            Type t = o.GetType();
            Write(w, t, o);
        }

        protected void Write(IWriteableBuffer w, params object[] objs)
        {
            foreach (var o in objs)
                Write(w, o);
        }

        protected void Write(IWriteableBuffer w, List<object> lst, params object[] objs)
        {
            foreach (var o in lst)
                Write(w, o);

            Write(w, objs);
        }
    }
}
