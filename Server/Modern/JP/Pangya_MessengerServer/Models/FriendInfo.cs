using Pangya_MessengerServer.Flags;
using PangyaAPI.Network;
using System.Runtime.InteropServices;
namespace Pangya_MessengerServer.Models
{
    public class FriendInfo
    {
        public FriendInfo(uint _ul = 0u)
        {
            clear();
        }

        public virtual void clear()
        {
            nickname = "";
            apelido = "";
            lUnknown = -1;
            lUnknown2 = 0;
            lUnknown3 = -1;
            lUnknown4 = 0;
            lUnknown5 = 0;
            lUnknown6 = 0;
            lUnknown7 = 0;
        }
        public string nickname = "";
        public string apelido = "";
        public uint uid;
        public int lUnknown;
        public int lUnknown2;
        public int lUnknown3;
        public int lUnknown4;
        public int lUnknown5;
        public int lUnknown6;
        public int lUnknown7; // Esse aqui s� tem no JP, esse valor a+, peguei ele sempre zero, das vezes que vi no pacote        

        public byte[] ToArray()
        {
            using (var p = new Packet())
            {
                p.WriteString(nickname, 22);
                p.WriteString(apelido, 11);
                p.WriteUInt32(uid);
                p.WriteInt32(lUnknown);
                p.WriteInt32(lUnknown2);
                p.WriteInt32(lUnknown3);
                p.WriteInt32(lUnknown4);
                p.WriteInt32(lUnknown5);
                p.WriteInt32(lUnknown6);
                p.WriteInt32(lUnknown7);
                return p.GetBytes;
            }
        }
    }
    //sera necessario ter duas classes?
    public class FriendInfoEx : FriendInfo
    {
        public FriendInfoEx(uint _ul = 0u) : base()
        {
            clear();
        }

        public override void clear()
        {

            base.clear();

            cUnknown_flag = 255;
            level = 0;
            flag = new uFlag(); // ServerFlag se o player � amigo ou � membro Guild
            state = new uState(); // Sex, online, friend, request, block e etc
            flag.clear();
            state.clear();
        }

        public class uState
        {
            public byte ucState;

            public void clear() => ucState = 0;

            public UserState State
            {
                get => (UserState)ucState;
                set => ucState = (byte)value;
            }

            public byte sex
            {
                get => (byte)(State.HasFlag(UserState.sex) ? 1 : 0);
                set => State = value != 0 ? State | UserState.sex : State & ~UserState.sex;
            }

            public byte online
            {
                get => (byte)(State.HasFlag(UserState.online) ? 1 : 0);
                set => State = value != 0 ? State | UserState.online : State & ~UserState.online;
            }

            public byte _friend
            {
                get => (byte)(State.HasFlag(UserState._friend) ? 1 : 0);
                set => State = value != 0 ? State | UserState._friend : State & ~UserState._friend;
            }

            public byte request_friend
            {
                get => (byte)(State.HasFlag(UserState.request_friend) ? 1 : 0);
                set => State = value != 0 ? State | UserState.request_friend : State & ~UserState.request_friend;
            }

            public byte block
            {
                get => (byte)(State.HasFlag(UserState.block) ? 1 : 0);
                set => State = value != 0 ? State | UserState.block : State & ~UserState.block;
            }

            public byte play
            {
                get => (byte)(State.HasFlag(UserState.play) ? 1 : 0);
                set => State = value != 0 ? State | UserState.play : State & ~UserState.play;
            }

            public byte AFK
            {
                get => (byte)(State.HasFlag(UserState.AFK) ? 1 : 0);
                set => State = value != 0 ? State | UserState.AFK : State & ~UserState.AFK;
            }

            public byte busy
            {
                get => (byte)(State.HasFlag(UserState.busy) ? 1 : 0);
                set => State = value != 0 ? State | UserState.busy : State & ~UserState.busy;
            }
        }

        public class uFlag
        {
            public byte ucFlag;

            public void clear() => ucFlag = 0;

            public byte _friend
            {
                get => (byte)((ucFlag & 0b0000_0001) != 0 ? 1 : 0);
                set => ucFlag = (byte)(value != 0 ? ucFlag | 0b0000_0001 : ucFlag & ~0b0000_0001);
            }

            public byte guild_member
            {
                get => (byte)((ucFlag & 0b0000_0010) != 0 ? 1 : 0);
                set => ucFlag = (byte)(value != 0 ? ucFlag | 0b0000_0010 : ucFlag & ~0b0000_0010);
            }
        }
        public byte cUnknown_flag;
        public uFlag flag = new uFlag(); // ServerFlag se o player � amigo ou � membro Guild
        public uState state = new uState(); // Sex, online, friend, request, block e etc
        public byte level;
    }
}
