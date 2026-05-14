using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities;
using System.Collections.Generic;
using PangyaAPI.Utilities.Models; 
namespace Pangya_RankingServer.PacketFunc
{
    public class packet_func : packet_func_base
    {
        // Cliente 
        public static int packet000(object param, ParamDispatch pd)
        { 
            try
            { 
                srs.rs.getInstance().requestLogin((Session.Player)pd._session, pd._packet); 
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet000][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.RANK_SERVER)
                {
                    throw;
                }
            }

            return 0;
        }

        public static int packet001(object param, ParamDispatch pd)
        {
  
            try
            { 
                srs.rs.getInstance().requestPlayerInfo((Session.Player)pd._session, pd._packet); 
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet001][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.RANK_SERVER)
                {
                    throw;
                }
            }

            return 0;
        }

        public static int packet002(object param, ParamDispatch pd)
        { 
            try
            { 
                srs.rs.getInstance().requestSearchPlayerInRank((Session.Player)pd._session, pd._packet); 
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet002][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.RANK_SERVER)
                {
                    throw;
                }
            }

            return 0;
        }

        public static int packet003(object param, ParamDispatch pd)
        { 
            try
            {

#if DEBUG
                _smp.message_pool.getInstance().push(new message("[packet_func::packet003][Log] Packet Hex: " + pd._packet.Log(), type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif // _DEBUG

                // Rank Server
                //rs->

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet003][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.RANK_SERVER)
                {
                    throw;
                }
            }

            return 0;
        }

        public static int packet004(object param, ParamDispatch pd)
        {



            try
            {

#if DEBUG
                _smp.message_pool.getInstance().push(new message("[packet_func::packet004][Log] Packet Hex: " + pd._packet.Log(), type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif // _DEBUG

                // Rank Server
                //rs->

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet004][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.RANK_SERVER)
                {
                    throw;
                }
            }

            return 0;
        }

        public static int packet005(object param, ParamDispatch pd)
        {



            try
            {

#if DEBUG
                _smp.message_pool.getInstance().push(new message("[packet_func::packet005][Log] Packet Hex: " + pd._packet.Log(), type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif // _DEBUG

                // Rank Server
                //rs->

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet005][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.RANK_SERVER)
                {
                    throw;
                }
            }

            return 0;
        }

        // Server

        // Server
        public static int packet_svFazNada(object param, ParamDispatch pd)
        {



            // Esse pacote � para os pacotes que o server envia para o cliente
            // e n�o precisa de tratamento depois que foi enviado para o cliente

            return 0;
        }

        // Auth Server
        // Auth Server

        // Auth Server
        public static int packet_as001(object param, ParamDispatch pd)
        {
            //rank_server rs = (rank_server)((_arg1));
            //_MAKE_BEGIN_PACKET_AUTH_SERVER(_arg2);

            try
            {

                // Exemplo se precisar
                //srs::rs::getInstance().DisconnectSession;

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet_as001][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.RANK_SERVER)
                {
                    throw;
                }
            }

            return 0;
        }

        // Broadcast

        // Session

        // Session
        public static void session_send(PangyaBinaryWriter _p,
            PangyaAPI.Network.PangyaSession.Session _s, byte _debug)
        {

            if (_s == null)
            {
                throw new exception("[packet_func::session_send][Error] session *_s is nullptr.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_RS,
                    1, 2));
            } 
            try
            {
                MAKE_SEND_BUFFER(_p.GetBytes, _s);
            }
            catch (exception e)
            {
                if (!ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(),
                    STDA_ERROR_TYPE.SESSION, 6))
                {
                    if ((_s).devolve())
                    {
                        srs.rs.getInstance().DisconnectSession((_s));
                    }
                }
                if (ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(),
                    STDA_ERROR_TYPE.SESSION, 2))
                {
                    throw;
                }
            };
        }

        public static void session_send(List<PangyaBinaryWriter> _v_p,
            PangyaAPI.Network.PangyaSession.Session _s, byte _debug)
        {

            if (_s == null)
            {
                throw new exception("[packet_func::session_send][Error] session *_p is nullptr.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_RS,
                    1, 2));
            }

            foreach (var el in _v_p)
            {
                if (el != null)
                { 
                    try
                    {
                        MAKE_SEND_BUFFER(el.GetBytes, _s);
                    }
                    catch (exception e)
                    {
                        if (!ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(),
                            STDA_ERROR_TYPE.SESSION, 6))
                        {
                            if ((_s).devolve())
                            {
                                srs.rs.getInstance().DisconnectSession((_s));
                            }
                        }
                        if (ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(),
                            STDA_ERROR_TYPE.SESSION, 2))
                        {
                            throw;
                        }
                    }; 
                }
                else
                {
                    _smp.message_pool.getInstance().push(new message("[packet_func::session_send][Error][WARNING] packet *p is nullptr.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
        }
    }
}
