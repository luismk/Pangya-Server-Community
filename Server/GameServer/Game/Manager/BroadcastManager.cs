using System;
using System.Collections.Generic;
using System.Linq;

namespace Pangya_GameServer.Game.Manager
{
    public class BroadcastManager
    {
        public enum RET_TYPE : byte
        {
            NO_NOTICE, // Não tem notice na lista
            OK,
            WAIT
        }

        public enum TYPE : byte
        {
            GM_NOTICE,
            CUBE_WIN_RARE,
            TICKER
        }

        public class NoticeCtx
        {

            // NoticeCtx
            public NoticeCtx(uint _ul = 0u)
            {
                clear();
            }

            public NoticeCtx(uint _time_second,
                string _notice, TYPE _type)
            {
                this.nickname = "";
                this.option = 0;

                type = _type;
                time_second = _time_second;

                notice = _notice;
            }

            public NoticeCtx(uint _time_second,
                string _notice,
                uint _option, TYPE _type)
            {
                this.nickname = "";

                type = _type;
                option = _option;
                time_second = _time_second;
                notice = _notice;
            }

            public NoticeCtx(uint _time_second,
                string _nickname,
                string _notice, TYPE _type)
            {
                this.option = 0;

                type = _type;
                time_second = _time_second;

                nickname = _nickname;
                notice = _notice;
            }

            public void clear()
            {

                type = TYPE.GM_NOTICE;
                time_second = 0;

                if (notice.Length != 0)
                {
                    notice = "";
                }
            }

            public TYPE type;
            public uint time_second = new uint();
            public uint option = new uint();
            public string nickname = "";
            public string notice = "";
        }

        public class RetNoticeCtx
        {

            // RetNotice
            public RetNoticeCtx(uint _ul = 0u)
            {
                clear();
            }

            public void clear()
            {

                ret = RET_TYPE.NO_NOTICE;

                nc.clear();
            }

            public RET_TYPE ret;
            public NoticeCtx nc = new NoticeCtx();
        }

        public BroadcastManager(uint _interval_time_second)
        {
            this.m_interval = _interval_time_second;
        }

        public void push_back(int time, string notice, TYPE type)
        {
            var t = (uint)((time < 0) ? 0 : time);
            push_back(new NoticeCtx(t, notice, type));
        }

        public void push_back(int time, string notice, uint option, TYPE type)
        {
            var t = (uint)((time < 0) ? 0 : time);
            push_back(new NoticeCtx(t, notice, option, type));
        }

        public void push_back(int time, string nickname, string notice, TYPE type)
        {
            var t = (uint)((time < 0) ? 0 : time);
            push_back(new NoticeCtx(t, nickname, notice, type));
        }

        public void push_back(NoticeCtx nc)
        {
            lock (cs_lock)
            {
                m_list.Add(nc.time_second, new List<NoticeCtx>() { nc });
            }
        }


        public RetNoticeCtx peek()
        {

            var retCtx = new RetNoticeCtx();
            uint now = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            lock (cs_lock)
            {
                if (m_list.Count == 0)
                {
                    retCtx.ret = RET_TYPE.NO_NOTICE;
                    return retCtx;
                }


                if ((now - m_last_peek) >= m_interval)
                {
                    var firstKey = m_list.Values.FirstOrDefault().FirstOrDefault();

                    if (firstKey.time_second == 0 || firstKey.time_second <= now)
                    {
                        retCtx.nc = firstKey;

                        m_list.Remove(firstKey.time_second);

                        retCtx.ret = RET_TYPE.OK;
                        m_last_peek = now;
                    }
                    else
                    {
                        retCtx.ret = RET_TYPE.WAIT; // Ainda não deu o tempo programado
                    }
                }
                else
                {
                    retCtx.ret = RET_TYPE.WAIT; // Ainda não deu o intervalo
                }
            }

            return retCtx;
        }

        public uint getSize()
        {
            var count = m_list.Count();
            return (uint)count;
        }

        protected Dictionary<uint, List<NoticeCtx>> m_list = new Dictionary<uint, List<NoticeCtx>>();
        protected uint m_last_peek = new uint();

        // Interval time to peek next Notice
        private uint m_interval = new uint();
        private readonly object cs_lock = new object();
    }
}
