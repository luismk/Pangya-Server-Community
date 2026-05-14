using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities;
using System;
using System.Collections.Generic;
using Pangya_AuthServer.Session;
using sas;
using Pangya_AuthServer.AuthServerTcp;
using PangyaAPI.Network.PangyaUtil;
using PangyaAPI.Utilities.Models;

namespace Pangya_AuthServer.PacketFunc
{
    public class packet_func : packet_func_base
    {
        // Cliente
        public static int packet001(object param, ParamDispatch pd)
        {

            try
            {

                @as.getInstance().requestAuthenticPlayer((Player)pd._session, pd._packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet001][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Log
                _smp.message_pool.getInstance().push(new message("[packet_func::packet001][Error] desconectando session[OID=" + Convert.ToString(pd._session.m_oid) + "], por que mandou alguns dados errado no packet de login. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                @as.getInstance().DisconnectSession((Player)pd._session);
            }

            return 0;
        }

        public static int packet002(object param, ParamDispatch pd)
        {



            try
            {

                @as.getInstance().requestDisconnectPlayer((Player)pd._session, pd._packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet002][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }

        public static int packet003(object param, ParamDispatch pd)
        {



            try
            {

                @as.getInstance().requestConfirmDisconnectPlayer((Player)pd._session, pd._packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet003][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }

        public static int packet004(object param, ParamDispatch pd)
        { 
            try
            { 
                @as.getInstance().requestInfoPlayer((Player)pd._session, pd._packet); 
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet004][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }

        public static int packet005(object param, ParamDispatch pd)
        {



            try
            {

                @as.getInstance().requestConfirmSendInfoPlayer((Player)pd._session, pd._packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet005][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }

        public static int packet006(object param, ParamDispatch pd)
        { 
            try
            {

                @as.getInstance().requestSendCommandToOtherServer((Player)pd._session, pd._packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet006][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }

        public static int packet007(object param, ParamDispatch pd)
        { 
            try
            {

                @as.getInstance().requestSendReplyToOtherServer((Player)pd._session, pd._packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet007][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }

        public static int packet0FF(object param, ParamDispatch pd)
        {
            try
            { 
                @as.getInstance().requestSendPongInfo((Player)pd._session, pd._packet); 
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet007][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }

        public static int packet_svFazNada(object param, ParamDispatch pd)
        {
            return 0;
        }

        // Server

        // Method Helper
        public static void session_send(PangyaBinaryWriter p, PangyaAPI.Network.PangyaSession.Session s, int __DEBUG = 1)
        {
            try
            {
                if (s == null)
                    throw new exception("Error session s is null, packet_func::session_send()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV, 1, 2));

                MAKE_SEND_BUFFER(p.GetBytes, s);
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[session_send(byte[])] Exception: " + e.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public static void session_send(byte[] p, PangyaAPI.Network.PangyaSession.Session s, int __DEBUG = 1)
        {
            try
            {
                if (s == null)
                    throw new exception("Error session s is null, packet_func::session_send()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV, 1, 2));

                MAKE_SEND_BUFFER(p, s);
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[session_send(byte[])] Exception: " + e.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public static void session_send(PangyaBinaryWriter p, Player s, int __DEBUG = 1)
        {
            try
            {
                if (s == null)
                    throw new exception("Error session s is null, packet_func::session_send()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV, 1, 2));

                MAKE_SEND_BUFFER(p.GetBytes, s);
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[session_send(writer)] Exception: " + e.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public static void session_send(List<PangyaBinaryWriter> v_p, Player s, int __DEBUG = 1)
        {
            try
            {
                if (s == null)
                    throw new exception("Error session s is null, packet_func::session_send()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV, 1, 2));

                for (var i = 0; i < v_p.Count; ++i)
                {
                    if (v_p[i] != null && v_p[i].GetSize > 0)
                        MAKE_SEND_BUFFER(v_p[i].GetBytes, s);
                    else
                        _smp.message_pool.getInstance().push(new message("Error byte[] p is null, packet_func::session_send()", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[session_send(writer list)] Exception: " + e.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public static void session_send(List<byte[]> v_p, Player s, int __DEBUG = 1)
        {
            try
            {
                if (s == null)
                    throw new exception("Error session s is null, packet_func::session_send()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV, 1, 2));

                for (var i = 0; i < v_p.Count; ++i)
                {
                    if (v_p[i] != null)
                        MAKE_SEND_BUFFER(v_p[i], s);
                    else
                        _smp.message_pool.getInstance().push(new message("Error byte[] p is null, packet_func::session_send()", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[session_send(byte[] list)] Exception: " + e.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public static void vector_send(PangyaBinaryWriter _v_p, List<Player> _v_s, int __DEBUG = 1)
        {
            try
            {
                if (_v_p != null)
                {
                    foreach (var el2 in _v_s)
                        MAKE_SEND_BUFFER(_v_p.GetBytes, el2);
                }
                else
                    _smp.message_pool.getInstance().push(new message("Error byte[] p is null, packet_func::vector_send(Player)", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[vector_send(Player List)] Exception: " + e.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public static void vector_send(List<PangyaBinaryWriter> _v_p, List<Player> _v_s, int __DEBUG = 1)
        {
            try
            {
                foreach (var el in _v_p)
                {
                    if (el != null)
                    {
                        foreach (var el2 in _v_s)
                            MAKE_SEND_BUFFER(el.GetBytes, el2);
                    }
                    else
                        _smp.message_pool.getInstance().push(new message("Error byte[] p is null, packet_func::vector_send(Player)", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[vector_send(Player List)] Exception: " + e.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
    }
}
