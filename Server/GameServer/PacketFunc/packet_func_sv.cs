using Pangya_GameServer.Repository;
using Pangya_GameServer.Game;
using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Game.System;
using Pangya_GameServer.Models;
using Pangya_GameServer.PangyaEnums;
using PangyaAPI.Network.Repository;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Network.PangyaSession;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using static Pangya_GameServer.Models.DefineConstants;
using sgs;
namespace Pangya_GameServer.PacketFunc
{
    /// <summary>
    /// somente as requisicoes feitas pelo cliente
    /// </summary>
    public class packet_func : packet_func_base
    {
        public static int packet_svFazNada(object param, ParamDispatch pd)
        {
            var str_tmp = "Time: " + ((Environment.TickCount - pd._session.m_time_start) / (double)1000);

            pd._session.m_time_start = Environment.TickCount;
            return 0;
        }

        public static int packet_sv4D(object param, ParamDispatch pd)
        {
            var str_tmp = "Time: " + ((Environment.TickCount - pd._session.m_time_start) / (double)1000);

            pd._session.m_time_start = Environment.TickCount;

            _smp.message_pool.getInstance().push(new message(str_tmp, type_msg.CL_ONLY_FILE_TIME_LOG));

            return 0;
        }

        public static int packet_svRequestInfo(object param, ParamDispatch pd)
        {
            return 0;
        }

        public static int packet_as001(object param, ParamDispatch pd)
        {
            return 0;
        }


