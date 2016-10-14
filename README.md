# CsDataUpdateProtocol

功能：生成协议代码，使得2个同类型的c#类对象以及其子对象可以方便地进行数据同步。
这2个同类型的c#类对象在我这里是一个在服务端，一个在客户端。用于服务器把数据同步给客户端。

这个c#对象的类型可以是 class、基础类型、List、Dictionary。如果要加其他类型也是很方便的。
类型可以互相嵌套

例子：
```CSharp
    // 英雄数据
    class ActorHeroData
    {
        public string Exp; // 英雄经验
    }

    // 玩家数据
    class UserInfo
    {
        public int Money; // 金钱
        public int Level; // 等级
    
        public ActorHeroData MainHero; // 主英雄
        public List<ActorHeroData> Heros; // 所有英雄
    }


    // 服务器代码
    {
        UserInfo info; // 玩家数据
        IWriteableBuffer w; // 代表一个缓冲区，用于发送数据
        var dp = new dp.dpNova_UserInfo(w);
        
        // 金钱发生变化，同步给客户端
        info.Money -= 200;
        dp.Money_Set(info.Money);
        
        // 英雄经验发生变化，同步给客户端
        info.MainHero.Exp += 100;
        dp.MainHero.Exp_Set(info.MainHero.Exp);
        
        // 第i个英雄发生变化，同步给客户端
        info.Heros[0].Exp += 50;
        dp.Heros[0].Exp_Set(info.Heros[0].Exp);
        
        SendMessage(w);
    }

    // 客户端代码
    {
        UserInfo info;
        IReadableBuffer r; // 代表一个缓冲区，用于接收服务器数据
        
        while (r.Available > 0) { // 一直读数据，直到结束
            dp.dpNova_UserInfo.Update(r, info);
        }
        
        // 以上代码执行完成后，客户端的金钱、英雄经验等均自动与服务器同步了
    }
```    