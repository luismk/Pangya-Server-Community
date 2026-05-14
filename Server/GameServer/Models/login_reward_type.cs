using System;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Models
{
    public class stPlayerState
    {
        public stPlayerState(uint _ul = 0u)
        {
            clear();
        }
        public stPlayerState(ulong _id,
            uint _uid,
            uint _count_days,
            uint _count_seq,
            SYSTEMTIME _upt_date,
            bool _is_clear = false)
        {
            this.id = _id;
            this.uid = _uid;
            this.count_days = _count_days;
            this.update_date = _upt_date;
            this.count_seq = _count_seq;
            this.is_clear = _is_clear;
        }

        public void clear()
        {

        }

        public string toString()
        {
            return "ID=" + Convert.ToString(id) + ", UID=" + Convert.ToString(uid) + ", COUNT_DAYS=" + Convert.ToString(count_days) + ", COUNT_SEQ=" + Convert.ToString(count_seq) + ", IS_CLEAR=" + (is_clear ? "TRUE" : "FALSE") + ", UPDATE_DATE=" + update_date.ConvertTime();
        }

        public ulong id = new ulong(); // index
        public uint uid = new uint();
        public uint count_days = new uint(); // Quantos dias j� logou no evento
        public uint count_seq = new uint(); // Quantas vezes j� repediu esse evento
        public SYSTEMTIME update_date = new SYSTEMTIME();

        public bool is_clear;
    }

    public class stLoginReward : System.IDisposable
    {
        public class stItemReward
        {
            public stItemReward(uint _ul = 0u)
            {
                clear();
            }
            public stItemReward(uint __typeid,
                uint _qntd,
                uint _qntd_time)
            {
                this._typeid = __typeid;
                this.qntd = _qntd;
                this.qntd_time = _qntd_time;
            }
            public void clear()
            {

            }

            public string toString()
            {
                return "TYPEID=" + Convert.ToString(_typeid) + ", QNTD=" + Convert.ToString(qntd) + ", QNTD_TIME=" + Convert.ToString(qntd_time);
            }

            public uint _typeid = new uint();
            public uint qntd = new uint();
            public uint qntd_time = new uint();
        }

        public enum eTYPE : byte
        {
            N_TIME, // Executa N Times, 1, 2, 3, 4, 5, 6, 7 e etc
            FOREVER // Enquanto ele estiver ativo ele executa sempre
        }

        public stLoginReward(uint _ul = 0u)
        {
            this.id = 0;
            this.type = eTYPE.N_TIME;
            this.name = "";
            this.is_end = false;
            this.end_date = new SYSTEMTIME();
            this.days_to_gift = 1u;
            this.n_times_gift = 1u;
            this.item_reward = new stItemReward(0u);
        }
        public stLoginReward(ulong _id,
            eTYPE _type, string _name,
            uint _days_to_gift,
            uint _n_times_gift,
            stItemReward _item,
            SYSTEMTIME _end_date,
            bool _is_end = false)
        {
            this.id = _id;
            this.type = (_type);
            this.end_date = _end_date;
            this.days_to_gift = _days_to_gift;
            this.n_times_gift = _n_times_gift;
            this.item_reward = (_item);
            this.is_end = _is_end;

            setName(_name);
        }
        public virtual void Dispose()
        {
            clear();
        }
        public void clear()
        {

            id = 0;
            is_end = false;
            type = eTYPE.N_TIME;
            name = "";
            days_to_gift = 1u;
            n_times_gift = 1u;

            end_date = new SYSTEMTIME();

            item_reward.clear();
        }

        public void setName(string _name)
        {

            if (_name != null)
            {
                name = _name;
            }
        }

        public string getName()
        {

            if (name == null)
            {
                return "";
            }

            return name;
        }

        public string toString()
        {
            return "ID=" + Convert.ToString(id) + ", TYPE=" + Convert.ToString((ushort)type) + ", NAME=" + getName() + ", DAYS_TO_GIFT=" + Convert.ToString(days_to_gift) + ", N_TIMES_GIFT=" + Convert.ToString(n_times_gift) + ", IS_END=" + (is_end ? "TRUE" : "FALSE") + ", ITEM{" + item_reward.toString() + "}, END_DATE=" + (end_date.IsEmpty? "": end_date.ConvertTime().ToString());
        }

        public ulong id = new ulong();
        public eTYPE type;
        public bool is_end;

        public uint days_to_gift = new uint();
        public uint n_times_gift = new uint();
        public SYSTEMTIME end_date = new SYSTEMTIME();

        public stItemReward item_reward = new stItemReward();

        private string name = "";
    }
}