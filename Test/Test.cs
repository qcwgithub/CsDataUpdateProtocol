
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

            new dp.Compiler().Compile(typeof(UserInfo), "D:\\Code\\CsDataUpdateProtocol\\Gen");
            return;


            UserInfo infoServer = new UserInfo();
            UserInfo infoClient = new UserInfo();

            //infoServer.Money = 5;
            //infoServer.hero = new ActorHeroData();
            //infoServer.hero.Name = "fuck";

            //infoClient.hero = new ActorHeroData();

            infoServer.Lst = new List<int>();
            infoServer.Heros = new List<ActorHeroData>();
            infoServer.DH = new Dictionary<int, ActorHeroData>();

            NetData d = new NetData();
            WriteableBuffer w = new WriteableBuffer(d);

            dpNova_UserInfo x = new dpNova_UserInfo(infoServer, null);
            {
                var n = new ActorHeroData();
                n.Name = "qiucw";
                infoServer.DH.Add(1, n);
                x.DH().Set(w, 1, n);

                infoServer.DH.Clear();
                x.DH().Remove(w, 1);
            }

            {
                infoServer.Lst.Add(66);
                infoServer.Lst.Add(77);
                new dpNova_UserInfo(infoServer, null).Lst().Add(w, 2);

                infoServer.Lst.Insert(1, 88);
                new dpNova_UserInfo(infoServer, null).Lst().Insert(w, 1);

                infoServer.Lst.RemoveAt(2);
                new dpNova_UserInfo(infoServer, null).Lst().RemoveByIndex(w, 2);
            }
            //{
            //    ActorHeroData heroData = new ActorHeroData();
            //    heroData.UniqueID = 123456;
            //    heroData.Name = "霜狼督军";
            //    infoServer.Heros.Add(heroData);

            //    new dpNova_UserInfo(infoServer, null).Heros().Add(w, 1);

            //    heroData.Name = "恶魔猎手";
            //    new dpNova_UserInfo(infoServer, null).Heros().GetByIndex(0).Name_Update(w);

            //    heroData.Name = "范克里夫";
            //    new dpNova_UserInfo(infoServer, null).Heros().RemoveByUid(w, heroData.UniqueID);
            //}

            infoClient.Lst = new List<int>();
            infoClient.Heros = new List<ActorHeroData>();
            infoClient.DH = new Dictionary<int, ActorHeroData>();
            ReadableBuffer r = new ReadableBuffer(d);
            while (r.Available > 0)
                dpNova_UserInfo.Sync(r, infoClient);

            //Console.WriteLine(infoClient.Lst[0]);
            Console.WriteLine("PerfectEnd ? " + (d.PerfectEnd() ? "1" : "0"));
        }
    }
}
