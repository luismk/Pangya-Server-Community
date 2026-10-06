using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

namespace PangyaAPI.DataBase.Models
{
    public class NormalDB
    {
        public class msg_t
        {
            public msg_t(int _id, Pangya_DB __pangya_db, Action<int, Pangya_DB, object> _callback_response, object _arg)
            {
                this.id = _id;
                this._pangya_db = __pangya_db;
                this.func = _callback_response;
                this.arg = _arg;
                sucess = false;//inicia como false
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
                    if (!sucess)
                    {
                        if (func != null)
                        {
                            _pangya_db.exec();//executa aqui e devolve
                            func.Invoke(id, _pangya_db, arg);
                        }
                        else
                            _smp.LogManager.Instance.push(new AppMessage("[NormalDB::mgs_t::execFunc][Log] func is null", type_msg.CL_ONLY_CONSOLE));

                    }
                    sucess = true; 
                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[NormalDB::mgs_t::execFunc][Error] " + e.getFullMessageError(), 0));
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

                    if (!sucess)
                        _pangya_db.exec();
                    else
                        _smp.LogManager.Instance.push(new AppMessage("[NormalDB::mgs_t::execQuery][Log] bug", type_msg.CL_ONLY_CONSOLE));

                    sucess = true;
                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[NormalDB::mgs_t::execQuery][Error] " + e.getFullMessageError(), 0));
                    throw;
                }
            }

            public bool IsFunc()
            {
               return func != null; 
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