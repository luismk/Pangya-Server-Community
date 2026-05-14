using System;
using System.Diagnostics;
using PangyaAPI.SQL.Manager;
using PangyaAPI.Utilities;
namespace PangyaAPI.SQL.Manager
{
    public class NormalManager
    {
        public NormalManager()
        {
        }

        public int add(NormalDB.msg_t _msg)
        {
            _msg.execQuery();
            _msg.execFunc();
            return 0;
        }
        public int add(int _id,
            ref Pangya_DB _pangya_db,
        Action<int, Pangya_DB, object> _callback_response,
            object _arg)
        {

            add(new NormalDB.msg_t(_id, _pangya_db, _callback_response, _arg));

            return 0;
        }

        public void add(int _id,
     Pangya_DB _pangya_db,
     Action<int, Pangya_DB, object> _callback_response,
     object _arg)
        {
            add(_id, ref _pangya_db, _callback_response, _arg);
        }

        public bool Connected()
        {
            try
            {
                return new DBCheckConnection().Connected();
            }
            catch (exception e)
            { 
                throw e;
            }
        }

    }
}

namespace snmdb
{
    public class NormalManagerDB : Singleton<NormalManager> { }
}