using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pangya_GameServer.Engine
{
    public class TitleMapCallBack
    { 
        private Func<object, bool> _callback;
        private object _arg;
         
        public TitleMapCallBack()
        {
            Clear();
        }

        // Construtor com parâmetros
        public TitleMapCallBack(Func<object, bool> callback, object arg)
        {
            _callback = callback;
            _arg = arg;
        }

        public void Clear()
        {
            _callback = null;
            _arg = null;
        }

        public bool Exec()
        {
            if (_callback != null)
            {
                return _callback(_arg);
            }
            else
            {
                // Mantendo o seu sistema de log padrão
                _smp.LogManager.Instance.push(new AppMessage(
                    "[PlayerInfo::TitleMapCallback::Exec][Error] callback is null.",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return false;
        }
    }
}
