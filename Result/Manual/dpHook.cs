using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dp
{
    public class dpHook
    {
        List<object> process = new List<object>();
        public List<object> Process
        {
            get
            {
                return process;
            }
        }
        public void Clear()
        {
            process.Clear();
        }
        public void Push(object p)
        {
            process.Add(p);
        }
        public Action Action1 = null;
        public void DoAction1()
        {
            if (Action1 != null)
                Action1();
        }
        public Action Action2 = null;
        public void DoAction2()
        {
            if (Action2 != null)
                Action2();
        }
        public bool MatchOps(object main, params object[] others)
        {
            if (!(process.Count > 0 && (int)process[0] == (int)main))
                return false;

            for (int i = 0; i < others.Length; i++)
            {
                if (!(process.Count > 1 + i && (int)process[1 + i] == (int)others[i]))
                    return false;
            }

            return true;
        }
        //public bool Match(object main, params object[] others)
        //{
        //    if (!(process.Count > 0 && process[0] == main))
        //        return false;

        //    for (int i = 0; i < others.Length; i++)
        //    {
        //        if (!(process.Count > 1 + i && process[1 + i] == others[i]))
        //            return false;
        //    }

        //    return true;
        //}
        public Dictionary<string, object> UserData = new Dictionary<string,object>();
    }
}
