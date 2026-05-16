using Pangya_AuthServer.Models;
using PangyaAPI.Network.Cryptor;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using System.Collections.Generic;

namespace Pangya_AuthServer.Session
{
    public class Player : PangyaAPI.Network.PangyaSession.Session
    {
        public PlayerInfo m_pi { get; set; }

        public Player()
        {
            m_pi = new PlayerInfo();
        }


        public override string getNickname()
        {
            return m_pi.nickname;
        }

        public override uint getUID()
        {
            return m_pi.uid;
        }

        public override string getID()
        {
            return m_pi.id;
        }

        public override uint getCapability() { return (uint)m_pi.tipo; }

        public override bool clear()
        {
            bool ret;
            if ((ret = base.clear()))
            {

                // Player Info
                m_pi.clear();

            }
            return ret;
        }

        public override byte getStateLogged()
        {
            return 1;
        }

        public override void requestSendBuffer(byte[] _buff, bool _raw = false)
        {

            if (_buff == null)
            {
                throw new exception("Error _buff is nullptr. Session::requestSendBuffer()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.SESSION,
                    3, 0));
            }
            int _size = _buff.Length;
            if (_size <= 0)
            {
                throw new exception("Error _size is less or equal the zero. Session::requestSendBuffer()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.SESSION,
                    4, 0));
            }
            try
            {
                if (isConnectedToSend())
                {

                    var payloadData = _raw ? _buff : Cipher.EncryptClient(_buff, m_key, 0);

                    if (!m_client.Send(payloadData, payloadData.Length))
                    {
                        @lock();
                        setConnectedToSend(false);
                        unlock();

                        try
                        {
                            _Packet_Handle_Base.DisconnectSession(this);
                        }
                        catch (exception e)
                        {
                            _smp.message_pool.getInstance().push(new message("[threadpool::send_new][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }
                    else
                    {
                        //new mode
                        _Packet_Handle_Base.dispach_packet_sv_same_thread(this, _raw ? new packet(_buff, true) : new packet(_buff));
                    }
                }
                else
                {
                    //m_buff_s.releaseWrite();
                    return;
                }
            }
            finally
            {
                // m_buff_s.unlock();
            }
        }


    }
}
