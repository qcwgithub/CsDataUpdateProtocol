//#define COMPILE_ACTORHERODATA
//#define TEST_ACTORHERODATA
//#define COMPILE_USERINFO
#define TEST_USERINFO

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
#if COMPILE_ACTORHERODATA
            new dp.Compiler().Compile(typeof(ActorHeroData), "D:\\Code\\CsDataUpdateProtocol\\Result\\Gen");
#elif TEST_ACTORHERODATA
            NetData d = new NetData();
            #region server
            {
                UserDataNotifier UDN = new UserDataNotifier(d);
                ActorHeroData heroData = new ActorHeroData();

                UDN.Notify(heroData, (dp) =>
                {
                    heroData.Color = 5;
                    dp.Color_Update();
                });
            }
            #endregion
            #region client
            {
                ActorHeroData heroData = new ActorHeroData();
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
                    Console.Write(sb.ToString());
                    Console.WriteLine();
                };

                while (r.Available > 0)
                {
                    hook.Clear();
                    dpNova_ActorHeroData.Sync(hook, r, heroData);
                }

                Console.WriteLine("PerfectEnd ? " + (d.PerfectEnd() ? "1" : "0"));
            }
            #endregion
#endif

#if COMPILE_USERINFO
            new dp.Compiler().Compile(typeof(UserInfo), "D:\\Code\\CsDataUpdateProtocol\\Result\\Gen");
#elif TEST_USERINFO
            NetData d = new NetData();

            #region server
            {
                UserDataNotifier UDN = new UserDataNotifier(d);

                UserInfo info = new UserInfo();
                UDN.Notify(info, (dp) =>
                {
                    {
                        //var n = new ActorHeroData();
                        //n.Name = "qiucw";
                        //infoServer.DH.Add(1, n);
                        //infoServer.dp().DH().Set(w, 1, n);

                        //infoServer.DH.Clear();
                        //infoServer.dp().DH().Remove(w, 1);
                    }

                    {
                        info.Lst.Add(66);
                        info.Lst.Add(77);
                        dp.Lst.Add_Value(66);
                        dp.Lst.Add_Value(77);

                        info.Lst.Insert(1, 88);
                        dp.Lst.Insert(1);

                        info.Lst.RemoveAt(2);
                        dp.Lst.RemoveAt(2);

                        info.Lst.Clear();
                        dp.Lst.Clear();
                    }

                    {
                        ItemInfo equip = new ItemInfo() { UniqueID = 786897 };
                        info.Hero.Equips.Add(equip);
                        dp.Hero.Equips.Add_Value(equip);

                        equip.Num = 56;
                        //dp.Hero().Equips().GetByIndex(0).Num_Update();
                        //dp.Hero().Equips()[0].Num_Update();
                        //dp.Hero().Equips().GetByUid(786897).Num_Update();
                        dp.Hero.Equips[equip.UniqueID].Num_Update();
                    }

                    {
                        ActorHeroData heroData = new ActorHeroData();
                        heroData.UniqueID = 123456;
                        heroData.Name = "霜狼督军";

                        info.Heros.Add(heroData);
                        dp.Heros.Add_Count(1);

                        heroData.Name = "恶魔猎手";
                        dp.Heros[0].Name_Update();

                        info.Heros.Remove(heroData);
                        dp.Heros.RemoveByUid(heroData.UniqueID);
                    }

                    {
                        info.Money = 999;
                        dp.Money_Update();

                        info.Level = 78;
                        dp.Level_Update();

                        info.Level = 80;
                        dp.Level_Update();
                    }
                });
            }

            #endregion
            #region client
            {
                UserInfo info = new UserInfo();
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
                        sb.AppendLine("Money will be update, old money = " + info.Money);
                    }
                    if (hook.MatchOps(dpNova_UserInfo.Op.Level_Update))
                    {
                        hook.UserData["oldLevel"] = info.Level;
                    }
                    Console.Write(sb.ToString());
                    Console.WriteLine();
                };

                hook.Action2 = () =>
                {
                    if (hook.MatchOps(dpNova_UserInfo.Op.Money_Update))
                    {
                        Console.WriteLine("new money = " + info.Money);
                    }
                    if (hook.MatchOps(dpNova_UserInfo.Op.Level_Update))
                    {
                        Console.WriteLine("OnLevelUp " + hook.UserData["oldLevel"] + " -> " + info.Level);
                    }
                };

                info.Money = 222;
                while (r.Available > 0)
                {
                    hook.Clear();
                    dpNova_UserInfo.Sync(hook, r, info);
                }

                Console.WriteLine("PerfectEnd ? " + (d.PerfectEnd() ? "1" : "0"));
            }
            #endregion
#endif
        }
    }
}
