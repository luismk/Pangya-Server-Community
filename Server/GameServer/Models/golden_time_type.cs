using System;
using System.Collections.Generic;
using System.Linq;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Models.golden_time_type
{
    public class stItemReward
    {
        public void clear()
        {
            _typeid = 0;
            qntd = 0;
            qntd_time = 0;
            rate = 0;
        }

        public uint _typeid;
        public uint qntd;
        public uint qntd_time;
        public uint rate;
    }

    public class stRound
    {
        public stRound(uint _ul = 0)
        {
            executed = false;
            time = new SYSTEMTIME();
            item = new stItemReward();
        }

        public stRound(SYSTEMTIME _time, bool _executed, stItemReward _item)
        {
            time = (_time);
            executed = _executed;
            item = new stItemReward
            {
                _typeid = _item._typeid,
                qntd = _item.qntd,
                qntd_time = _item.qntd_time,
                rate = _item.rate
            };
        }

        public void clear()
        {
            executed = false;
            time = new SYSTEMTIME();
            item.clear();
        }

        public SYSTEMTIME time; // hora que o round vai ser executado
        public bool executed;
        public stItemReward item;
    }

    public class stGoldenTime
    {
        public enum eTYPE : byte
        {
            ONE_DAY, // Começa em 1 dia e termina no mesmo dia
            INTERVAL, // Intervalo de dias
            FOREVER // Nunca acaba
        }

        public stGoldenTime(uint _ul = 0)
        {
            clear();
        }

        public stGoldenTime(uint _id, eTYPE _type, SYSTEMTIME _start_date, SYSTEMTIME _end_date, uint _rate_of_players = 1)
        {
            id = _id;
            type = _type;
            date = new SYSTEMTIME[2] { (_start_date), (_end_date) };
            rate_of_players = _rate_of_players;
            current_round = null;
            rounds = new List<stRound>();
            item_rewards = new List<stItemReward>();
            is_end = false;
        }

        public void clear()
        {
            id = 0;
            type = eTYPE.ONE_DAY;
            date = new SYSTEMTIME[2];
            date[0]= new SYSTEMTIME(DateTime.Now);
            date[1] = new SYSTEMTIME(DateTime.Now);
            is_end = false;
            rate_of_players = 1;
            current_round = null;
            rounds = new List<stRound>();
            item_rewards = new List<stItemReward>(); 
        }

        public stRound updateRound()
        {
            stRound ret = null;

            if (rounds.Count == 0)
                return null;

            var now = DateTime.Now;

            var it = rounds
                .FirstOrDefault(_el =>
                    !_el.executed &&
                    !_el.time.IsEmpty &&
                    now >= _el.time.ConvertTime().AddMinutes(-5) &&  // já passou do "5 minutos antes"
                    now < _el.time.ConvertTime()                     // mas ainda não bateu o horário exato
                );

            if (it != null)
                current_round = ret = it;
            else
                current_round = null;

            return ret;
        }



        public uint id;
        public eTYPE type;
        public SYSTEMTIME[] date; // Começo e Fim
        public List<stRound> rounds;
        public List<stItemReward> item_rewards;
        public bool is_end;
        public uint rate_of_players; // Rate de quantos players a cada NUMBER_OF_PLAYER players, padrão 1
        public stRound current_round;
    }

    public class stPlayerReward
    {
        public uint uid;
        public bool is_premium;
        public bool is_playing; // se não estiver jogando, ele está na sala lounge

        public stPlayerReward()
        { }
        public stPlayerReward(uint _uid, bool _is_premium, bool _is_playing)
        {
            uid = _uid;
            is_premium = _is_premium;
            is_playing = _is_playing;
        }
    }

    public class stGoldenTimeReward
    {
        public void clear()
        {
            round.clear();
            players.Clear();
        }

        public stRound round = new stRound();
        public List<stPlayerReward> players = new List<stPlayerReward>();
    }
}
