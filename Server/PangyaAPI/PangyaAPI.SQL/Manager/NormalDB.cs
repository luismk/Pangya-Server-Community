using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Threading;

namespace PangyaAPI.SQL
{
    public class NormalDB
    {

        public enum TT_DB : uint
        {
            TT_NORMAL_EXEC_QUERY,
            TT_NORMAL_RESPONSE
        }

        public class msg_t
        {
            public msg_t(int _id, Pangya_DB __pangya_db, Action<int, Pangya_DB, object> _callback_response, object _arg)
            {
                this.id = _id;
                this._pangya_db = __pangya_db;
                this.func = _callback_response;
                this.arg = _arg;
            }

            public void execFunc()
            {
                if (func == null)
                {
                    return;
                }

                try
                {
                    if (_pangya_db == null)
                    {
                        throw new System.Exception("_pangya_db is null");
                    }

                    func.Invoke(id, _pangya_db, arg);
                    sucess = true;
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[NormalDB::mgs_t::execFunc][Error] " + e.getFullMessageError(), 0));
                }
            }

            public void execQuery()
            {
                try
                {
                    if (_pangya_db == null)
                    {
                        throw new System.Exception("[NormalDB::mgs_t::execQuery][Error] _pangya_db is null");
                    }
                    sucess = true;
                    _pangya_db.exec();
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[NormalDB::mgs_t::execQuery][Error] " + e.getFullMessageError(), 0));
                    throw e;
                }
            }

            protected int id; // ID da msg
            protected Pangya_DB _pangya_db;
            protected Action<int, Pangya_DB, object> func;
            protected object arg;
            public bool sucess;
        }

        protected Thread m_pExec;
        protected Thread m_pResponse;
        protected bool m_state;
        protected uint m_continue_exec;
        protected uint m_continue_response;
        protected uint m_free_all_waiting;
    }
}
