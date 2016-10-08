using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nova;

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

        protected void Write(IWriteableBuffer w, object o)
        {
            Type t = o.GetType();

            if (t.IsEnum || t == typeof(int))
                w.Write((int)o);
            else if (t == typeof(long))
                w.Write((long)o);
            else if (t == typeof(ulong))
                w.Write((ulong)o);
            else if (t == typeof(string))
                w.Write((string)o);
            else if (o is ISerializable)
                w.Write((ISerializable)o);
            else if (t == typeof(bool))
                w.Write((bool)o);
            else if (t == typeof(float))
                w.Write((float)o);
            else if (t == typeof(double))
                w.Write((double)o);
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