        public static int packet002(object param, ParamDispatch pd)
        {
            try
            {
                var player = getPlayer(pd._session);

                sgs.gs.getInstance().requestLogin(player, pd._packet);
            }
            catch (exception ex)
            {
                _smp.message_pool.getInstance().push(new message("[packet_func::packet002][ErrorSystem] " + ex.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return 0;
        }

        public static int packet003(object param, ParamDispatch pd)
        {
            try
            {
                var player = getPlayer(pd._session);


                sgs.gs.getInstance().requestChat(player, pd._packet);
            }
            catch (exception ex)
            {
                _smp.message_pool.getInstance().push(new message("[packet_func::packet003][ErrorSystem] " + ex.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return 0;
        }

        public static int packet004(object param, ParamDispatch pd)
        {
            try
            {
                var player = getPlayer(pd._session);


                // Enter Channel, channel ID
                sgs.gs.getInstance().requestEnterChannel(player, pd._packet);
            }
            catch (exception ex)
            {
                _smp.message_pool.getInstance().push(new message("[packet_func::packet004][ErrorSystem] " + ex.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return 0;
        }

        public static int packet006(object param, ParamDispatch pd)
        {
            try
            {
                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestFinishGame(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet006][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet007(object param, ParamDispatch pd)
        {
            try
            {
                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestCheckNick(player, pd._packet);
                }
                return 0;
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message(
                    $"[packet_func::packet007][ErrorSystem] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return -1;
        }


        public static int packet008(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestMakeRoom(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet008][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL) // Por Hora relança qualquer exception que não seja do channel
                {
                    throw;
                }
            }
            return 0;
        }

        public static int packet009(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestEnterRoom(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet009][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL) // Por Hora relança qualquer exception que não seja do channel
                {
                    throw;
                }
                return 0;
            }
            return 0;
        }

        public static int packet00A(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestChangeInfoRoom(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet00A][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL) // Por Hora relança qualquer exception que não seja do channel
                {
                    throw;
                }
                return 0;
            }
            return 0;
        }

        public static int packet00B(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestChangePlayerItemChannel(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet00B][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL) // Por Hora relança qualquer exception que não seja do channel
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet00C(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                // Bloquear para ver se funciona o sync do entra depois no camp,
                // mesmo que o outro(0x9D) chama primeiro esse(0x0C) é mais rápido para verificar se o player está em uma sala
                //
                pd._session.lockSync();

                if (c != null)
                {
                    c.requestChangePlayerItemRoom(player, pd._packet);
                }

                // Bloquear para ver se funciona o sync do entra depois no camp,
                // mesmo que o outro(0x9D) chama primeiro esse(0x0C) é mais rápido para verificar se o player está em uma sala
                //
                pd._session.unlockSync();
            }
            catch (exception e)
            {
                // Bloquear para ver se funciona o sync do entra depois no camp,
                // mesmo que o outro(0x9D) chama primeiro esse(0x0C) é mais rápido para verificar se o player está em uma sala
                //  
                _smp.message_pool.getInstance().push(new message("[packet_func::packet00C][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL) // Por Hora relança qualquer exception que não seja do channel
                {
                    throw;
                }

            }
            return 0;
        }

        public static int packet00D(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);
                Channel c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestChangePlayerStateReadyRoom(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet00D][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL) // Por Hora relança qualquer exception que não seja do channel
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet00E(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestStartGame(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet00E][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet00F(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);
                Channel c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestExitRoom(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet00F][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL) // Por Hora relança qualquer exception que não seja do channel
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet010(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);
                Channel c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestChangePlayerTeamRoom(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet010][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL) // Por Hora relança qualquer exception que não seja do channel
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet011(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestFinishLoadHole(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet011][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet012(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestInitShot(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet012][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet013(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestChangeMira(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet013][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet014(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestChangeStateBarSpace(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet014][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet015(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestActivePowerShot(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet015][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;
        }

        public static int packet016(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestChangeClub(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet016][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet017(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestUseActiveItem(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet017][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet018(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestChangeStateTypeing(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet018][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet019(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestMoveBall(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet019][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet01A(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestInitHole(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet01A][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet01B(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestSyncShot(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet01B][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet01C(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestFinishShot(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet01C][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet01D(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);
                Channel c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestBuyItemShop(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet01D][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL) // Por Hora relança qualquer exception que não seja do channel
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet01F(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);
                Channel c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestGiftItemShop(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet01F][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL) // Por Hora relança qualquer excpetion que não seja do channel
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet020(object param, ParamDispatch pd)
        {
            try
            {
                var player = (Player)(pd._session);
                Channel c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestChangePlayerItemMyRoom(player, pd._packet);
                }
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[packet_func::packet020][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL) // Por Hora relança qualquer exception que não seja do channel
                {
                    throw;
                }
                return 0;
            }
            return 0;
        }

        public static int packet022(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestStartTurnTime(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet022][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet026(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);
                Channel c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestKickPlayerOfRoom(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet026][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return 0;

            }
            return 0;

        }

        public static int packet029(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestCheckInvite(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet029][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet02A(object param, ParamDispatch pd)
        {



            try
            {
                var player = getPlayer(pd._session);

                sgs.gs.getInstance().requestPrivateMessage(player, pd._packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet02A][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.GAME_SERVER)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet02D(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);
                Channel c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestShowInfoRoom(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet02D][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return 0;

            }

            return 0;
        }

        public static int packet02F(object param, ParamDispatch pd)
        {
            try
            {
                var player = getPlayer(pd._session);

                sgs.gs.getInstance().requestPlayerInfo(player, pd._packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet02F][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.GAME_SERVER)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet030(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestUnOrPauseGame(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet030][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet031(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestFinishHoleData(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet031][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet032(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);
                Channel c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestChangePlayerStateAFKRoom(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet032][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet033(object param, ParamDispatch pd)
        {



            try
            {
                var player = getPlayer(pd._session);


                sgs.gs.getInstance().requestExceptionClientMessage(player, pd._packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet033][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.GAME_SERVER)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet034(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestFinishCharIntro(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet034][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet035(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestTeamFinishHole(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet035][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet036(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestReplyContinueVersus(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet036][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet037(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestLastPlayerFinishVersus(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet037][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet039(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestPayCaddieHolyDay(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet039][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet03A(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestPlayerReportChatGame(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet03A][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        // 2018-03-04 19:26:39.633	Tipo: 60(0x3C), desconhecido ou nao implementado. func_arr::getPacketCall()	 Error Code: 335609856
        // 2018-03-04 19:26:39.633	size packet: 4
        //
        //0000 3C 00 1F 01 -- -- -- -- -- -- -- -- -- -- -- -- 	<...............
        //static int packet03C(void* _arg1, void* _arg2);	// manda msg OFF na opção 0x6F e a opção 0x11F pede a lista de amigos para enviar presente

        public static int packet03C(object param, ParamDispatch pd)
        {



            try
            {
                var player = getPlayer(pd._session);


                sgs.gs.getInstance().requestTranslateSubPacket(player, pd._packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet03C][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.GAME_SERVER)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet03D(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestCookie(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet03D][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet03E(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestEnterSpyRoom(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet03E][ErrorSystem] " + e.getCodeError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet041(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestExecCCGIdentity(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet041][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet042(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestInitShotArrowSeq(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet042][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet043(object param, ParamDispatch pd)
        {



            try
            {
                var player = getPlayer(pd._session);
                sgs.gs.getInstance().sendServerListAndChannelListToSession(player);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet043][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.GAME_SERVER)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet047(object param, ParamDispatch pd)
        {
            try
            {
                var player = getPlayer(pd._session);
                sgs.gs.getInstance().sendRankServer(player);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet047][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.GAME_SERVER)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet048(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestLoadGamePercent(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet048][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet04A(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestActiveReplay(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet04A][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet04B(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestClubSetStatsUpdate(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet04B][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet04F(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestChangeStateChatBlock(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet04F][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet054(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestChatTeam(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet054][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet055(object param, ParamDispatch pd)
        {



            try
            {
                var player = getPlayer(pd._session);


                sgs.gs.getInstance().requestChangeWhisperState(player, pd._packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet055][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.GAME_SERVER)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet057(object param, ParamDispatch pd)
        {



            try
            {
                var player = getPlayer(pd._session);


                sgs.gs.getInstance().requestCommandNoticeGM(player, pd._packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet057][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.GAME_SERVER)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet05C(object param, ParamDispatch pd)
        {



            try
            {
                var player = getPlayer(pd._session);
                sgs.gs.getInstance().sendDateTimeToSession(player);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet05C][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.GAME_SERVER)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        // 2018 - 12 - 01 18:49 : 14.928 size packet : 4
        // Destroy Room, 2 Bytes Room Number
        // 0000 60 00 01 00 -- -- -- -- -- -- -- -- -- -- -- --    `...............
        public static int packet060(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestExecCCGDestroy(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet060][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        // 2018 - 12 - 01 18:48 : 02.634 size packet : 6
        // Disconnect User, 2 Bytes Online ID
        // 0000 61 00 00 00 00 00 -- -- -- -- -- -- -- -- -- --a...............
        public static int packet061(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    _smp.message_pool.getInstance().push(new message("[packet_func::packet061][Log] PLAYER[UID=" + Convert.ToString(player.m_pi.uid) + "] tentou desconectar um player, mas o server ja faz o tratamento do packet08F do comando GM.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                // Verifica se session está varrizada para executar esse ação,
                // se ele não fez o login com o Server ele não pode fazer nada até que ele faça o login
                if (player == null)
                {
                    //throw new exception("[packet_func::" + "packet061" + "][Error] PLAYER[UID=" + Convert.ToString(player.m_pi.m_uid) + "] Nao esta autorizado a fazer esse request por que ele ainda nao fez o login com o Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV,
                    //    1, 0x7000501));
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet061][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet063(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);
                Channel c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestPlayerLocationRoom(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet063][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));


            }
            return 0;
        }

        public static int packet064(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestDeleteActiveItem(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet064][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet065(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestActiveBooster(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet065][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet066(object param, ParamDispatch pd)
        {
            try
            {
                var player = getPlayer(pd._session);


                sgs.gs.getInstance().requestSendTicker(player, pd._packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet066][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.GAME_SERVER)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet067(object param, ParamDispatch pd)
        {



            try
            {
                var player = getPlayer(pd._session);


                sgs.gs.getInstance().requestQueueTicker(player, pd._packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet067][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.GAME_SERVER)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet069(object param, ParamDispatch pd)
        {



            try
            {
                var player = getPlayer(pd._session);


                sgs.gs.getInstance().requestChangeChatMacroUser(player, pd._packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet069][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.GAME_SERVER)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet06B(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestSetNoticeBeginCaddieHolyDay(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet06B][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet073(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestChangeMascotMessage(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet73][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet074(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestCancelEditSaleShop(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet074][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet075(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestCloseSaleShop(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet075][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet076(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestOpenEditSaleShop(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet076][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet077(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestViewSaleShop(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet077][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet078(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestCloseViewSaleShop(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet078][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet079(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestChangeNameSaleShop(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet079][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet07A(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestVisitCountSaleShop(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet07A][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet07B(object param, ParamDispatch pd)
        {



            try
            {
                var player = getPlayer(pd._session);
                var r = sgs.gs.getInstance().findChannel(player.m_channel);

                if (r != null)
                {
                    r.requestPangSaleShop(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet07B][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet07C(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestOpenSaleShop(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet07C][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet07D(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestBuyItemSaleShop(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet07D][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet081(object param, ParamDispatch pd)
        {



            try
            {
                var player = getPlayer(pd._session);
                Channel c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestEnterLobby(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet081][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet082(object param, ParamDispatch pd)
        {
            try
            {
                var player = getPlayer(pd._session);
                Channel c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestExitLobby(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet082][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

            }

            return 0;
        }

        public static int packet083(object param, ParamDispatch pd)
        {



            try
            {
                var player = getPlayer(pd._session);

                sgs.gs.getInstance().requestEnterOtherChannelAndLobby(player, pd._packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet083][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.GAME_SERVER)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet088(object param, ParamDispatch pd)
        {



            try
            {
                var player = getPlayer(pd._session);

                sgs.gs.getInstance().requestCheckGameGuardAuthAnswer(player, pd._packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet088][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.GAME_SERVER)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet08B(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                CmdServerList cmd_sl = new CmdServerList(TYPE_SERVER.MSN); // waitable

                snmdb.NormalManagerDB.getInstance().add(0, cmd_sl, null, null);

                if (cmd_sl.getException().getCodeError() != 0)
                    throw cmd_sl.getException();

                var v_si = cmd_sl.getServerList();

                session_send(pacote0FC(v_si), pd._session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet08B][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return 0;
        }

        public static int packet08F(object param, ParamDispatch pd)
        {
            try
            {
                var player = getPlayer(pd._session);

                sgs.gs.getInstance().requestCommonCmdGM(player, pd._packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet08F][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.GAME_SERVER)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet098(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestOpenPapelShop(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet098][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet09A(object param, ParamDispatch pd)
        {

            try
            {
                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestUpdatePCBangMascot(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet09C][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return 0;
        }


        public static int packet09C(object param, ParamDispatch pd)
        {



            try
            {
                var player = getPlayer(pd._session);

                //// Last 5 Player Game Info   
                session_send(pacote10E(player.m_pi.l5pg), pd._session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet09C][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return 0;
        }

        public static int packet09D(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestEnterGameAfterStarted(player, pd._packet);
                }
            }
            catch (exception e)
            {

                // Bloquear para ver se funciona o sync do entra depois no camp,
                // mesmo que o outro(0x9D) chama primeiro esse(0x0C) é mais rápido para verificar se o player está em uma sala


                _smp.message_pool.getInstance().push(new message("[packet_func::packet09D][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet09E(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestUpdateGachaCoupon(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet09E][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0A1(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestEnterWebLinkState(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0A1][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0A2(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestExitedFromWebGuild(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0A2][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0AA(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestUseTicketReport(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0AA][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0AB(object param, ParamDispatch pd)
        {

            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestOpenTicketReportScroll(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0AB][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0AE(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestMakeTutorial(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0AE][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0B2(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestOpenBoxMyRoom(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0B2][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0B4(object param, ParamDispatch pd)
        {



            try
            {
                var player = getPlayer(pd._session);

                byte option = pd._packet.ReadByte();
                ushort numero_sala = pd._packet.ReadUInt16();

                // Log
                _smp.message_pool.getInstance().push(new message("[packet_func::packet0B4][Log][Option=" + Convert.ToString((ushort)option) + "] PLAYER[UID=" + Convert.ToString(player.m_pi.uid) + "] foi convidado por um player aceitou o pedido saiu da sala[NUMERO=" + Convert.ToString(numero_sala) + "] e relogou.", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0B4][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0B5(object param, ParamDispatch pd)
        {



            try
            {
                var player = getPlayer(pd._session);

                uint from_uid = new uint();
                uint to_uid = new uint();

                from_uid = pd._packet.ReadUInt32();
                to_uid = pd._packet.ReadUInt32();

                var p = new PangyaBinaryWriter();
                p.init_plain(0x12B);

                // Aqui o player só pode pedir para entrar no dele mesmo
                if (from_uid == to_uid && player.m_pi.mrc.allow_enter == 1)
                { // Isso tinha no season 4, agora nos season posteriores tiraram isso
                    p.WriteUInt32(1); // option;

                    p.WriteUInt32(to_uid);

                    p.WriteBytes(player.m_pi.mrc.ToArray());
                }
                else
                {
                    p.WriteUInt32(0);

                    p.WriteUInt32(to_uid);
                }

                session_send(p, pd._session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0B5][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return 0;

        }

        public static int packet0B7(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_pi.channel);

                if (c != null)
                    c.requestEnterMyRoom(player, pd._packet);
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[packet_func::packet0B7][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;

        }

        public static int packet0B9(object param, ParamDispatch pd)
        {
            try
            {
                var player = getPlayer(pd._session);

                sgs.gs.getInstance().requestUCCSystem(player, pd._packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0B9][ErrorSystem]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.GAME_SERVER)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0BA(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestInvite(player, pd._packet);
                }

            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[packet_func::packet0BA][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
            }
            return 0;
        }

        public static int packet0BD(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestUseCardSpecial(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0BD][ErrorSytem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0C1(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                player.m_pi.place = pd._packet.ReadSByte(); // Att place(lugar)

                // Update Location Player on DB
                player.m_pi.updateLocationDB();
                //sucess


            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0C1][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return 0;

        }

        public static int packet0C9(object param, ParamDispatch pd)
        {



            try
            {
                var player = getPlayer(pd._session);

                sgs.gs.getInstance().requestUCCWebKey(player, pd._packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0C9][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.GAME_SERVER)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0CA(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestOpenCardPack(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0CA][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0CB(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestFinishGame(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0CB][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0CC(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestCheckDolfiniLockerPass(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0CC][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0CD(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestDolfiniLockerItem(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0CD][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0CE(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestAddDolfiniLockerItem(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0CE][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0CF(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestRemoveDolfiniLockerItem(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0CF][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0D0(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestMakePassDolfiniLocker(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0D0][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0D1(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestChangeDolfiniLockerPass(player, pd._packet);
                }

            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[packet_func::packet0D1][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0D2(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestChangeDolfiniLockerModeEnter(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0D2][ErroSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0D3(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                uint check = 0;

                check = player.m_pi.df.isLocker();

                var p = new PangyaBinaryWriter((ushort)0x170);

                p.WriteUInt32(0); // option
                p.WriteUInt32(check);

                session_send(p, pd._session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0D3][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return 0;
        }

        public static int packet0D4(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestUpdateDolfiniLockerPang(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0D4][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0D5(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestDolfiniLockerPang(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0D5][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0D8(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestUseItemBuff(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0D8][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0DE(object param, ParamDispatch pd)
        {



            try
            {
                var player = getPlayer(pd._session);

                // Envia mensagem para o player que enviou o MP que o player não pode ver a mensagem
                sgs.gs.getInstance().requestNotifyNotDisplayPrivateMessageNow(player, pd._packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0DE][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.GAME_SERVER)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0E5(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestActiveCutin(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0E5][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0E6(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestExtendRental(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0E6][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0E7(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestDeleteRental(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0E7][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0EB(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);
                Channel c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestPlayerStateCharacterLounge(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0EB][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL) // Por Hora relança qualquer exception que não seja do channel
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0EC(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestCometRefill(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0EC][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0EF(object param, ParamDispatch pd)
        { 
            try
            {

                var player = getPlayer(pd._session);

                var c = gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestOpenBoxMail(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0EF][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet0F4(object param, ParamDispatch pd)
        {
            try
            {
                var player = getPlayer(pd._session);
                // Verifica se session está varrizada para executar esse ação,
                // se ele não fez o login com o Server ele não pode fazer nada até que ele faça o login
                 
                player.m_tick_bot = Environment.TickCount; 

                //pd._session.m_time_start = std::clock();
                //pd._session.m_tick = std::clock();
                //if (player.m_pi.uid > 0)
                //{
                //    var cp = player.m_pi.cookie;
                //    var pang = player.m_pi.ui.pang;
                //    player.m_pi.updateMoeda();
                //    // player.m_pi.ReloadMemberInfo();
                //    using (var p = new PangyaBinaryWriter())
                //    {
                //        if (cp != player.m_pi.cookie)  //so envia se estiver com valores novos
                //        {
                //            //// Update ON GAME(cookies)
                //            p.init_plain(0x96);
                //            p.WriteUInt64(player.m_pi.cookie);
                //            p.WriteUInt32(0);
                //            session_send(p, getPlayer(pd._session), 1);
                //        }
                //        if (pang != player.m_pi.ui.pang) //so envia se estiver com valores novos
                //        {
                //            // UPDATE pang ON GAME(pangs)
                //            p.init_plain(0xC8);
                //            p.WriteUInt64(player.m_pi.ui.pang);
                //            p.WriteUInt32(0);
                //            session_send(p, getPlayer(pd._session), 1);
                //        }
                //    }
                //}


            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[packet_func::packet0F4][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return 0;
        }

        public static int packet0FB(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var cmd_gwk = new CmdGeraWebKey(player.m_pi.uid);

                snmdb.NormalManagerDB.getInstance().add(0, cmd_gwk, null, null);


                if (cmd_gwk.getException().getCodeError() != 0)
                    throw cmd_gwk.getException();

                var webKey = cmd_gwk.getKey();

                session_send(pacote1AD(webKey, 1), pd._session);


            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0FB][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                session_send(pacote1AD("", 0), pd._session);
            }
            return 0;

        }

        public static int packet0FE(object param, ParamDispatch pd)
        {
            //packet 254, no send response!        
            try
            {
                var player = getPlayer(pd._session);

                UCCSystem.HandleUCCLoad(player);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet0FE][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;

        }

        public static int packet119(object param, ParamDispatch pd)
        {
            try
            {
                var player = getPlayer(pd._session);


                sgs.gs.getInstance().requestChangeServer(player, pd._packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet119][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return 0;

        }

        public static int packet126(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestOpenLegacyTikiShop(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet126][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw; // Relança
                }
            }
            return 0;
        }

        public static int packet127(object param, ParamDispatch pd)
        {

            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestPointLegacyTikiShop(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet127][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw; // Relança
                }
            }
            return 0;
        }

        public static int packet128(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestExchangeTPByItemLegacyTikiShop(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet128][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw; // Relança 
                }
            }
            return 0;
        }

        public static int packet129(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestExchangeItemByTPLegacyTikiShop(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet129][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw; // Relança
                }
            }
            return 0;
        }

        public static int packet12C(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestFinishGame(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet12C][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet12D(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestReplyInitialValueGrandZodiac(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet12D][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet12E(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestMarkerOnCourse(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet12E][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet12F(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestShotEndData(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet12F][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet130(object param, ParamDispatch pd)
        {
            var p = new PangyaBinaryWriter();

            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestLeavePractice(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet130][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }

        public static int packet131(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestLeaveChipInPractice(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet131][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet137(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestStartFirstHoleGrandZodiac(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet137][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet138(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestActiveWing(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet138][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet140(object param, ParamDispatch pd)
        {
            try
            { 
                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestEnterShop(player, pd._packet);
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet140][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return 0;
        }

        public static int packet141(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestChangeWindNextHoleRepeat(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet141][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet143(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestOpenMailBox(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet143][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet144(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestInfoMail(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet144][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet145(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestSendMail(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet145][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet146(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestTakeItemFomMail(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet146][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet147(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestDeleteMail(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet147][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet14B(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestPlayPapelShop(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet14B][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet151(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestDailyQuest(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet151][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet152(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestAcceptDailyQuest(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet152][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet153(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestTakeRewardDailyQuest(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet153][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet154(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestLeaveDailyQuest(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet154][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet155(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestLoloCardCompose(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet155][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;
        }

        public static int packet156(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestActiveAutoCommand(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet156][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet157(object param, ParamDispatch pd)
        {
            var p = new PangyaBinaryWriter();

            try
            {
                var player = getPlayer(pd._session);

                uint uid = pd._packet.ReadUInt32();

                AchievementManager mgr_achievement = null;
                Player s = null;

                if (player.m_pi.uid == uid) // O player solicitou o próprio achievement info
                {
                    mgr_achievement = player.m_pi.mgr_achievement;
                }
                else if ((s = sgs.gs.getInstance().findPlayer(uid)) != null) // O player solicitou o achievement info de outro player online
                {
                    mgr_achievement = s.m_pi.mgr_achievement;
                }
                else
                {
                    // O player solicitou o achievement info de outro player off-line 
                    mgr_achievement = new AchievementManager();

                    mgr_achievement.initAchievement(uid);

                    mgr_achievement.sendAchievementGuiToPlayer(player);
                }

                if (mgr_achievement == null)
                {
                    session_send(pacote22C(1),player); // unsucess  
                }
                else
                {
                    mgr_achievement.sendAchievementGuiToPlayer(player);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet157][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                session_send(pacote22C(1), getPlayer(pd._session)); // unsucess  
            }
            return 0;
        }

        public static int packet158(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestCadieCauldronExchange(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet158][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet15C(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestActivePaws(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet15C][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet15D(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestActiveRing(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet15D][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet164(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestClubSetWorkShopUpLevel(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet164][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet165(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestClubSetWorkShopUpLevelConfirm(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet165][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet166(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestClubSetWorkShopUpLevelCancel(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet166][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet167(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestClubSetWorkShopUpRank(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet167][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet168(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestClubSetWorkShopUpRankTransformConfirm(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet168][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet169(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestClubSetWorkShopUpRankTransformCancel(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet169][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet16B(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestClubSetWorkShopRecoveryPts(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet16B][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet16C(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestClubSetWorkShopTransferMasteryPts(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet16C][ErrorSystem] ", type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet16D(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestClubSetReset(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet16D][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet16E(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestCheckAttendanceReward(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet16E][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet16F(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestAttendanceRewardLoginCount(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet16F][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet171(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestActiveEarcuff(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet171][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet172(object param, ParamDispatch pd)
        {

            try
            {
                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                    c.requestOpenClubWorkShopEvent(player, pd._packet);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet172][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return 0;
        }

        public static int packet173(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestClubWorkShopEventCount(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet173][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;
        }


        public static int packet174(object param, ParamDispatch pd)
        {
            var player = getPlayer(pd._session);
            return 0;
        }


        public static int packet175(object param, ParamDispatch pd)
        {
            var player = getPlayer(pd._session);
            return 0;
        }

        public static int packet176(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestEnterLobbyGrandPrix(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet176][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet177(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestExitLobbyGrandPrix(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet177][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet179(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestEnterRoomGrandPrix(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet179][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet17A(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestExitRoomGrandPrix(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet17A][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet17F(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestPlayMemorial(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet17F][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet180(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestActiveGlove(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet180][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet181(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestActiveRingGround(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet181][ErroSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet184(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestToggleAssist(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet184][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet185(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestActiveAssistGreen(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet185][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet186(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestPlayBigPapelShop(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet186][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet187(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestCharacterMasteryExpand(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet187][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));


            }
            return 0;

        }

        public static int packet188(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestCharacterStatsUp(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet188][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet189(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestCharacterStatsDown(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet189][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet18A(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestCharacterCardEquip(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet18A][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet18B(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestCharacterCardEquipWithPatcher(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet18B][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet18C(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestCharacterRemoveCard(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet18C][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet18D(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestTikiShopExchangeItem(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet18D][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet192(object param, ParamDispatch pd)
        {
            try
            {
                var player = getPlayer(pd._session);
                // !@ Log
                _smp.message_pool.getInstance().push(new message("[packet_func::packet192][Log] PLAYER[UID=" + Convert.ToString(player.m_pi.uid) + "] request open Event Arin 2014.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                _smp.message_pool.getInstance().push(new message("[packet_func::packet192][Log] PLAYER[UID=" + Convert.ToString(player.m_pi.uid) + "] " + pd._packet.Log(), type_msg.CL_FILE_LOG_AND_CONSOLE));


            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet192][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }

        public static int packet196(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestActiveRingPawsRainbowJP(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet196][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet197(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestActiveRingPowerGagueJP(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet197][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet198(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestActiveRingMiracleSignJP(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet198][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }

        public static int packet199(object param, ParamDispatch pd)
        {



            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestActiveRingPawsRingSetJP(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet199][ErrorSystem]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }



        public static int packet_sv055(object param, ParamDispatch pd)
        {
            try
            {

                var player = getPlayer(pd._session);

                var c = sgs.gs.getInstance().findChannel(player.m_channel);

                if (c != null)
                {
                    c.requestInitShotSended(player, pd._packet);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[packet_func::packet_sv055][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if ((STDA_ERROR_TYPE)ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE.CHANNEL)
                {
                    throw;
                }
                return 0;
            }
            return 0;

        }


        private static void CHECK_SESSION_IS_AUTHORIZED(Player _session, string method)
        {
            if (!_session.m_is_authorized)
                throw new exception("[packet_func::" + ((method)) + "][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] Nao esta autorizado a fazer esse request por que ele ainda nao fez o login com o Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV, 1, 0x7000501));
        }


        //////

        public static int principal(ref PangyaBinaryWriter p, PlayerInfo pi, ServerInfoEx _si)
        {
            try
            {
                if (pi == null)
                    throw new exception("Erro PlayerInfo *pi is null. packet_func::principal()");

                if (_si.version_client.Length > 12)
                    throw new exception("Erro _si.version_client.Length > 12. packet_func::principal()");

                p.WritePStr(_si.version_client);

                //write struct member info player      
                p.WriteBytes(pi.getLoginInfo());//new version
                p.WriteUInt32(pi.uid);
                // write struct user info player(statistic)
                p.WriteBytes(pi.getUserInfo());//new version

                // write struct Trofel Info
                p.WriteBytes(pi.getInfoTrophy());
                //write struct User Equip
                p.WriteBytes(pi.getUserEquip());//new version

                p.WriteBytes(pi.GetMapStatistic());
                //Equiped Items
                #region EquipedItem
                p.WriteBytes(pi.getUserEquipedItem());
                #endregion
                // Write Time, 16 Bytes
                p.WriteTime();

                // Config do Server(struct for server)
                p.WriteUInt16(0); //server_state_flag, Valor padrão, 1 na primeira vez, 2 para logins subsequentes 
                p.WriteBytes(pi.mi.papel_shop.ToArray());//aqui e outro
                p.WriteUInt32(pi.mi.point_point_event); // point_point_event. Valor novo no JP, indicado como 0 em novas contas 
                p.WriteUInt64(pi.block_flag.m_flag.ullFlag); // Flag do server para bloquear sistemas 
                p.WriteInt32(pi.ToTalClubsetCNT + pi.ToTalPartsCNT); // Quantidade de vezes que logou 
                p.WriteUInt32(_si.propriedade.ulProperty);
                return 0;
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[packet_func_gs::InitialLogin][ErrorSystem]: " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return 1;
            }
        }

        public static byte[] pacote047(List<RoomInfoEx> v_element, int option)
        {

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            p.init_plain(0x47);
            p.WriteByte((byte)((option == 0) ? v_element.Count() : 1));              // count;
            p.WriteByte((byte)option);
            p.WriteUInt16(-1);                 // Não sei bem, mas sempre peguei esse pacote com -1 aqui             
            for (var i = 0; i < v_element.Count(); ++i)
                p.WriteBytes(v_element[i].ToArray());

            return p.GetBytes;
        }

        public static List<PangyaBinaryWriter> pacote046(List<PlayerLobbyInfo> v_element, int option)
        {
            var responses = new List<PangyaBinaryWriter>();
            int elements = v_element.Count;
            int itensPorPacote = 20;

            // Divide a lista apenas se necessário
            var splitList = (elements * 200 < (MAX_BUFFER_PACKET - 100))
                ? new List<List<PlayerLobbyInfo>> { v_element } // Envia tudo em um pacote
                : v_element.Select((item, index) => new { item, index })
                           .GroupBy(x => x.index / itensPorPacote)
                           .Select(g => g.Select(x => x.item).ToList())
                           .ToList();

            // Gera pacotes corretamente
            foreach (var lista in splitList)
            {
                var p = new PangyaBinaryWriter(0x046);
                p.WriteByte((byte)option);
                p.WriteByte((byte)lista.Count);

                foreach (var item in lista)
                    p.WriteBytes(message: item.ToArray());

                responses.Add(p);
            }

            return responses;
        }

        public static byte[] pacote11F(PlayerInfo pi, short tipo)
        {
            var p = new PangyaBinaryWriter();
            if (pi == null)
                throw new exception("Erro PlayerInfo *pi is null. packet_func::pacote11F()");

            p.init_plain(0x11F);

            p.WriteInt16(tipo);

            p.WriteBytes(pi.TutoInfo.ToArray());
            return p.GetBytes;
        }

        public static byte[] pacote1A9(int ttl_milliseconds/*time to live*/, int option = 1)
        {
            var p = new PangyaBinaryWriter(0x1A9);

            p.WriteByte((byte)option);

            p.WriteInt32(ttl_milliseconds);
            return p.GetBytes;
        }

        public static byte[] pacote095(short sub_tipo, int option = 0, PlayerInfo pi = null)
        {
            var p = new PangyaBinaryWriter(0x95);

            p.WriteInt16(sub_tipo);

            if (sub_tipo == 0x102)
                p.WriteByte((byte)option);
            else if (sub_tipo == 0x111)
            {
                p.WriteInt32(option);

                if (pi == null)
                {
                    throw new exception("Erro PlayerInfo *pi is null. packet_func::pacote095()");
                }

                p.WriteUInt64(pi.ui.pang);
            }
            return p.GetBytes;
        }

        public static List<PangyaBinaryWriter> pacote25D(List<TrofelEspecialInfo> v_element, int option)
        {
            var responses = new List<PangyaBinaryWriter>();
            int elements = v_element.Count;
            int itensPorPacote = 20;

            // Divide a lista apenas se necessário
            var splitList = (elements * 200 < (MAX_BUFFER_PACKET - 100))
                ? new List<List<TrofelEspecialInfo>> { v_element } // Envia tudo em um pacote
                : v_element.Select((item, index) => new { item, index })
                           .GroupBy(x => x.index / itensPorPacote)
                           .Select(g => g.Select(x => x.item).ToList())
                           .ToList();

            // Gera pacotes corretamente
            foreach (var lista in splitList)
            {
                var p = new PangyaBinaryWriter(0x25D);
                p.WriteByte((byte)option);
                p.WriteUInt32((uint)lista.Count);
                p.WriteUInt32((uint)lista.Count);

                foreach (var item in lista)
                {
                    p.WriteBytes(item.ToArray());
                }

                responses.Add(p);
            }

            return responses;
        }
        public static byte[] pacote156(uint _uid, UserEquip _ue, byte season)
        {
            var p = new PangyaBinaryWriter(0x156);

            p.WriteByte(season);

            p.WriteUInt32(_uid);
            p.WriteBytes(_ue.ToArray());
            return p.GetBytes;
        }


        public static byte[] pacote157(MemberInfoEx _mi, byte season)
        {
            var p = new PangyaBinaryWriter(0x157);

            p.WriteByte(season);
            p.WriteUInt32(_mi.uid);
            p.WriteBytes(_mi.ToArrayEx());
            p.WriteUInt32(_mi.uid);
            p.WriteUInt32(_mi.guild_point);
            return p.GetBytes;
        }

        public static byte[] pacote158(uint _uid, UserInfoEx _ui, byte season)
        {
            var p = new PangyaBinaryWriter(0x158);

            p.WriteByte((byte)season);
            p.WriteUInt32(_uid);
            p.WriteBytes(_ui.ToArray());//new version 
            return p.GetBytes;
        }

        public static byte[] pacote159(uint uid, TrofelInfo ti, byte season)
        {
            var p = new PangyaBinaryWriter(0x159);
            p.WriteByte(season);
            p.WriteUInt32(uid);
            p.WriteBytes(ti.ToArray());
            return p.GetBytes;
        }

        public static byte[] pacote15A(uint uid, List<TrofelEspecialInfo> vTei, byte season)
        {
            var p = new PangyaBinaryWriter(0x15A);
            p.WriteByte(season);
            p.WriteUInt32(uid);
            p.WriteUInt16((ushort)vTei.Count);

            foreach (var item in vTei)
                p.WriteBytes(item.ToArray());

            return p.GetBytes;
        }

        public static byte[] pacote15B(uint uid, byte season)
        {
            var p = new PangyaBinaryWriter(0x15B);
            p.WriteByte(season);
            p.WriteUInt32(uid);
            p.WriteInt16(1); // Count desconhecido
            for (int i = 0; i < 60; i++)
                p.Write(i);
            return p.GetBytes;
        }

        public static byte[] pacote15C(uint uid, List<MapStatisticsEx> vMs, List<MapStatisticsEx> vMsa, byte season)
        {
            var p = new PangyaBinaryWriter(0x15C);
            p.WriteByte(season);
            p.WriteUInt32(uid);
            p.WriteInt32(vMs.Count);

            foreach (var item in vMs)
                p.WriteBytes(item.ToArray());

            p.WriteInt32(vMsa.Count);

            foreach (var item in vMsa)
                p.WriteBytes(item.ToArray());

            return p.GetBytes;
        }

        public static byte[] pacote15D(uint uid, GuildInfo gi)
        {
            var p = new PangyaBinaryWriter(0x15D);
            p.WriteUInt32(uid);
            p.WriteBytes(gi.ToArray());
            return p.GetBytes;
        }

        public static byte[] pacote15E(uint uid, CharacterInfo ci)
        {
            var p = new PangyaBinaryWriter(0x15E);
            p.WriteUInt32(uid);
            p.WriteBytes(ci.ToArray());
            return p.GetBytes;
        }

        public static byte[] pacote096(PlayerInfo pi)
        {
            if (pi == null)
                throw new exception("Erro PlayerInfo *pi is null. packet_func::pacote096()");
            using (var p = new PangyaBinaryWriter(0x96))
            {
                p.WriteUInt64(pi.cookie);
                return p.GetBytes;
            }
        }

        public static byte[] pacote181(List<ItemBuffEx> v_element, int option = 0)
        {
            using (var p = new PangyaBinaryWriter(0x181))
            {
                p.WriteInt32(option);

                if (option == 0)
                {
                    p.WriteByte(v_element.Count());
                    for (int i = 0; i < v_element.Count; i++)
                        p.WriteBytes(v_element[i].ToArray());

                }
                else if (option == 2)
                {
                    p.WriteUInt32((uint)v_element.Count);

                    for (int i = 0; i < v_element.Count; i++)
                    {
                        p.WriteUInt32(v_element[i]._typeid);
                        p.WriteBytes(v_element[i].ToArray());

                    }
                }
                else
                    p.WriteByte(0);

                return p.GetBytes;
            }
        }

        public static byte[] pacote13F(int option = 0)
        {
            using (var p = new PangyaBinaryWriter(0x13F))
            {
                p.WriteByte(option);
                return p.GetBytes;
            }
        }

        public static byte[] pacote135()
        {
            using (var p = new PangyaBinaryWriter(0x135))
            {
                return p.GetBytes;
            }
        }

        public static byte[] pacote136()
        {
            using (var p = new PangyaBinaryWriter(0x136))
            {
                return p.GetBytes;
            }
        }

        public static byte[] pacote137(CardEquipManager v_element)
        {
            using (var p = new PangyaBinaryWriter(0x137))
            {
                p.WriteUInt16((short)v_element.Count());
                foreach (var CardEquip in v_element)
                {
                    p.WriteBytes(CardEquip.ToArray());
                }
                return p.GetBytes;
            }
        }

        public static byte[] pacote138(CardManager v_element, int option = 0)
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.init_plain(0x138);
                p.WriteInt32(option);
                p.WriteUInt16((ushort)v_element.Count);
                foreach (var Card in v_element.Values)
                    p.WriteBytes(Card.ToArray());

                return p.GetBytes;
            }
        }

        public static byte[] pacote1F()
        {
            using (var p = new PangyaBinaryWriter(0x01F))
            {
                return p.GetBytes;
            }
        }

        public static byte[] pacote131(int option = 1)
        {
            if (!sTreasureHunterSystem.getInstance().isLoad())
                sTreasureHunterSystem.getInstance().load();

            using (var p = new PangyaBinaryWriter(new byte[] { 0x31, 0x01, Convert.ToByte(option), Convert.ToByte(MS_NUM_MAPS) }))
            {
                var _TreasureHunterInfo = sTreasureHunterSystem.getInstance().getAllCoursePoint();

                foreach (var _TreasureHunter in _TreasureHunterInfo)
                {
                    if (_TreasureHunter.point < 1000)//abaixo ou sem dados preciso, fica zerado
                        _TreasureHunter.point = 1000;

                    p.WriteBytes(message: _TreasureHunter.ToArray());
                }
                return p.GetBytes;
            }
        }

        public static byte[] pacote072(UserEquip ue)
        {
            var p = new PangyaBinaryWriter();

            p.init_plain(0x72);
            p.WriteBytes(ue.ToArray());
            return p.GetBytes;
        }

        public static byte[] pacote0E1(MascotManager v_element, int option = 0)
        {
            var p = new PangyaBinaryWriter(0xE1);

            p.Write(v_element.Build());
            return p.GetBytes;
        }

        public static byte[] pacote0E2()
        {
            return new byte[] { 0x0E, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        }

        public static PangyaBinaryWriter pacote21E(List<AchievementInfoEx> v_element, int option = 0)
        {
            var p = new PangyaBinaryWriter();
            try
            {
                p.init_plain(0x21E);
                p.WriteUInt32(0); // SUCCESS    
                p.WriteUInt32((uint)v_element.Count);
                p.WriteUInt32((uint)v_element.Count);
                foreach (var ai in v_element)
                {
                    p.WriteByte(ai.active);
                    p.WriteUInt32(ai._typeid);
                    p.WriteInt32(ai.id);
                    p.WriteInt32(ai.status);
                    p.WriteUInt32((uint)ai.v_qsi.Count);

                    foreach (var qsi in ai.v_qsi)
                    {
                        CounterItemInfo cii = null;

                        p.WriteUInt32(qsi._typeid);

                        if (qsi.counter_item_id > 0 && (cii = ai.findCounterItemById(qsi.counter_item_id)) != null)
                        {
                            p.WriteUInt32(cii._typeid);
                            p.WriteInt32(cii.id);
                        }
                        else
                        {
                            p.WriteZero(8);
                        }

                        p.WriteUInt32(qsi.clear_date_unix);
                    }
                }
                return p;
            }
            catch
            {
                return p;
            }
        }

        public static PangyaBinaryWriter pacote21D(List<CounterItemInfo> v_element, int option = 0)
        {
            var p = new PangyaBinaryWriter();
            try
            {
                p.init_plain(0x21D);
                p.WriteUInt32(0); // SUCCESS    
                p.WriteUInt32((uint)v_element.Count);
                p.WriteUInt32((uint)v_element.Count);
                foreach (var counter in v_element)
                {
                    p.WriteByte(counter.active);//;
                    p.WriteUInt32(counter._typeid);//
                    p.WriteInt32(counter.id);//
                    p.WriteInt32(counter.value);//
                }
                return p;
            }
            catch
            {
                return p;
            }
        }

        public static PangyaBinaryWriter pacote22D(List<AchievementInfoEx> v_element, int option = 0)
        {
            var p = new PangyaBinaryWriter();
            try
            {
                p.init_plain(0x22D);
                p.WriteUInt32(0); // SUCCESS
                p.WriteUInt32((uint)v_element.Count());
                p.WriteUInt32((uint)v_element.Count());

                foreach (var ai in v_element)
                {
                    p.WriteUInt32(ai._typeid);
                    p.WriteInt32(ai.id);
                    p.WriteUInt32((uint)ai.v_qsi.Count);
                    CounterItemInfo cii = null;
                    foreach (var qsi in ai.v_qsi)
                    {
                        p.WriteUInt32(qsi._typeid);
                        p.WriteInt32(qsi.counter_item_id > 0 && (cii = ai.findCounterItemById(qsi.counter_item_id)) != null ? cii.value : 0);
                        p.WriteUInt32(qsi.clear_date_unix);
                    }
                }
                return p;
            }
            catch
            {
                return p;
            }
        }

        public static PangyaBinaryWriter pacote22C(int option = 0)
        {
            var p = new PangyaBinaryWriter();
            try
            {
                p.init_plain(0x22C);
                p.WriteInt32(option); // SUCCESS

                return p;
            }
            catch
            {
                return p;
            }
        }

        public static PangyaBinaryWriter pacote073(List<WarehouseItemEx> v_element, int Count = 0, int option = 0)
        {
            var p = new PangyaBinaryWriter();
            p.init_plain(value: 0x73);
            try
            {
                p.WriteUInt16(Count);
                p.WriteUInt16(option);
                foreach (var item in v_element)
                {
                    p.WriteBytes(item.ToArray());
                }
                return p;
            }
            catch
            {
                if (p.GetSize == 2)
                {
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                }
                return p;
            }
        }

        public static byte[] pacote071(CaddieManager v_element, int option = 0)
        {
            var p = new PangyaBinaryWriter();
            try
            {
                p.init_plain(0x71);
                p.WriteInt16((short)v_element.Count);
                p.WriteInt16((short)v_element.Count);
                foreach (var char_info in v_element.Values)
                {
                    p.WriteBytes(char_info.getInfo().ToArray());
                }
                return p.GetBytes;
            }
            catch (Exception)
            {
                return p.GetBytes;
            }
        }

        /// <summary>
        /// Send Packet for Info Characters(Personagens)
        /// </summary>
        /// <param name="v_element">object list</param>
        /// <param name="option">what?</param>
        /// <returns>obj using for write data</returns>
        public static byte[] pacote070(CharacterManager v_element, int option = 0)
        {
            var p = new PangyaBinaryWriter();
            try
            {
                p.init_plain(0x70);
                p.WriteInt16((short)v_element.Count);
                p.WriteInt16((short)v_element.Count);
                foreach (var char_info in v_element.Values)
                {
                    p.WriteBytes(char_info.ToArray());
                }
                return p.GetBytes;
            }
            catch (Exception)
            {
                return p.GetBytes;
            }
        }

        /// <summary>
        /// packet 9D use channel list!
        /// </summary>
        /// <param name="v_element"></param>
        /// <param name="build_s">true is server, false is chanell call!</param>
        /// <returns></returns>
        public static byte[] pacote04D(List<Channel> v_element, bool build_s = false)
        {
            try
            {
                using (var p = new PangyaBinaryWriter())
                {
                    if (!build_s)
                        p.init_plain(0x4D); //channel list!         

                    p.WriteByte(v_element.Count);
                    foreach (var channel in v_element)
                        p.WriteBytes(channel.getInfo().ToArray());

                    return p.GetBytes;
                }
            }
            catch (exception ex)
            {
                _smp.message_pool.getInstance().push(new message(
               $"[packet_func::pacote04D][ErrorSystem] {ex.getFullMessageError()}",
               type_msg.CL_FILE_LOG_AND_CONSOLE));
                return new byte[] { 0x4D, 0x00, 0x00 };
            }
        }

        public static byte[] pacote248(
            AttendanceRewardInfo ari,
            int option = 0)
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.Write(new byte[] { 0x48, 0x02 });
                p.WriteInt32(option);
                p.WriteBytes(ari.ToArray());
                return p.GetBytes;
            }
        }

        public static byte[] pacote249(
            AttendanceRewardInfo ari,
            int option = 0)
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.Write(new byte[] { 0x49, 0x02 });
                p.WriteInt32(option);
                p.WriteBytes(ari.ToArray());
                return p.GetBytes;
            }
        }

        public static byte[] pacote24E(
           club_work_shop_event_type work_Shop_Event,
           int option = 0)
        {
            using (var p = new PangyaBinaryWriter())
            {
                work_Shop_Event.Calc();

                p.init_plain(0x24E); // packet id
                p.WriteInt32(option);                // subcode (fixo)
                p.WriteInt32(3000);   // quantos holes são exigidos por fase
                p.WriteInt32(0);      // total  de holes jogados

                p.WriteByte(3000 / 30);         // valor máximo da barra
                p.WriteByte(0);       // valor atual da barra 
                p.WriteByte(work_Shop_Event.barraMax);         // valor máximo da barra
                p.WriteByte(10);       // valor atual da barra

                return p.GetBytes;
            }
        }

        public static byte[] pacoteRandom(
          PacketIDServer id,
          int option = 0)
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.init_plain(id); // packet id
                p.WriteInt32(0);                // subcode (fixo)
                p.WriteInt32(18);   // quantos holes são exigidos por fase
                p.WriteInt32(9);      // total de holes jogados

                p.WriteByte(1);         // valor máximo da barra
                p.WriteByte(0);       // valor atual da barra 
                p.WriteByte(10);       // valor atual da barra

                return p.GetBytes;
            }
        }

        public static byte[] pacote257(uint _uid, List<TrofelEspecialInfo> v_tegi, byte season)
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.init_plain(0x257);

                p.WriteByte(season);
                p.WriteUInt32(_uid);

                p.WriteInt16((short)v_tegi.Count);
                foreach (var item in v_tegi)
                    p.WriteBytes(item.ToArray());
                return p.GetBytes;
            }
        }

        public static byte[] pacote04E(int option, int _codeErrorInfo = 0)
        {
            /* Option Values
                * 1 Sucesso
                * 2 Channel Full
                * 3 Nao encontrou canal
                * 4 Nao conseguiu pegar informções do canal
                * 6 ErrorCode Info
                */
            using (var p = new PangyaBinaryWriter(0x4E))
            {
                p.WriteByte((byte)option);

                if (_codeErrorInfo != 0)
                    p.WriteInt32(_codeErrorInfo);
                return p.GetBytes;
            }
        }


        public static byte[] pacote040(string nick, string msg, eChatMsg option)
        {

            if ((option == eChatMsg.CHAT_NORMAL || option == eChatMsg.CHAT_GM || option == eChatMsg.CHAT_MAX) && string.IsNullOrEmpty(nick))
                throw new exception("Error PlayerInfo *pi is null. packet_func::pacote040()");

            using (var p = new PangyaBinaryWriter(0x40))
            {
                p.WriteByte(option);

                if (option == 0 || (option == eChatMsg.CHAT_GM) || option == eChatMsg.CHAT_REFUSE_WHISPER)
                {
                    p.WritePStr(nick);
                    if (option != eChatMsg.CHAT_REFUSE_WHISPER)
                        p.WritePStr(msg);
                }
                return p.GetBytes;
            }
        }

        public static byte[] pacote044(ServerInfoEx _si, eLoginAck option, PlayerInfo pi = null, int valor = 0)
        {
            var p = new PangyaBinaryWriter(0x44);

            if (option == eLoginAck.ACK_LOGIN_OK && pi == null)
                throw new exception("Erro PlayerInfo *pi is null. packet_func::pacote044()");

            p.WriteByte(option);   // Option

            switch (option)
            {
                case eLoginAck.ACK_LOGIN_FAIL: // 1:
                    p.WriteByte(0);
                    break;
                case eLoginAck.ACK_AUTO_RECONNECT:
                    p.WriteByte(0);
                    break;
                case eLoginAck.ACK_UPDATE_LOGIN_UNIT:
                    p.WriteInt32(valor);
                    break;
                default:
                    if (option == eLoginAck.ACK_LOGIN_OK && principal(ref p, pi, _si) == 1)
                    { }
                    break;
            }
            return p.GetBytes;
        }

        public static byte[] pacote0B2(

List<MsgOffInfo> v_element,
int option = 0)
        {
            var p = new PangyaBinaryWriter();

            p.init_plain(0xB2);

            p.WriteInt32(2); // Não sei bem o que é, mas pode ser uma opção

            p.WriteInt32(option);

            p.WriteUInt32((uint)v_element.Count);

            foreach (MsgOffInfo i in v_element)
            {
                p.WriteBytes(i.ToArray());
            }

            return p.GetBytes;
        }

        public static byte[] pacote0D4(CaddieManager v_element)
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.init_plain(0xD4);
                p.WriteUInt32((uint)v_element.Count());
                foreach (var item in v_element.Values)
                    p.WriteBytes(item.getInfo().ToArray());

                return p.GetBytes;
            }
        }

        // Metôdos de auxílio de criação de pacotes


        public static byte[] pacote210(

                List<MailBox> v_element,
                int option = 0)
        {
            var p = new PangyaBinaryWriter();

            p.init_plain(0x210);

            p.WriteInt32(option);

            p.WriteInt32(v_element.Count);

            for (var i = 0; i < v_element.Count; ++i)
            {
                p.WriteBytes(v_element[i].ToArray());
            }

            return p.GetBytes;
        }



        public static byte[] pacote211(
            List<MailBox> v_element,
            int pagina,
            int paginas, int error = 0)
        {
            var p = new PangyaBinaryWriter();

            p.init_plain(0x211);

            p.WriteInt32(error);

            if (error == 0)
            {
                p.WriteInt32(pagina);
                p.WriteInt32(paginas);
                p.WriteInt32(v_element.Count);

                for (var i = 0; i < v_element.Count; ++i)
                    p.WriteBytes(v_element[i].ToArray());
            }

            return p.GetBytes;
        }

        public static byte[] pacote214(int error = 0)
        {

            var p = new PangyaBinaryWriter();
            p.init_plain(0x214);

            p.WriteInt32(error);

            return p.GetBytes;
        }

        public static byte[] pacote215(
            List<MailBox> v_element,
            int pagina,
            int paginas, int error = 0)
        {
            var p = new PangyaBinaryWriter();

            p.init_plain(0x215);

            p.WriteInt32(error);

            if (error == 0)
            {
                p.WriteInt32(pagina);
                p.WriteInt32(paginas);
                p.WriteUInt32((uint)v_element.Count);

                for (var i = 0; i < v_element.Count; ++i)
                {
                    p.WriteBytes(v_element[i].ToArray());
                }
            }

            return p.GetBytes;
        }

        public static byte[] pacote216(
            List<stItem> v_item,
            int option = 0)
        {

            var p = new PangyaBinaryWriter();
            p.init_plain(0x216);

            p.WriteInt32((int)UtilTime.GetSystemTimeAsUnix());

            if (v_item.Count > 0)
            {
                p.WriteInt32(v_item.Count);

                foreach (stItem i in v_item)
                {

                    // Begin Base Item
                    p.WriteByte(i.type);
                    p.WriteUInt32(i._typeid);
                    p.WriteInt32(i.id);
                    p.WriteUInt32(i.flag_time);
                    p.WriteBytes(i.stat.ToArray());
                    p.WriteInt32((i.STDA_C_ITEM_TIME > 0) ? i.STDA_C_ITEM_TIME : i.STDA_C_ITEM_QNTD);

                    // End Base Item

                    if (i.type == 2)
                    {
                        try
                        {
                            p.WriteString(i.ucc.IDX);
                        }
                        catch (exception e)
                        {
                            if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.PACKET && ExceptionError.STDA_ERROR_DECODE(e.getCodeError()) == 3)
                            {
                                p.WriteInt16(0);
                            }
                            else
                            {
                                throw;
                            }
                        }

                        p.WriteUInt32(i.ucc.status);
                        p.WriteUInt32(i.ucc.seq);
                        p.WriteZeroByte(5); // É o Unknown de cima
                    }
                }
            }
            else
            {
                p.WriteInt32(option);
            }

            return p.GetBytes;
        }

        public static byte[] pacote10E(Last5PlayersGame l5pg)
        {
            var p = new PangyaBinaryWriter(0x10E);
            foreach (var p_log in l5pg.players)
            {
                p.WriteBytes(p_log.ToArray());
            }
            return p.GetBytes;
        }
        public static byte[] pacote0FC(List<ServerInfo> v_si)
        {
            var p = new PangyaBinaryWriter(0xFC);
            p.WriteByte((byte)v_si.Count);

            foreach (ServerInfo i in v_si)
                p.WriteBytes(i.ToArray());

            return p.GetBytes;
        }



        public static byte[] pacote101(int option = 0)
        {
            var p = new PangyaBinaryWriter(0x101);
            p.WriteByte(value: (byte)option);
            return p.GetBytes;
        }
        public static byte[] pacote0B4(List<TrofelEspecialInfo> v_element, int option = 0)
        {
            var p = new PangyaBinaryWriter();

            p.init_plain(0xB4);

            p.WriteInt16((short)option);

            p.WriteByte((byte)v_element.Count);

            foreach (TrofelEspecialInfo i in v_element)
            {
                p.Write(i.id);
                p.Write(i._typeid);
                p.Write(i.qntd);
            }

            return p.GetBytes;
        }

        public static byte[] pacote0F1(int option = 0)
        {
            var p = new PangyaBinaryWriter();

            p.init_plain(0xF1);

            p.WriteByte((byte)option);

            return p.GetBytes;
        }


        public static byte[] pacote0F5()
        {
            var p = new PangyaBinaryWriter(0x0F5);
            return p.GetBytes;
        }


        public static byte[] pacote0F6()
        {
            var p = new PangyaBinaryWriter(0x0F6);
            return p.GetBytes;
        }


        public static byte[] pacote169(
           TrofelInfo ti,
            int option = 0)
        {
            var p = new PangyaBinaryWriter(0x169);

            p.WriteByte((byte)option);

            p.WriteBytes(ti.ToArray());

            return p.GetBytes;
        }

        public static byte[] pacote09F(List<ServerInfo> v_server, List<Channel> v_channel)
        {
            using (var p = new PangyaBinaryWriter(0x09F))
            {
                p.WriteByte((byte)v_server.Count);

                for (var i = 0; i < v_server.Count; ++i)
                    p.WriteBytes(v_server[i].ToArray());

                p.WriteBytes(pacote04D(v_channel, true));
                return p.GetBytes;
            }
        }

        public static byte[] pacote089(uint _uid = 0, byte season = 0, uint err_code = 1)
        {

            using (var p = new PangyaBinaryWriter((ushort)0x089))
            {
                p.WriteUInt32(err_code);
                if (err_code > 0)
                {
                    p.WriteByte(season);
                    p.WriteUInt32(_uid);
                }
                return p.GetBytes;
            }
        }

        public static byte[] pacote211(List<MailBox> v_element, uint pagina, uint paginas, uint error = 0)
        {

            using (var p = new PangyaBinaryWriter(0x211))
            {
                p.WriteUInt32(error);

                if (error == 0)
                {
                    p.WriteUInt32(pagina);
                    p.WriteUInt32(paginas);
                    p.WriteInt32(v_element.Count);

                    for (int i = 0; i < v_element.Count; ++i)
                        p.WriteBytes(v_element[i].ToArray());
                }

                return p.GetBytes;
            }
        }

        public static byte[] pacote212(EmailInfo ei, uint error = 0)
        {

            using (var p = new PangyaBinaryWriter(0x212))
            {
                p.WriteUInt32(error);

                if (error == 0)
                    p.WriteBytes(ei.ToArray());

                return p.GetBytes;
            }
        }


        public static byte[] pacote06B(PlayerInfo pi, byte type, int err_code = 4)
        {

            if (pi == null)
            {
                throw new exception("Erro PlayerInfo *pi is null. packet_func::pacote06B()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV,
                    1, 0));
            }
            var p = new PangyaBinaryWriter(0x06B);
            p.WriteByte(err_code); // Error Code, 4 Sucesso, diferente é erro
            p.WriteByte(type);

            if (err_code == 4)
            {
                switch (type)
                {
                    case 0: // Character Equipado Com os Parts Equipado
                        if (pi.ei.char_info != null)
                            p.WriteBytes(pi.ei.char_info.ToArray());
                        else
                            p.WriteZero(513);
                        break;
                    case 1: // Caddie Equipado
                        if (pi.ei.cad_info != null)
                            p.WriteInt32(pi.ei.cad_info.id);
                        else
                            p.WriteInt32(0);
                        break;
                    case 2: // Itens Equipáveis
                        p.WriteUInt32(pi.ue.item_slot);
                        break;
                    case 3: // Ball e Clubset Equipado
                        if (pi.ei.comet != null) // Ball
                            p.WriteUInt32(pi.ei.comet._typeid);
                        else
                            p.WriteInt32(0);

                        p.WriteInt32(pi.ei.csi.id); // ClubSet ID
                        break;
                    case 4: // Skins
                        p.WriteUInt32(pi.ue.skin_typeid);
                        break;
                    case 5: // Only Chracter Equipado
                        if (pi.ei.char_info != null)
                        {
                            p.WriteInt32(pi.ei.char_info.id);
                        }
                        else
                        {
                            p.WriteZero(4);
                        }
                        break;
                    case 8: // Mascot Equipado
                        if (pi.ei.mascot_info != null)
                        {
                            p.WriteBytes(pi.ei.mascot_info.ToArray());
                        }
                        else
                        {
                            p.WriteZero(62);
                        }
                        break;
                    case 9: // Character Cutin Equipado
                        if (pi.ei.char_info != null)
                        {
                            p.WriteInt32(pi.ei.char_info.id);
                            p.WriteUInt32(pi.ei.char_info.cut_in);
                        }
                        else
                        {
                            p.WriteZero(20);
                        }
                        break;
                    case 10: // Poster Equipado
                        p.WriteUInt32(pi.ue.poster);
                        break;
                }
            }

            return p.GetBytes;
        }

        public static byte[] pacote1D4(string _AuthKeyLogin, int option = 0)
        {
            using (var p = new PangyaBinaryWriter(0x1D4))
            {
                p.WriteInt32(option);

                if (option == 0 && !string.IsNullOrEmpty(_AuthKeyLogin))
                    p.WritePStr(_AuthKeyLogin);

                return p.GetBytes;
            }
        }

        public static PangyaBinaryWriter pacote04B(Player _session, byte _type,
         int error = 0, int _valor = 0)
        {

            var p = new PangyaBinaryWriter(0x4B);

            if (_session == null)
                throw new exception("Error _session is null. Em packet_func::pacote04B()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV,
                       1, 0));

            if (!_session.getState())
                throw new exception("Error player nao esta mais connectado. Em packet_func::pacote04B()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV,
                       2, 0));


            p.WriteInt32(error);

            if (error == 0)
            {
                p.WriteByte(_type);

                p.WriteInt32(_session.m_oid);

                switch (_type)
                {
                    case 1: // Caddie
                        if (_session.m_pi.ei.cad_info != null)
                        {
                            p.WriteBytes(_session.m_pi.ei.cad_info.ToArray());
                        }
                        else
                        {
                            p.WriteZero(25);
                        }
                        break;
                    case 2: // Ball(Comet)
                        if (_session.m_pi.ei.comet != null)
                        {
                            p.WriteUInt32(_session.m_pi.ei.comet._typeid);
                        }
                        else
                        {
                            p.WriteZero(4);
                        }
                        break;
                    case 3: // ClubSet
                        p.WriteBytes(_session.m_pi.ei.csi.ToArray());
                        break;
                    case 4: // Character
                        if (_session.m_pi.ei.char_info != null)
                        {
                            p.WriteBytes(_session.m_pi.ei.char_info.ToArray());
                        }
                        else
                        {
                            p.WriteZero(513);
                        }
                        break;
                    case 5: // Mascot
                        if (_session.m_pi.ei.mascot_info != null)
                        {
                            p.WriteBytes(_session.m_pi.ei.mascot_info.ToArray());
                        }
                        else
                        {
                            p.WriteZero(62);
                        }
                        break;
                    case 6: // Itens Active 1 = Jester big cabeça, 2 = Hermes velocidade x2, 3 = Twilight Fogos na cabeça
                        {
                            p.WriteInt32(_valor);

                            if (_valor == (int)ChangePlayerItemRoom.stItemEffectLounge.TYPE_EFFECT.TE_TWILIGHT)
                            {
                                p.WriteInt32(1); // Ativa Fogos
                            }
                            else
                            {

                                if (_session.m_pi.ei.char_info != null)
                                {
                                    var it = (_session.m_pi.ei.char_info == null) ? _session.m_pi.mp_scl.end() : _session.m_pi.mp_scl.find(_session.m_pi.ei.char_info.id);

                                    if (it.Value == null)
                                    {

                                        _smp.message_pool.getInstance().push(new message("[channel::pacote04B][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem os estados do character na lounge. Criando um novo para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                        // Add New State Character Lounge
                                        var pair = _session.m_pi.mp_scl.insert(Tuple.Create(_session.m_pi.ei.char_info.id, new StateCharacterLounge()));

                                        it = pair;
                                    }

                                    switch ((ChangePlayerItemRoom.stItemEffectLounge.TYPE_EFFECT)_valor)
                                    {
                                        case ChangePlayerItemRoom.stItemEffectLounge.TYPE_EFFECT.TE_BIG_HEAD: // Jester (Big head)
                                            p.WriteFloat(it.Value.scale_head);
                                            break;
                                        case ChangePlayerItemRoom.stItemEffectLounge.TYPE_EFFECT.TE_FAST_WALK: // Hermes (Velocidade x2)
                                            p.WriteFloat(it.Value.walk_speed);
                                            break;
                                    }
                                }
                            }
                        }
                        break;
                    case 7: // Player game
                            // Nada Aqui
                        {
                        }
                        break;
                    default:
                        throw new exception("Error tipo desconhecido. Em packet_func::pacote04B()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV,
                            3, 0));
                }
            }

            return p;
        }

        public static byte[] pacote1AD(string webKey, int option)
        {
            using (var p = new PangyaBinaryWriter(0x1AD))
            {
                p.WriteInt32(option);

                if (webKey.empty())
                    p.WriteInt16(0);
                else
                    p.WritePStr(webKey);

                return p.GetBytes;
            }
        }

        public static byte[] pacote102(PlayerInfo pi)
        {
            if (pi == null)
            {
                throw new exception("[packet_func::pacote12][Error] PlayerInfo *pi is null.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV,
                    1, 0));
            }
            using (var p = new PangyaBinaryWriter(0x102))
            {
                p.WriteInt32(pi.cg.normal_ticket);
                p.WriteInt32(pi.cg.partial_ticket);

                p.WriteUInt64(pi.ui.pang);
                p.WriteUInt64(pi.cookie);


                return p.GetBytes;
            }
        }

        public static byte[] pacote144(int option = 0)
        {
            var p = new PangyaBinaryWriter(0x144);
            p.WriteByte((byte)option);

            return p.GetBytes;
        }

        public static byte[] pacote09A(int ulCapability)
        {        // UPDATE ON GAME
            var p = new PangyaBinaryWriter(0x9A);

            p.WriteInt32(ulCapability);
            return p.GetBytes;
        }

        //tested, melhorar com tempo@@@@
        public static bool pacote048(ref PangyaBinaryWriter p, Player _session, List<PlayerRoomInfoEx> v_element, int option = 0)
        {
            TPlayerRoom_Action opt = (TPlayerRoom_Action)(option & 0xFF);

            Debug.WriteLine($"pacote048 => enum: {opt}, code: {option & 0xFF}, code2: {option & 0x100}");

            try
            {

                if ((option & 0xFF) == 2)
                { // exit player
                    p.init_plain(0x48);
                    p.WriteSByte((sbyte)option);
                    p.WriteInt16(-1);
                    p.WriteInt32(_session.m_oid);
                    return true;
                }
                else if ((option & 0xFF) == 7)
                {
                    int elementSize = (option & 0x100) != 0 ? Marshal.SizeOf(new PlayerRoomInfo()) : Marshal.SizeOf(new PlayerRoomInfoEx());
                    int maxPacket = Marshal.SizeOf(new PlayerRoomInfoEx());
                    int total = v_element.Count;
                    int por_packet = (maxPacket - 100 > elementSize) ? (maxPacket - 100) / elementSize : 1;

                    int index = 0;

                    while (index < total)
                    {
                        p.init_plain(0x48);
                        p.WriteSByte((sbyte)option);
                        p.WriteInt16(-1);

                        if ((option & 0xFF) == 0 || (option & 0xFF) == 5)
                            p.WriteSByte((sbyte)Math.Min(por_packet, total - index));
                        else if ((option & 0xFF) == 7)
                            p.WriteSByte((sbyte)total);
                        else if ((option & 0xFF) == 3 || (option & 0xFF) == 3)
                        {
                            p.WriteInt32(_session.m_oid);
                        }
                        for (int i = 0; i < por_packet && index < total; i++, index++)
                        {
                            var playerRoom = v_element[index];
                            if (elementSize == 348)
                            {
                                p.WriteBytes(playerRoom.ToArray());
                            }
                            else
                            {
                                p.WriteBytes(playerRoom.ToArrayEx());
                            }
                        }

                        p.WriteByte(0);     // Final list de PlayerRoomInfo
                        //p.WriteFile($"pacote048-{option}-{_session.m_pi.id}-{DateTime.Now.Ticks}.hex");
                        session_send(p, _session, 1);//-> MAKE_END_SPLIT_PACKET
                    }
                    return true;
                }
                else
                {
                    int elementSize = (option & 0x100) != 0 ? Marshal.SizeOf(new PlayerRoomInfo()) : Marshal.SizeOf(new PlayerRoomInfoEx());
                    int elements = v_element.Count;
                    int totalSize = elements * elementSize;

                    try
                    {
                        if (totalSize < MAX_BUFFER_PACKET - 100)//-> MAKE_END_SPLIT_PACKET nao tem, so no else, OK?
                        {
                            p.init_plain(0x48);
                            p.WriteSByte((sbyte)option);
                            p.WriteInt16(-1);

                            if ((option & 0xFF) == 0 || (option & 0xFF) == 5)
                                p.WriteByte((byte)elements);
                            else if ((option & 0xFF) == 3 || (option & 0xFF) == 3)
                            {
                                p.WriteInt32(_session.m_oid);
                            }

                            foreach (var playerRoom in v_element)
                            {

                                if (elementSize == 348)
                                {
                                    p.WriteBytes(playerRoom.ToArray());
                                }
                                else
                                {
                                    p.WriteBytes(playerRoom.ToArrayEx());
                                }
                            }

                            p.WriteByte(0);
                            //p.WriteFile($"pacote048-{option}-{_session.m_pi.id}-{DateTime.Now.Ticks}.hex");
                            return true;
                        }
                        else
                        {
                            int total = elements;
                            int por_packet = ((MAX_BUFFER_PACKET - 100) > elementSize) ? (MAX_BUFFER_PACKET - 100) / elementSize : 1;

                            int index = 0;

                            while (index < total)
                            {
                                p.init_plain(0x48);

                                if ((option & 0xFF) == 0 && index != 0)
                                    p.WriteByte(5); // append players
                                else
                                    p.WriteByte((byte)option);

                                p.WriteInt16(-1);

                                if ((option & 0xFF) == 0 || (option & 0xFF) == 5)
                                    p.WriteSByte((sbyte)Math.Min(por_packet, total - index));
                                else if ((option & 0xFF) == 3)
                                {
                                    elementSize = 348;
                                    p.WriteInt32(_session.m_oid);
                                }

                                for (int i = 0; i < por_packet && index < total; i++, index++)
                                {
                                    var playerRoom = v_element[index];
                                    if (elementSize == 348)
                                    {
                                        p.WriteBytes(playerRoom.ToArray());
                                    }
                                    else
                                    {
                                        p.WriteBytes(playerRoom.ToArrayEx());
                                    }
                                }

                                p.WriteByte(0); // Final list de PlayerRoomInfo
                                //p.WriteFile($"pacote048-{option}-{_session.m_pi.id}-{DateTime.Now.Ticks}.hex");
                                session_send(p, _session, 1);//-> MAKE_END_SPLIT_PACKET
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[pacote048][Fatal] " + ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[pacote048][Fatal] " + ex);
            }
            return false;
        }

        public static PangyaBinaryWriter pacote04A(RoomInfoEx _ri, short option)
        {
            var p = new PangyaBinaryWriter();
            p.init_plain(0x4A);

            p.WriteUInt16(_ri.numero);      // pode ser valor constante da sala ou o número, ainda não descobri, sempre passa -1 des vezes que vi
            // Tem que ser o tipo_show, por que ele é o que o cliente quer,
            // o tipo(real) só server conhece para poder fazer o jogo direito 
            p.WriteBytes(_ri.ToArrayEx());
            return p;
        }

        public static byte[] pacote049(Room _room, TGAME_CREATE_RESULT option = 0)
        {
            try
            {
                if (option !=  TGAME_CREATE_RESULT.CREATE_GAME_RESULT_SUCCESS && _room == null)
                    throw new exception("Error _room is null. EM packet_func::pacote049()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV, 3, 0));

                var p = new PangyaBinaryWriter();

                p.init_plain(0x49);
                if (option == 0)//sucess
                {
                    p.WriteInt16((short)option);
                    p.WriteBytes(_room.getInfo().ToArray());
                }
                else
                    p.WriteByte((byte)option);//write error code packet 
                return p.GetBytes;
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public static byte[] pacote225(DailyQuestInfoUser _dq,
            List<RemoveDailyQuestUser> _delete_quest,
            int option = 0)
        {
            var p = new PangyaBinaryWriter();
            p.init_plain(0x225);

            p.WriteInt32(option);

            if (option == 0)
            {
                // Convert to UTC send to client
                p.WriteUInt32((uint)UtilTime.TzLocalUnixToUnixUTC(_dq.current_date));//data em unix
                p.WriteUInt32((uint)UtilTime.TzLocalUnixToUnixUTC(_dq.accept_date));//data em unix

                p.WriteUInt32(_dq.count);
                p.WriteUInt32(_dq._typeid); //a quest sao 3

                p.WriteInt32(_delete_quest.Count);
                foreach (RemoveDailyQuestUser it in _delete_quest)//quest of delete
                {
                    p.WriteInt32(it.id);
                }
            }
            return p.GetBytes;
        }

        public static byte[] pacote226(List<AchievementInfoEx> v_element, int option = 0)
        {
            var p = new PangyaBinaryWriter();
            p.init_plain(0x226);

            p.WriteInt32(option);

            if (option == 0)
            {
                if (v_element.Count > 0)
                {
                    CounterItemInfo cii = null;

                    p.WriteInt32(v_element.Count);

                    foreach (AchievementInfoEx i in v_element)
                    {
                        p.WriteByte(i.active);
                        p.WriteUInt32(i._typeid);
                        p.WriteInt32(i.id);
                        p.WriteInt32(i.status);
                        p.WriteUInt32((uint)i.v_qsi.Count);
                        foreach (var ii in i.v_qsi)
                        {
                            p.WriteUInt32(ii._typeid);

                            if (ii.counter_item_id > 0 && (cii = i.findCounterItemById(ii.counter_item_id)) != null)
                            {
                                p.WriteUInt32(cii._typeid);
                                p.WriteInt32(cii.id);
                            }
                            else // não tem o counter id e nem o typeid
                            {
                                p.WriteZeroByte(8);
                            }

                            p.WriteUInt32(ii.clear_date_unix);
                        }
                    }
                }
            }
            else
            {
                p.WriteInt32(0);
            }

            return p.GetBytes;
        }

        public static byte[] pacote227(List<AchievementInfoEx> v_element,
            int option = 0)
        {

            var p = new PangyaBinaryWriter();
            p.init_plain(0x227);

            p.WriteInt32(option);

            if (v_element.Count > 0)
            {

                p.WriteInt32(v_element.Count);

                foreach (var el in v_element)
                {
                    p.WriteInt32(el.id);
                }
            }
            else
            {
                p.WriteInt32(0);
            }

            return p.GetBytes;
        }

        public static byte[] pacote228(List<AchievementInfoEx> v_element, int option = 0)
        {

            var p = new PangyaBinaryWriter();
            p.init_plain(0x228);

            p.WriteInt32(option);

            if (option == 0)
            {
                if (v_element.Count > 0)
                {
                    p.WriteInt32(v_element.Count);

                    foreach (var el in v_element)
                    {
                        p.WriteInt32(el.id);
                    }
                }
            }

            return p.GetBytes;
        }



        public static byte[] pacote04C(int option)
        {
            var p = new PangyaBinaryWriter();
            p.init_plain(0x4C);

            p.WriteInt16((short)option);
            return p.GetBytes;
        }

        public static byte[] pacote0AA(Player _session, List<stItem> v_item)
        {
            if (_session == null || !_session.getState())
                throw new exception("Error player nao esta conectado. Em packet_func::pacote0AA()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV, 50, 0));

            var p = new PangyaBinaryWriter();
            if (v_item.Count() > 0)
            {
                p.init_plain(0xAA);

                p.WriteUInt16((ushort)v_item.Count()); // Count, ele só manda de 1 msm não manda todos, não sei por que

                for (var i = 0; i < v_item.Count(); ++i)
                {

                    p.WriteUInt32(v_item[i]._typeid);
                    p.WriteInt32(v_item[i].id);
                    p.WriteUInt16(v_item[i].STDA_C_ITEM_TIME);
                    p.WriteByte(v_item[i].flag_time);
                    p.WriteUInt16((ushort)v_item[i].stat.qntd_dep);
                    p.WriteTime(v_item[i].date.date.sysDate[1]);
                    p.WriteStr(v_item[i].ucc.IDX, 9);

                    // Aqui é a reflexão desse pacote, usa no ticket report
                    if (v_item[i]._typeid == 0x1A000042)
                    {
                        p.WriteUInt16(v_item[i].STDA_C_ITEM_TICKET_REPORT_ID_HIGH);
                        p.WriteUInt16(v_item[i].STDA_C_ITEM_TICKET_REPORT_ID_LOW);

                        p.WriteTime(v_item[i].date.date.sysDate[1]);
                    }
                }

                p.WriteUInt64(_session.m_pi.ui.pang);
                p.WriteUInt64(_session.m_pi.cookie);
            }
            return p.GetBytes;
        }

        public static PangyaBinaryWriter pacote196(Player _session, StateCharacterLounge stateCharacterLounge)
        {
            var p = new PangyaBinaryWriter((ushort)0x196);

            p.WriteInt32(_session.m_oid);//coloquei 1 pra testar

            p.WriteBytes(stateCharacterLounge.ToArray());

            return p;
        }

        public static PangyaBinaryWriter pacote26D(int _unix_end_date)
        {
            var p = new PangyaBinaryWriter((ushort)0x26D);

            p.WriteInt32(_unix_end_date);

            return p;
        }



        // Metôdos de auxílio de criação de pacotes 
        public static void channel_broadcast(Channel _channel, byte[] p, int __DEBUG = 1)
        {
            try
            {
                var channel_session = _channel.getSessions();
                for (var i = 0; i < channel_session.Count; ++i)
                    MAKE_SEND_BUFFER(p, channel_session[i]);
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[channel_broadcast(byte[])] Exception: " + e.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public static void channel_broadcast(Channel _channel, PangyaBinaryWriter p, int __DEBUG = 1)
        {
            try
            {
                var channel_session = _channel.getSessions();
                for (var i = 0; i < channel_session.Count; ++i)
                    MAKE_SEND_BUFFER(p.GetBytes, (Player)channel_session[i]);
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[channel_broadcast(byte[])] Exception: " + e.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public static void channel_broadcast(Channel _channel, List<byte[]> v_p, int __DEBUG = 1)
        {
            try
            {
                for (var i = 0; i < v_p.Count; ++i)
                {
                    if (v_p[i] != null)
                    {
                        var channel_session = _channel.getSessions();
                        for (int ii = 0; ii < channel_session.Count; ii++)
                            MAKE_SEND_BUFFER(v_p[i], channel_session[ii]);
                    }
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[channel_broadcast(List<byte[]>)] Exception: " + e.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public static void channel_broadcast(Channel _channel, List<PangyaBinaryWriter> v_p, int __DEBUG = 1)
        {
            try
            {
                for (int i = 0; i < v_p.Count; ++i)
                {
                    var writer = v_p[i];
                    if (writer != null)
                    {
                        var channel_session = _channel.getSessions();
                        for (int ii = 0; ii < channel_session.Count; ii++)
                            MAKE_SEND_BUFFER(writer.GetBytes, channel_session[ii]);
                    }
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[channel_broadcast(List<PangyaBinaryWriter>)] Exception: " + e.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public static void lobby_broadcast(Channel _channel, byte[] p, int __DEBUG = 1)
        {
            try
            {
                var channel_session = _channel.getSessions();
                for (var i = 0; i < channel_session.Count; ++i)
                {
                    if (channel_session[i].m_pi.mi.sala_numero == ushort.MaxValue)
                        MAKE_SEND_BUFFER(p, channel_session[i]);
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[lobby_broadcast] Exception: " + e.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public static void room_broadcast(Room _room, PangyaBinaryWriter p, int __DEBUG = 1)
        {
            try
            {
                var room_session = _room.getSessions(null, false/*without invited*/);
                for (var i = 0; i < room_session.Count; ++i)
                {
                    if (room_session[i] != null)
                    {
                        MAKE_SEND_BUFFER(p.GetBytes, room_session[i]);
                    }
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[room_broadcast(writer)] Exception: " + e.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public static void room_broadcast(Room _room, byte[] p, int __DEBUG = 1)
        {
            if (_room == null)
            {
                return;
            }
            if (p.Length == 0)
            {
                return;
            }

            try
            {
                var room_session = _room.getSessions(null, false/*without invited*/);
                for (var i = 0; i < room_session.Count; ++i)
                {
                    if (room_session[i] != null)
                    {
                        MAKE_SEND_BUFFER(p, room_session[i]);
                    }
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[room_broadcast(writer)] Exception: " + e.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public static void room_broadcast(Room _room, List<PangyaBinaryWriter> v_p, int __DEBUG = 1)
        {
            try
            {
                for (var i = 0; i < v_p.Count; ++i)
                {
                    if (v_p[i] != null)
                    {
                        var room_session = _room.getSessions();
                        for (var ii = 0; ii < room_session.Count; ++ii)
                            MAKE_SEND_BUFFER(v_p[i].GetBytes, room_session[ii]);
                    }
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[room_broadcast(List)] Exception: " + e.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public static void game_broadcast(Game.Base.GameBase _game, byte[] p, int __DEBUG = 1)
        {
            try
            {
                var game_session = _game.getSessions();
                for (var i = 0; i < game_session.Count; ++i)
                    MAKE_SEND_BUFFER(p, game_session[i]);
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[game_broadcast(byte[])] Exception: " + e.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public static void game_broadcast(Game.Base.GameBase _game, PangyaBinaryWriter p, int __DEBUG = 1)
        {
            try
            {
                var game_session = _game.getSessions();
                for (var i = 0; i < game_session.Count; ++i)
                    MAKE_SEND_BUFFER(p.GetBytes, game_session[i]);
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[game_broadcast(byte[])] Exception: " + e.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public static void game_broadcast(Game.Base.GameBase _game, List<PangyaBinaryWriter> v_p, int __DEBUG = 1)
        {
            try
            {
                for (var i = 0; i < v_p.Count; ++i)
                {
                    if (v_p[i] != null)
                    {
                        var game_session = _game.getSessions();
                        for (var ii = 0; ii < game_session.Count; ++ii)
                            MAKE_SEND_BUFFER(v_p[i].GetBytes, game_session[ii]);
                    }
                    else
                    {
                        _smp.message_pool.getInstance().push(new message("Error byte[] p is null, packet_func::game_broadcast()", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[game_broadcast(List)] Exception: " + e.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public static void vector_send(PangyaBinaryWriter _p, List<Player> _v_s, int __DEBUG = 1)
        {
            try
            {
                foreach (var el in _v_s)
                    MAKE_SEND_BUFFER(_p.GetBytes, el);
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[vector_send(Session)] Exception: " + e.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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

        public static void session_send(PangyaBinaryWriter p, Session s, int __DEBUG = 1)
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

        public static void session_send(byte[] p, Session s, int __DEBUG = 1)
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


        public static Player getPlayer(Session _session)
        { 
            if (_session != null && _session.getConnectTime() == 1)
            {
                return (Player)(_session);
            }
            else
            {
                // Em vez de null, lançamos uma exceção detalhada
                throw new exception($"[Player::getPlayer][Error] Session é inválida ou não está conectada. " +
                    (_session != null ? $"UID: {_session.getUID()}" : "Session is NULL"),
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.SESSION, 1, 0));
            }
        }
    }
}
