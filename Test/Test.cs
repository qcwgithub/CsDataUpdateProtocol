
using Swift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nova;
using dp;

namespace CsDataUpdateProtocol
{
    class Test
    {
        public static void DoTest()
        {
            //TextFile t = new TextFile();
            //t.AddS("using System;")
            //    .AddS("using System.Collections.Generic;")
            //    .AddLine();

            //t.AddS("public class Hello")
            //    .BraceIn()
            //    .AddS(@"print(""hello"");")
            //    .BraceOut();

            //string s = t.Format(-1);
            //Console.Write(s);

#if COMPILE
            new dp.Compiler().Compile(typeof(UserInfo), "D:\\Code\\CsDataUpdateProtocol\\Result\\Gen");
#else
            UserInfo infoServer = new UserInfo();
            UserInfo infoClient = new UserInfo();

            NetData d = new NetData();
            WriteableBuffer w = new WriteableBuffer(d);

            dpNova_UserInfo x = new dpNova_UserInfo(infoServer, null);
            {
                //var n = new ActorHeroData();
                //n.Name = "qiucw";
                //infoServer.DH.Add(1, n);
                //x.DH().Set(w, 1, n);

                //infoServer.DH.Clear();
                //x.DH().Remove(w, 1);
            }

            {
                //infoServer.Lst.Add(66);
                //infoServer.Lst.Add(77);
                //x.Lst().Add(w, 2);

                //infoServer.Lst.Insert(1, 88);
                //x.Lst().Insert(w, 1);

                //infoServer.Lst.RemoveAt(2);
                //x.Lst().RemoveByIndex(w, 2);

                infoServer.Lst.Clear();
                x.Lst().Clear(w);
            }

            {
                //ActorHeroData heroData = new ActorHeroData();
                //heroData.UniqueID = 123456;
                //heroData.Name = "霜狼督军";
                //infoServer.Heros.Add(heroData);

                //x.Heros().Add(w, 1);

                //heroData.Name = "恶魔猎手";
                //x.Heros().GetByIndex(0).Name_Update(w);

                //heroData.Name = "范克里夫";
                //x.Heros().RemoveByUid(w, heroData.UniqueID);
            }

            {
                infoServer.Money = 999;
                x.Money_Update(w);
            }

            ReadableBuffer r = new ReadableBuffer(d);
            dpHook hook = new dpHook();
            StringBuilder sb = new StringBuilder();
            hook.Action1 = () =>
            {
                List<object> L = hook.Process;
                sb.Remove(0, sb.Length);


                //sb.AppendLine("hook1: ");
                for (int i = 0; i < L.Count; i++)
                {
                    object obj = L[i];
                    if (obj == null)
                        sb.Append(" - null");
                    else
                    {
                        Type tObj = obj.GetType();
                        if (tObj.IsEnum)
                        {
                            if (i > 0)
                            {
                                sb.AppendLine();

                                //if (i < L.Count - 1)
                                    //sb.Append(" - ");
                            }
                            sb.Append(tObj.Name + "." + obj.ToString());
                            //s = tObj.DeclaringType.Name + "." + obj.ToString();
                            //sb.Append(tObj.DeclaringType.Name + "." + tObj.Name + "." + obj.ToString());
                        }
                        else
                            sb.Append(" - " + obj.ToString());
                    }
                }

                sb.AppendLine();
                if (hook.MatchOps(dpNova_UserInfo.Op.Lst, dpList_Nova_ActorHeroData.Op.Clear))
                {
                    sb.AppendLine("Lst Will Be Clear");
                }
                if (hook.MatchOps(dpNova_UserInfo.Op.Money_Update))
                {
                    sb.AppendLine("Money will be update, old money = " + infoClient.Money);
                }
                Console.Write(sb.ToString());
                Console.WriteLine();
            };

            hook.Action2 = () =>
            {
                if (hook.MatchOps(dpNova_UserInfo.Op.Money_Update))
                {
                    Console.WriteLine("new money = " + infoClient.Money);
                }
            };

            infoClient.Money = 222;
            while (r.Available > 0)
            {
                hook.Clear();
                dpNova_UserInfo.Sync(hook, r, infoClient);
            }

            Console.WriteLine("PerfectEnd ? " + (d.PerfectEnd() ? "1" : "0"));

#endif
        }
    }
}
