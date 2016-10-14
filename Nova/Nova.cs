using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Swift;
using Nova;

namespace Swift
{

    public interface IReadableBuffer
    {
        int Available { get; }
        byte ReadByte();
        bool ReadBool();
        bool[] ReadBoolArr();
        short ReadShort();
        short[] ReadShortArr();
        int ReadInt();
        int[] ReadIntArr();
        long ReadLong();
        long[] ReadLongArr();
        ulong ReadULong();
        ulong[] ReadULongArr();
        float ReadFloat();
        float[] ReadFloatArr();
        double ReadDouble();
        double[] ReadDoubleArr();
        char ReadChar();
        char[] ReadCharArr();
        string ReadString();
        string[] ReadStringArr();
        T Read<T>() where T : ISerializable;
    }

    public interface IWriteableBuffer
    {
        void Write(byte[] src);
        void Write(byte v);
        void Write(bool v);
        void Write(bool[] v);
        void Write(short v);
        void Write(short[] v);
        void Write(int v);
        void Write(int[] v);
        void Write(long v);
        void Write(long[] v);
        void Write(ulong v);
        void Write(ulong[] v);
        void Write(float v);
        void Write(float[] v);
        void Write(double v);
        void Write(double[] v);
        void Write(char v);
        void Write(char[] v);
        void Write(string v);
        void Write(string[] v);
        void Write(ISerializable v);
        void Write(ISerializable[] v);
    }
    public interface ISerializable
    {
        void Serialize(IWriteableBuffer w);
        void Deserialize(IReadableBuffer r);
    }
}

namespace Nova
{

    public class SerializableData : ISerializable
    {
        public void Serialize(IWriteableBuffer w) { }
        public void Deserialize(IReadableBuffer r) { }
    }

    public class SerializableDataWithID : SerializableData
    {
        public ulong UniqueID;
    }
    public class ItemInfo : SerializableDataWithID
    {
        public int Num;
    }

    public class ActorHeroData : SerializableDataWithID
    {
        public int Color;
        public string Name;
        public List<ItemInfo> Equips = new List<ItemInfo>();
    }
    public class UserInfo : SerializableData
    {
        //public int Money;
        //public long Star;
        //public string Name;
        //public SearchOption so;
        public int Level;
        public ActorHeroData Hero = new ActorHeroData();
        public List<ActorHeroData> Heros = new List<ActorHeroData>();
        public List<int> Lst = new List<int>();
        public int Money;
        public Dictionary<int, ActorHeroData> DH = new Dictionary<int, ActorHeroData>();
    }
    public class NetData
    {
        public List<object> lst = new List<object>();
        public int i = 0;

        public T Next<T>() { return (T)lst[i++]; }
        public void Push(object o) { lst.Add(o);  }

        public bool PerfectEnd()
        {
            return i == lst.Count;
        }
        public int Available
        {
            get { return lst.Count - i; }
        }
    }


    public class ReadableBuffer : IReadableBuffer
    {
        NetData d;
        public ReadableBuffer(NetData d)
        {
            d.i = 0;
            this.d = d;
        }
        public int Available { get { return d.Available; } }
        public byte ReadByte() { return d.Next<byte>(); }
        public bool ReadBool() { return d.Next<bool>(); }
        public bool[] ReadBoolArr() { return d.Next<bool[]>(); }
        public short ReadShort() { return d.Next<short>(); }
        public short[] ReadShortArr() { return d.Next<short[]>(); }
        public int ReadInt() { return d.Next<int>(); }
        public int[] ReadIntArr() { return d.Next<int[]>(); }
        public long ReadLong() { return d.Next<long>(); }
        public long[] ReadLongArr() { return d.Next<long[]>(); }
        public ulong ReadULong() { return d.Next<ulong>(); }
        public ulong[] ReadULongArr() { return d.Next<ulong[]>(); }
        public float ReadFloat() { return d.Next<float>(); }
        public float[] ReadFloatArr() { return d.Next<float[]>(); }
        public double ReadDouble() { return d.Next<double>(); }
        public double[] ReadDoubleArr() { return d.Next<double[]>(); }
        public char ReadChar() { return d.Next<char>(); }
        public char[] ReadCharArr() { return d.Next<char[]>(); }
        public string ReadString() { return d.Next<string>(); }
        public string[] ReadStringArr() { return d.Next<string[]>(); }
        public T Read<T>() where T : ISerializable { return d.Next<T>(); }
    }


    public class WriteableBuffer : IWriteableBuffer
    {
        NetData d;
        public WriteableBuffer(NetData d)
        {
            this.d = d;
        }

        public void Write(byte[] v) { d.Push(v); }
        public void Write(byte v) { d.Push(v); }
        public void Write(bool v) { d.Push(v); }
        public void Write(bool[] v) { d.Push(v); }
        public void Write(short v) { d.Push(v); }
        public void Write(short[] v) { d.Push(v); }
        public void Write(int v) { d.Push(v); }
        public void Write(int[] v) { d.Push(v); }
        public void Write(long v) { d.Push(v); }
        public void Write(long[] v) { d.Push(v); }
        public void Write(ulong v) { d.Push(v); }
        public void Write(ulong[] v) { d.Push(v); }
        public void Write(float v) { d.Push(v); }
        public void Write(float[] v) { d.Push(v); }
        public void Write(double v) { d.Push(v); }
        public void Write(double[] v) { d.Push(v); }
        public void Write(char v) { d.Push(v); }
        public void Write(char[] v) { d.Push(v); }
        public void Write(string v) { d.Push(v); }
        public void Write(string[] v) { d.Push(v); }
        public void Write(ISerializable v) { d.Push(v); }
        public void Write(ISerializable[] v) { d.Push(v); }
    }

    public class UserDataNotifier
    {
        NetData d;
        public UserDataNotifier(NetData d)
        {
            this.d = d;
        }
        public void Notify(UserInfo info, Action<dp.dpNova_UserInfo> action)
        {
            WriteableBuffer w = new WriteableBuffer(d);
            dp.dpNova_UserInfo dp = new dp.dpNova_UserInfo(w, info, null);
            action(dp);
        }
        public void Notify(ActorHeroData heroData, Action<dp.dpNova_ActorHeroData> action)
        {
            WriteableBuffer w = new WriteableBuffer(d);
            dp.dpNova_ActorHeroData dp = new dp.dpNova_ActorHeroData(w, heroData, null);
            action(dp);
        }
    }
}
