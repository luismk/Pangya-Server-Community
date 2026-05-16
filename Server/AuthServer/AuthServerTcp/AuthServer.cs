using Pangya_AuthServer.Models;
using Pangya_AuthServer.PacketFunc;
using Pangya_AuthServer.PangyaEnums;
using Pangya_AuthServer.Repository;
using Pangya_AuthServer.Session;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Network.PangyaServer;
using PangyaAPI.Network.PangyaUnit;
using PangyaAPI.Network.PangyaUtil;
using PangyaAPI.Network.Repository;
using PangyaAPI.SQL; 
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Pangya_AuthServer.AuthServerTcp
{
    public class AuthServer : unit
    {
        static player_manager m_player_manager = new player_manager();
        DateTime m_guild_ranking_time;
        public AuthServer() : base(m_player_manager)
        {
            // Inicializa config do Game Server
            config_init();
            // init Request Client packets
            init_Packets(); 

            // Initialized complete
            m_state = ServerState.Initialized;

        }

        public override bool CheckPacket(PangyaAPI.Network.PangyaSession.Session _session, packet packet, int opt = 0)
        {
            var player = (Player)_session;
            var packetId = packet.Id;
            var uid = player.m_pi.uid;


            switch (opt)
            {
                case 1:
                    // Verifica se o valor de packetId é válido no enum PacketIDClient
                    if (Enum.IsDefined(typeof(PacketIDClient), (PacketIDClient)packetId))
                    { 
                        return true;
                    }
                    else// nao tem no PacketIDClient
                    {
                        _smp.message_pool.getInstance().push(new message($"[{GetType().Name}::CheckPacket][Info]: PLAYER[UID: {player.m_pi.uid}, CGPID: 0x{packet.Id:X}]", type_msg.CL_ONLY_CONSOLE));
                        return true;
                    }
                default:
                     
                        Debug.WriteLine($"[{GetType().Name}::CheckPacket][Info]: PLAYER[UID: {player.m_pi.uid}, SGPID: 0x{packet.Id:X}]");
                        return true; 
            }
        }

        public override void onDisconnected(PangyaAPI.Network.PangyaSession.Session _session)
        {

            if (_session == null)
                throw new exception("[AuthServer::onDisconnect][Error] _session is nullptr.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 60, 0));

            Player p = (Player)_session;

            _smp.message_pool.getInstance().push(new message("[AuthServer::onDisconnected][Log] PLAYER[ID: " + (p.m_pi.id) + ", UID: " + (p.m_pi.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
            // Aqui não faz nada, no login server por enquanto

        }

        public override void OnHeartBeat()
        {
            var local = DateTime.Now;

            try
            {


                // Check Commands
                CmdCommandInfo cmd_ci = new CmdCommandInfo(); // Waiter

                snmdb.NormalManagerDB.getInstance().add(0,
                    cmd_ci, null, null);

                if (cmd_ci.getException().getCodeError() != 0)
                {
                    throw cmd_ci.getException();
                }

                translateCmd(cmd_ci.getInfo());

                // Guild Ranking Update

                // Verifica se já pegou a hora do Guild Ranking se não pega no banco de dados
                if (m_guild_ranking_time.Year == 0)
                {

                    CmdGuildRankingUpdateTime cmd_grut = new CmdGuildRankingUpdateTime(); // Waiter

                    snmdb.NormalManagerDB.getInstance().add(0,
                        cmd_grut, null, null);


                    if (cmd_grut.getException().getCodeError() != 0)
                    {
                        throw cmd_grut.getException();
                    }

                    m_guild_ranking_time = cmd_grut.getTime();

                    // Log
                }


                // Verifica se é um novo dia e atualiza o Guild Ranking
                if (m_guild_ranking_time.Year < local.Year
                    || m_guild_ranking_time.Month < local.Month
                    || m_guild_ranking_time.Day < local.Day)
                {
                    snmdb.NormalManagerDB.getInstance().add(3,
                        new CmdUpdateGuildRanking(),
                        SQLDBResponse,
                        this);

                    // atualiza a hora do ranking do server
                    m_guild_ranking_time = DateTime.Now;
                }

            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[AuthServer::onHeartBeat][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return;
        }

        public override void OnStart()
        {
            Console.Title = $"Auth Server - P: {m_si.curr_user}";
            m_state = ServerState.Initialized;
        }

        public void translateCmd(List<CommandInfo> _v_ci)
        {

            try
            {

                foreach (var el in _v_ci)
                {

                    // Ainda n�o chegou na date reservada, pula esse comando
                    if (DateTime.Now < el.reserveDate)
                    {
                        continue;
                    }

                    switch ((COMMAND_ID)el.id)
                    {
                        case COMMAND_ID.BROADCAST_NOTICE:
                            {
                                CmdNoticeInfo cmd_ni = new CmdNoticeInfo(el.idx, true); // Waiter

                                snmdb.NormalManagerDB.getInstance().add(0,
                                    cmd_ni, null, null);

                                if (cmd_ni.getException().getCodeError() != 0)
                                {
                                    _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Error] " + cmd_ni.getException().getFullMessageError(), type_msg.CL_ONLY_CONSOLE));

                                    // Trata os outros comandos
                                    continue;
                                }

                                var msg = cmd_ni.getInfo();

                                if (msg.Length == 0)
                                {
                                    _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Error] msg is empty. Comando[" + el.toString() + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    // Trata os outros comandos
                                    continue;
                                }

                                // Send Msg
                                var p = new PangyaBinaryWriter((ushort)0x03);

                                p.WriteString(msg);
                                var s = (Player)m_player_manager.findSessionByUID((el.target));
                                if (s == null)
                                {
                                    var v_s = m_player_manager.findPlayerByType((el.target));
                                    if (!v_s.empty())
                                    {
                                        _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Log] Send " + (("Broadcast Notice[MESSAGE=" + msg + "]")) + " For Server[UID=" + Convert.ToString((el.target)) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                        packet_func.vector_send(p,
                                            v_s, 1);
                                    }
                                    else
                                    {
                                        _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Error] Nao encontrou o SERVER[UID/TIPO=" + Convert.ToString(el.target) + "], para enviar o comando de " + "Broadcast Notice[MESSAGE=" + msg + "]" + " para ele.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                    }
                                }
                                else
                                {
                                    _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Log] Send " + (("Broadcast Notice[MESSAGE=" + msg + "]")) + " For Server[UID=" + Convert.ToString(el.target) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                    packet_func.session_send(p,
                                        s, 1);
                                }
                                break;
                            }
                        case COMMAND_ID.BROADCAST_TICKER:
                            {

                                CmdTickerInfo cmd_ti = new CmdTickerInfo(el.idx, true); // Waiter

                                snmdb.NormalManagerDB.getInstance().add(0,
                                    cmd_ti, null, null);

                                if (cmd_ti.getException().getCodeError() != 0)
                                {
                                    _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Error] " + cmd_ti.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    // Trata os outros comandos
                                    continue;
                                }

                                var ti = cmd_ti.getInfo();

                                if (!ti.isValid())
                                {
                                    _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Error] Ticker Info is invalid [MSG=" + ti.msg + ", NICK=" + ti.nick + "]. Comando[" + el.toString() + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    // Trata os outros comandos
                                    continue;
                                }

                                // Send Ticker
                                var p = new PangyaBinaryWriter((ushort)0x04);

                                p.WriteString(ti.nick);
                                p.WriteString(ti.msg);

                                // Find Server By UID, se n�o Encontrar, procura por TIPO
                                var s = (Player)m_player_manager.findSessionByUID(el.target);

                                if (s == null)
                                {

                                    // Exclui do vector de server para enviar, o server que gerou o ticker, ele nao precisa que envie de novo para ele
                                    var v_s = m_player_manager.findPlayerByTypeExcludeUID(el.target, el.arg[1]);

                                    if (!v_s.empty())
                                    {

                                        // Log
                                        _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Log] Send Ticker[MESSAGE=" + ti.msg + ", NICK=" + ti.nick + "] For Server[UID=" + Convert.ToString(el.target) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                        packet_func.vector_send(p,
                                            v_s, 1);

                                    }
                                    else
                                    {
                                        _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Error] Nao encontrou o SERVER[UID/TIPO=" + Convert.ToString(el.target) + "], para enviar o comando de Ticker[MESSAGE=" + ti.msg + ", NICK=" + ti.nick + "] para ele.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                    }

                                }
                                else
                                {

                                    // Log 
                                    _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Log] Send Ticker[MESSAGE=" + ti.msg + ", NICK=" + ti.nick + "] For Server[UID=" + Convert.ToString(el.target) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    packet_func.session_send(p,
                                        s, 1);
                                }

                                //FIND_TARGET_AND_SEND(el.target, p, "Ticker[MESSAGE=" + ti.msg + ", NICK=" + ti.nick + "]");

                                break;
                            }
                        case COMMAND_ID.BROADCAST_CUBE_WIN:
                            {
                                CmdNoticeInfo cmd_ni = new CmdNoticeInfo(el.idx, true); // Waiter

                                snmdb.NormalManagerDB.getInstance().add(0,
                                    cmd_ni, null, null);

                                if (cmd_ni.getException().getCodeError() != 0)
                                {
                                    _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Error] " + cmd_ni.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    // Trata os outros comandos
                                    continue;
                                }

                                var msg = cmd_ni.getInfo();

                                if (msg.Length == 0)
                                {
                                    _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Error] msg is empty. Comando[" + el.toString() + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    // Trata os outros comandos
                                    continue;
                                }

                                // Send Broadcast Notice Cube Win Rare
                                var p = new PangyaBinaryWriter((ushort)0x05);

                                p.WriteUInt32(el.arg[1]); // Option
                                p.WriteString(msg);
                                var s = (Player)m_player_manager.findSessionByUID((el.target));
                                if (s == null)
                                {
                                    var v_s = m_player_manager.findPlayerByType((el.target));
                                    if (!v_s.empty())
                                    {
                                        _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Log] Send " + (("Broadcast Notice Cube Win Rare[MESSAGE=" + msg + ", OPTION=" + Convert.ToString(el.arg[1]) + "]")) + " For Server[UID=" + Convert.ToString((el.target)) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                        packet_func.vector_send(p,
                                            v_s, 1);
                                    }
                                    else
                                    {
                                        _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Error] Nao encontrou o SERVER[UID/TIPO=" + Convert.ToString((el.target)) + "], para enviar o comando de " + (("Broadcast Notice Cube Win Rare[MESSAGE=" + msg + ", OPTION=" + Convert.ToString(el.arg[1]) + "]")) + " para ele.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                    }
                                }
                                else
                                {
                                    _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Log] Send " + (("Broadcast Notice Cube Win Rare[MESSAGE=" + msg + ", OPTION=" + Convert.ToString(el.arg[1]) + "]")) + " For Server[UID=" + Convert.ToString(el.target) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                    packet_func.session_send(p,
                                        s, 1);
                                }
                                break;
                            }
                        case COMMAND_ID.NEW_ITEM_NOTICE:
                            {

                                // Update Command on DB
                                el.valid = 0;

                                snmdb.NormalManagerDB.getInstance().add(1,
                                    new CmdUpdateCommand(el),
                                    SQLDBResponse,
                                    this);

                                // Send New Mail Arrived in MailBox
                                var p = new PangyaBinaryWriter((ushort)0x08);

                                p.WriteUInt32(el.arg[0]); // Player UID
                                p.WriteUInt32(el.arg[1]); // Msg Id

                                var s = (Player)m_player_manager.findSessionByUID((el.target));
                                if (s == null)
                                {
                                    var v_s = m_player_manager.findPlayerByType((el.target));
                                    if (!v_s.empty())
                                    {
                                        _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Log] Send " + (("New Mail Arrived In MailBox[PLAYER=" + Convert.ToString(el.arg[0]) + ", MSG_ID=" + Convert.ToString(el.arg[1]) + "]")) + " For Server[UID=" + Convert.ToString((el.target)) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                        packet_func.vector_send(p,
                                            v_s, 1);
                                    }
                                    else
                                    {
                                        _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Error] Nao encontrou o SERVER[UID/TIPO=" + Convert.ToString((el.target)) + "], para enviar o comando de " + (("New Mail Arrived In MailBox[PLAYER=" + Convert.ToString(el.arg[0]) + ", MSG_ID=" + Convert.ToString(el.arg[1]) + "]")) + " para ele.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                    }
                                }
                                else
                                {
                                    _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Log] Send " + (("New Mail Arrived In MailBox[PLAYER=" + Convert.ToString(el.arg[0]) + ", MSG_ID=" + Convert.ToString(el.arg[1]) + "]")) + " For Server[UID=" + Convert.ToString(el.target) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                    packet_func.session_send(p,
                                        s, 1);
                                }

                                break;
                            }
                        case COMMAND_ID.NEW_RATE:
                            {
                                // Update Command on DB
                                el.valid = 0;

                                snmdb.NormalManagerDB.getInstance().add(1,
                                    new CmdUpdateCommand(el),
                                    SQLDBResponse,
                                    this);

                                // Send New Rate to Server
                                var p = new PangyaBinaryWriter((ushort)0x09);

                                p.WriteUInt32(el.arg[0]); // Tipo Rate
                                p.WriteUInt32(el.arg[1]); // Quantidade (amount)
                                var s = (Player)m_player_manager.findSessionByUID((el.target));
                                if (s == null)
                                {
                                    var v_s = m_player_manager.findPlayerByType((el.target));
                                    if (!v_s.empty())
                                    {
                                        _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Log] Send " + (("New Rate[TIPO=" + Convert.ToString(el.arg[0]) + ", QNTD=" + Convert.ToString(el.arg[1]) + "]")) + " For Server[UID=" + Convert.ToString((el.target)) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                        packet_func.vector_send(p,
                                            v_s, 1);
                                    }
                                    else
                                    {
                                        _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Error] Nao encontrou o SERVER[UID/TIPO=" + Convert.ToString((el.target)) + "], para enviar o comando de " + (("New Rate[TIPO=" + Convert.ToString(el.arg[0]) + ", QNTD=" + Convert.ToString(el.arg[1]) + "]")) + " para ele.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                    }
                                }
                                else
                                {
                                    _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Log] Send " + (("New Rate[TIPO=" + Convert.ToString(el.arg[0]) + ", QNTD=" + Convert.ToString(el.arg[1]) + "]")) + " For Server[UID=" + Convert.ToString(el.target) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                    packet_func.session_send(p,
                                        s, 1);
                                }

                                break;
                            }
                        case COMMAND_ID.ADM_KICK_FROM_WEBSITE:
                            {
                                // Update Command on DB
                                el.valid = 0;

                                snmdb.NormalManagerDB.getInstance().add(1,
                                    new CmdUpdateCommand(el),
                                    SQLDBResponse,
                                    this);

                                // Send Disconnect Player
                                var p = new PangyaBinaryWriter((ushort)0x06);

                                p.WriteUInt32(el.arg[0]); // Playe UID
                                p.WriteInt32(m_si.uid); // Quem pediu para desconectar o jogador
                                p.WriteByte(1); // 1 Ativado, Flag que forca a desconecta mesmo se o server tiver outras regras
                                var s = (Player)m_player_manager.findSessionByUID((el.target));
                                if (s == null)
                                {
                                    var v_s = m_player_manager.findPlayerByType((el.target));
                                    if (!v_s.empty())
                                    {
                                        _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Log] Send " + (("ADM Website Disconnect User[UID=" + Convert.ToString(el.arg[0]) + "]")) + " For Server[UID=" + Convert.ToString((el.target)) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                        packet_func.vector_send(p,
                                            v_s, 1);
                                    }
                                    else
                                    {
                                        _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Error] Nao encontrou o SERVER[UID/TIPO=" + Convert.ToString((el.target)) + "], para enviar o comando de " + (("ADM Website Disconnect User[UID=" + Convert.ToString(el.arg[0]) + "]")) + " para ele.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                    }
                                }
                                else
                                {
                                    _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Log] Send " + (("ADM Website Disconnect User[UID=" + Convert.ToString(el.arg[0]) + "]")) + " For Server[UID=" + Convert.ToString(el.target) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                    packet_func.session_send(p,
                                        s, 1);
                                }

                                break;
                            }
                        case COMMAND_ID.SHUTDOWN:
                            {
                                CmdShutdownInfo cmd_si = new CmdShutdownInfo(el.idx, true); // Waiter

                                snmdb.NormalManagerDB.getInstance().add(0,
                                    cmd_si, null, null);

                                if (cmd_si.getException().getCodeError() != 0)
                                {
                                    _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Error] " + cmd_si.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    // Trata os outros comandos
                                    continue;
                                }

                                var time_sec = cmd_si.getInfo();

                                // Send Time Shutdown
                                var p = new PangyaBinaryWriter((ushort)0x02);

                                p.WriteInt32(time_sec);

                                // Verifica se � o Auth Server, se for envia para todos o tempo, e desliga o Auth Server Tamb�m
                                if (el.target == m_si.uid || el.target == m_si.tipo)
                                {

                                    packet_func.vector_send(p,
                                        m_player_manager.getAllPlayer(),
                                        1);

                                    _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Log] Comando de Desligar o Auth Server. Desligando o Server em " + Convert.ToString(time_sec) + " segundos", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    if (time_sec <= 0)
                                    {
                                        Thread.Sleep(5000); // Espera 5 segundos para da tempo de enviar para todos os server conectados
                                    }

                                    // Shutdown With Time
                                    //   shutdown_time(time_sec);

                                }
                                else
                                {
                                    var s = (Player)m_player_manager.findSessionByUID((el.target));
                                    if (s == null)
                                    {
                                        var v_s = m_player_manager.findPlayerByType((el.target));
                                        if (!v_s.empty())
                                        {
                                            _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Log] Send " + (("Shutdown[TIME=" + Convert.ToString(time_sec) + "]")) + " For Server[UID=" + Convert.ToString((el.target)) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                            packet_func.vector_send(p,
                                                v_s, 1);
                                        }
                                        else
                                        {
                                            _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Error] Nao encontrou o SERVER[UID/TIPO=" + Convert.ToString((el.target)) + "], para enviar o comando de " + (("Shutdown[TIME=" + Convert.ToString(time_sec) + "]")) + " para ele.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                        }
                                    }
                                    else
                                    {
                                        _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Log] Send " + (("Shutdown[TIME=" + Convert.ToString(time_sec) + "]")) + " For Server[UID=" + Convert.ToString(el.target) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                        packet_func.session_send(p,
                                            s, 1);
                                    }
                                }
                                ;

                                break;
                            }
                        case COMMAND_ID.RELOAD_SYSTEM:
                            {
                                // Update Command on DB
                                el.valid = 0;

                                snmdb.NormalManagerDB.getInstance().add(1,
                                    new CmdUpdateCommand(el),
                                    SQLDBResponse,
                                    this);

                                // Send Disconnect Player
                                var p = new PangyaBinaryWriter((ushort)0x0A);

                                p.WriteUInt32(el.arg[0]);

                                {
                                    var s = (Player)m_player_manager.findSessionByUID((el.target));
                                    if (s == null)
                                    {
                                        var v_s = m_player_manager.findPlayerByType((el.target));
                                        if (!v_s.empty())
                                        {
                                            _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Log] Send " + (("Reload System[TYPE=" + Convert.ToString(el.arg[0]) + "]")) + " For Server[UID=" + Convert.ToString((el.target)) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                            packet_func.vector_send(p,
                                                v_s, 1);
                                        }
                                        else
                                        {
                                            _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Error] Nao encontrou o SERVER[UID/TIPO=" + Convert.ToString((el.target)) + "], para enviar o comando de " + (("Reload System[TYPE=" + Convert.ToString(el.arg[0]) + "]")) + " para ele.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                        }
                                    }
                                    else
                                    {
                                        _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Log] Send " + (("Reload System[TYPE=" + Convert.ToString(el.arg[0]) + "]")) + " For Server[UID=" + Convert.ToString(el.target) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                        packet_func.session_send(p,
                                            s, 1);
                                    }
                                }
                                ;

                                break;

                            } // END COMMAND_ID::RELOAD_SYSTEM
                        default:
#if DEBUG
                            _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Log] Comando[" + el.toString() + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
#else
					_smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][Log] Comando[" + el.toString() + "]", type_msg.CL_ONLY_FILE_LOG));
#endif // _DEBUG
                            break;
                    } // END SWITCH
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[AuthServer::translateCmd][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        protected override void onAcceptCompleted(PangyaAPI.Network.PangyaSession.Session _session)
        {
            try
            {
                packet _packet = new packet(0x00);	// Tipo Packet Auth Server initial packet no compress e no crypt

                _packet.AddUInt32(_session.m_key); // key); 	// key
                _packet.AddInt32(m_si.uid); // Server UID

                _packet.makeRaw();

                var mb = _packet.getBuffer();
                _session.requestSendBuffer(mb, true);
            }
            catch (Exception ex)
            {
                _smp.message_pool.getInstance().push(new message(
              $"[AuthServer.onAcceptCompleted][ErrorSt] {ex.Message}\nStack Trace: {ex.StackTrace}",
              type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        /// <summary>
        /// init packet to call !
        /// </summary>
        protected void init_Packets()
        {

            packet_func.funcs.addPacketCall(0x01,
                packet_func.packet001, this);
            packet_func.funcs.addPacketCall(0x02,
                packet_func.packet002, this);
            packet_func.funcs.addPacketCall(0x03,
                packet_func.packet003, this);
            packet_func.funcs.addPacketCall(0x04,
                packet_func.packet004, this);
            packet_func.funcs.addPacketCall(0x05,
                packet_func.packet005, this);
            packet_func.funcs.addPacketCall(0x06,
                packet_func.packet006, this);
            packet_func.funcs.addPacketCall(0x07,
                packet_func.packet007, this);
            //keep live
            packet_func.funcs.addPacketCall(0xFF, packet_func.packet0FF, this);

            packet_func.funcs_sv.addPacketCall(0x00,
                packet_func.packet_svFazNada,
                this);
            packet_func.funcs_sv.addPacketCall(0x01,
                packet_func.packet_svFazNada,
                this);
            packet_func.funcs_sv.addPacketCall(0x02,
                packet_func.packet_svFazNada,
                this);
            packet_func.funcs_sv.addPacketCall(0x03,
                packet_func.packet_svFazNada,
                this);
            packet_func.funcs_sv.addPacketCall(0x04,
                packet_func.packet_svFazNada,
                this);
            packet_func.funcs_sv.addPacketCall(0x05,
                packet_func.packet_svFazNada,
                this);
            packet_func.funcs_sv.addPacketCall(0x06,
                packet_func.packet_svFazNada,
                this);
            packet_func.funcs_sv.addPacketCall(0x07,
                packet_func.packet_svFazNada,
                this);
            packet_func.funcs_sv.addPacketCall(0x08,
                packet_func.packet_svFazNada,
                this);
            packet_func.funcs_sv.addPacketCall(0x09,
                packet_func.packet_svFazNada,
                this);
            packet_func.funcs_sv.addPacketCall(0x0A,
                packet_func.packet_svFazNada,
                this);
            packet_func.funcs_sv.addPacketCall(0x0B,
                packet_func.packet_svFazNada,
                this);
            packet_func.funcs_sv.addPacketCall(0x0C,
                packet_func.packet_svFazNada,
                this);
            packet_func.funcs_sv.addPacketCall(0x0D,
                packet_func.packet_svFazNada,
                this);
            packet_func.funcs_sv.addPacketCall(0x0E,
                packet_func.packet_svFazNada,
                this);

            packet_func.funcs_sv.addPacketCall(0xFE, packet_func.packet_svFazNada, this);

            m_state = ServerState.Initialized;

        }


        public override void config_init()
        {
            base.config_init();

            m_si.tipo = 5;//auth server

        }
        protected virtual void ReloadFiles()
        {
            config_init();

            sIff.getInstance().reload();
        }

        public void requestDisconnectPlayer(Player _session, packet _packet)
        {
            //REQUEST_BEGIN("DisconnectPlayer");

            var p = new PangyaBinaryWriter();

            uint player_uid = 0;
            uint server_uid = 0;

            try
            {

                player_uid = _packet.ReadUInt32();
                server_uid = _packet.ReadUInt32();

                // Verifica se session est  autorizada para executar esse a  o, 
                // se ele n o fez o login com o Server ele n o pode fazer nada at  que ele fa a o login
                //CHECK_SESSION_IS_AUTHORIZED("DisconnectPlayer");

                var s = m_player_manager.findPlayer(server_uid);

                if (s != null)
                { 
                    // Envia para o outro server o comando para desconectar o player
                    p.init_plain((ushort)0x6);

                    p.WriteUInt32(player_uid);
                    p.WriteUInt32(_session.m_pi.uid); // Quem pediu para disconectar o player
                    p.WriteByte(0); // 1 Ativado, Flag que forca a desconecta mesmo se o server tiver outras regras

                    packet_func.session_send(p,
                        s, 1);

                }
                else
                { 
                    // Ent o retorn para o Cliente que pediu para desconectar o player,
                    // para ele continuar sua execu  o e deixar que o server deconecte o player quando ele logar
                    p.init_plain((ushort)0x7);

                    p.WriteUInt32(player_uid);

                    packet_func.session_send(p,
                        _session, 1);

                }

            }
            catch (exception e)
            {

                // Ent o retorn para o Cliente que pediu para desconectar o player,
                // para ele continuar sua execu  o e deixar que o server deconecte o player quando ele logar
                p.init_plain((ushort)0x7);

                p.WriteUInt32(player_uid);

                packet_func.session_send(p,
                    _session, 1);

                _smp.message_pool.getInstance().push(new message("[AuthServer::requestDisconnectPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public void requestConfirmDisconnectPlayer(Player _session, packet _packet)
        {
            //REQUEST_BEGIN("ConfirmDisconnectPlayer");

            try
            {

                uint player_uid = _packet.ReadUInt32();
                uint server_uid = _packet.ReadUInt32();

                // Verifica se session est  autorizada para executar esse a  o, 
                // se ele n o fez o login com o Server ele n o pode fazer nada at  que ele fa a o login
                //CHECK_SESSION_IS_AUTHORIZED("ConfirmDisconnectPlayer");

                if (server_uid != m_si.uid)
                {

                    // N o foi o Auth Server que pediu para disconectar esse usu rio, procura o cliente e manda a resposta para ele
                    var s = m_player_manager.findPlayer(server_uid);

                    if (s != null)
                    {

                        // Log
                        _smp.message_pool.getInstance().push(new message("[AuthServer::requestConfirmDisconnectPlayer][Log] o Server[UID=" + Convert.ToString(_session.m_pi.uid) + "] pediu para enviar a confirmacao para o Server[UID=" + Convert.ToString(server_uid) + "] que o Player[UID=" + Convert.ToString(player_uid) + "] foi deconectado.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        var p = new PangyaBinaryWriter((ushort)0x7);

                        p.WriteUInt32(player_uid);

                        packet_func.session_send(p,
                            s, 1);

                    }
                    else
                    {
                        _smp.message_pool.getInstance().push(new message("[AuthServer::requestConfirmDisconnectPlayer][WARNING] o Server[UID=" + Convert.ToString(_session.m_pi.uid) + "] pediu para enviar a confirmacao para o Server[UID=" + Convert.ToString(server_uid) + "] que o Player[UID=" + Convert.ToString(player_uid) + "] foi desconectado, mas o server nao esta conectado.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                }
                else
                {
                    _smp.message_pool.getInstance().push(new message("[AuthServer::requestConfirmDisconnectPlayer][Log] o Server[UID=" + Convert.ToString(_session.m_pi.uid) + "] pediu para enviar a confirmacao para o Server[UID=" + Convert.ToString(server_uid) + "](Auth Server) que o Player[UID=" + Convert.ToString(player_uid) + "] foi desconectado.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[AuthServer::requestConfirmDisconnectPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestInfoPlayer(Player _session, packet _packet)
        {
            //REQUEST_BEGIN("InfoPlayer");

            var p = new PangyaBinaryWriter();

            uint server_uid = 0;
            uint player_uid = 0;

            try
            {

                server_uid = _packet.ReadUInt32();
                player_uid = _packet.ReadUInt32(); 
                if (server_uid > 0)
                {
                    var s = m_player_manager.findPlayer(server_uid);
                    if (s != null)
                    {

                        // Log
                        _smp.message_pool.getInstance().push(new message("[AuthServer::requestInfoPlayer][Log] o Server[UID=" + Convert.ToString(_session.m_pi.uid) + "] pediu para o outro Server[UID=" + Convert.ToString(server_uid) + "] o Info do Player[UID=" + Convert.ToString(player_uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        // Envia para o outro server o comando para desconectar o player
                        p.init_plain(0x0B);

                        p.WriteUInt32(_session.m_pi.uid); // Server UID request (quem pediu o info do player)
                        p.WriteUInt32(player_uid);

                        packet_func.session_send(p,
                            s, 1);

                    }
                    else
                    {

                        // Ent o retorna para o Cliente que pediu o info do player, 
                        // dizendo que o player n o foi encontrado online por que o server n o foi encontrado online no Auth Server
                        p.init_plain((ushort)0xC);

                        p.WriteUInt32(server_uid);
                        p.WriteInt32(-1); // Error n o encontrou o server para enviar o request

                        p.WriteUInt32(player_uid);

                        packet_func.session_send(p,
                            _session, 1);

                    }
                }
                else
                {
                    var game = m_player_manager.findPlayerByType(1);
                    foreach (var s in game)
                    { 
                        // Envia para o outro server o comando para desconectar o player
                        p.init_plain(0x0B);

                        p.WriteUInt32(_session.m_pi.uid); // Server UID request (quem pediu o info do player)
                        p.WriteUInt32(player_uid);

                        packet_func.session_send(p,
                            s, 1);
                    }
                }
            }
            catch (exception e)
            {

                // Ent o retorna para o Cliente que pediu o info do player, 
                // dizendo que o player n o foi encontrado online por que o teve algum Exception no Auth Server
                p.init_plain((ushort)0xC);

                p.WriteUInt32(server_uid);
                p.WriteInt32(-1); // Error n o encontrou o server para enviar o request

                p.WriteUInt32(player_uid);

                packet_func.session_send(p,
                    _session, 1);

                _smp.message_pool.getInstance().push(new message("[AuthServer::requestInfoPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public void requestConfirmSendInfoPlayer(Player _session, packet _packet)
        {
            //REQUEST_BEGIN("ConfirmSendInfoPlayer");

            try
            {

                AuthServerPlayerInfo aspi = new AuthServerPlayerInfo();

                uint req_server_uid = _packet.ReadUInt32();

                aspi.option = _packet.ReadInt32();
                aspi.uid = _packet.ReadUInt32();

                if (aspi.option == 1)
                { 
                    aspi.id = _packet.ReadString();
                    aspi.ip = _packet.ReadString(); 
                } 
                if (req_server_uid != m_si.uid)
                {

                    // N o foi o Auth Server que pediu para disconectar esse usu rio, procura o cliente e manda a resposta para ele
                    var s = m_player_manager.findPlayer(req_server_uid);

                    if (s != null)
                    {

                        // Log
                        _smp.message_pool.getInstance().push(new message("[AuthServer::requestConfirmSendInfoPlayer][Log] o Server[UID=" + Convert.ToString(_session.m_pi.uid) + "] pediu para enviar a confirmacao para o Server[UID=" + Convert.ToString(req_server_uid) + "] do info do Player[UID=" + Convert.ToString(aspi.uid) + "].", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        // Resposta
                        var p = new PangyaBinaryWriter((ushort)0xC);

                        p.WriteUInt32(_session.m_pi.uid); // Server UID (Quem pediu para enviar a confirma  o do info do player)
                        p.WriteInt32(aspi.option);
                        p.WriteUInt32(aspi.uid);

                        if (aspi.option == 1)
                        {

                            p.WriteString(aspi.id);
                            p.WriteString(aspi.ip);

                        }

                        packet_func.session_send(p,
                            s, 1);

                    }
                    else
                    {
                        _smp.message_pool.getInstance().push(new message("[AuthServer::requestConfirmSendInfoPlayer][WARNING] o Server[UID=" + Convert.ToString(_session.m_pi.uid) + "] pediu para enviar a confirmacao para o Server[UID=" + Convert.ToString(req_server_uid) + "] do info do Player[UID=" + Convert.ToString(req_server_uid) + "], mas o server nao esta conectado.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                }
                else
                {
                    _smp.message_pool.getInstance().push(new message("[AuthServer::requestConfirmSendInfoPlayer][Log] o Server[UID=" + Convert.ToString(_session.m_pi.uid) + "] pediu para enviar a confirmacao para o Server[UID=" + Convert.ToString(req_server_uid) + "](Auth Server) do info do Player[UID=" + Convert.ToString(req_server_uid) + "].", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[AuthServer::requestConfirmSendInfoPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public void requestSendCommandToOtherServer(Player _session, packet _packet)
        {
            //REQUEST_BEGIN("SendCommandToOtherServer");

            var p = new PangyaBinaryWriter();

            try
            {

                // Verifica se session est  autorizada para executar esse a  o, 
                // se ele n o fez o login com o Server ele n o pode fazer nada at  que ele fa a o login
                //CHECK_SESSION_IS_AUTHORIZED("SendCommandToOtherServer");

                CommandOtherServerHeaderEx cosh = new CommandOtherServerHeaderEx();

                _packet.ReadBuffer(ref cosh, Marshal.SizeOf(new CommandOtherServerHeader()));

                ushort command_buff_size = 0;

                if ((Marshal.SizeOf(new CommandOtherServerHeader()) + 2 /*/ *Packet ID * /*/) < _packet.GetSize)
                {
                    command_buff_size = (ushort)(_packet.GetSize - (Marshal.SizeOf(new CommandOtherServerHeader()) + 2 /*/ *Packet ID * /*/));
                }

                var s = m_player_manager.findPlayer(cosh.send_server_uid_or_type);

                if (s == null)
                {

                    var v_s = m_player_manager.findPlayerByTypeExcludeUID(cosh.send_server_uid_or_type, _session.m_pi.uid);

                    if (!v_s.empty())
                    {

                        // Inicializa o Buffer do comando
                        if (command_buff_size > 0)
                        {

                            cosh.command.init(command_buff_size);

                            if (!cosh.command.is_good())
                            {
                                throw new exception("[AuthServer::requestSendCommandToOtherServer][Error] Server[UID=" + Convert.ToString(_session.m_pi.uid) + "] pediu para enviar o command[ID=" + Convert.ToString(cosh.command_id) + "] para o outro Server[UID/TYPE=" + Convert.ToString(cosh.send_server_uid_or_type) + "], mas nao conseguiu alocar memoria para o comando buff[size=" + Convert.ToString(command_buff_size) + "]. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.AUTH_SERVER,
                                    3501, 0));
                            }

                            // Ler o comando buffer do _packet, para enviar para o outro Server
                            cosh.command.buff = _packet.ReadBytes(cosh.command.size);

                        }

                        // Log
                        _smp.message_pool.getInstance().push(new message("[AuthServer::requestSendCommandToOtherServer][Log] Server[UID=" + Convert.ToString(_session.m_pi.uid) + "] pediu para enviar o command[ID=" + Convert.ToString(cosh.command_id) + "] para o outro Server[UID/TYPE=" + Convert.ToString(cosh.send_server_uid_or_type) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        // Envia para todos o Server do mesmo tipo excluindo quem pediu para enviar o comando
                        p.init_plain((ushort)0x0D);

                        // Quem pediu para enviar esse comando para o outro Server
                        p.WriteUInt32(_session.m_pi.uid);

                        // Comando ID
                        p.WriteUInt16(cosh.command_id);

                        if (command_buff_size > 0 && cosh.command.size > 0)
                        {

                            // Comando buffer
                            p.WriteBytes(cosh.command.buff, cosh.command.size);

                        }
                        else
                        {
                            p.WriteUInt16(0); // Comando buffer   vazio
                        }

                        packet_func.vector_send(p,
                            v_s, 1);

                    }
                    else
                    {
                        throw new exception("[AuthServer::requestSendCommandToOtherServer][WARNING] Server[UID=" + Convert.ToString(_session.m_pi.uid) + "] pediu para enviar o command[ID=" + Convert.ToString(cosh.command_id) + "] para o outro Server[UID/TYPE=" + Convert.ToString(cosh.send_server_uid_or_type) + "], mas nao encontrou ele.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.AUTH_SERVER,
                            3500, 0));
                    }

                }
                else
                {

                    // Inicializa o Buffer do comando
                    if (command_buff_size > 0)
                    {

                        cosh.command.init(command_buff_size);

                        if (!cosh.command.is_good())
                        {
                            throw new exception("[AuthServer::requestSendCommandToOtherServer][Error] Server[UID=" + Convert.ToString(_session.m_pi.uid) + "] pediu para enviar o command[ID=" + Convert.ToString(cosh.command_id) + "] para o outro Server[UID/TYPE=" + Convert.ToString(cosh.send_server_uid_or_type) + "], mas nao conseguiu alocar memoria para o comando buff[size=" + Convert.ToString(command_buff_size) + "]. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.AUTH_SERVER,
                                3501, 0));
                        }

                        // Ler o comando buffer do _packet, para enviar para o outro Server
                        cosh.command.buff = _packet.ReadBytes(cosh.command.size);

                    }

                    // Log
                    _smp.message_pool.getInstance().push(new message("[AuthServer::requestSendCommandToOtherServer][Log] Server[UID=" + Convert.ToString(_session.m_pi.uid) + "] pediu para enviar o command[ID=" + Convert.ToString(cosh.command_id) + "] para o outro Server[UID/TYPE=" + Convert.ToString(cosh.send_server_uid_or_type) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Envia para o Server
                    p.init_plain((ushort)0x0D);

                    // Quem pediu para enviar esse comando para o outro Server
                    p.WriteUInt32(_session.m_pi.uid);

                    // Comando ID
                    p.WriteUInt16(cosh.command_id);

                    if (command_buff_size > 0 && cosh.command.size > 0)
                    {

                        // Comando buffer
                        p.WriteBytes(cosh.command.buff, cosh.command.size);

                    }
                    else
                    {
                        p.WriteUInt16(0); // Comando buffer   vazio
                    }

                    packet_func.session_send(p,
                        s, 1);
                }


            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[AuthServer::requestSendCommandToOtherServer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public void requestSendReplyToOtherServer(Player _session, packet _packet)
        {
            //REQUEST_BEGIN("SendReplyToOtherServer");

            var p = new PangyaBinaryWriter();

            try
            {

                // Verifica se session est  autorizada para executar esse a  o, 
                // se ele n o fez o login com o Server ele n o pode fazer nada at  que ele fa a o login
                //CHECK_SESSION_IS_AUTHORIZED("SendReplyToOtherServer");

                CommandOtherServerHeaderEx cosh = new CommandOtherServerHeaderEx();

                _packet.ReadBuffer(ref cosh, Marshal.SizeOf(new CommandOtherServerHeader()));

                ushort command_buff_size = 0;

                if ((Marshal.SizeOf(new CommandOtherServerHeader()) + 2 /*/ *Packet ID * /*/) < _packet.GetSize)
                {
                    command_buff_size = (ushort)(_packet.GetSize - (Marshal.SizeOf(new CommandOtherServerHeader()) + 2 /*/ *Packet ID * /*/));
                }

                var s = m_player_manager.findPlayer(cosh.send_server_uid_or_type);

                if (s == null)
                {
                    throw new exception("[AuthServer::requestSendReplyToOtherServer][WARNING] Server[UID=" + Convert.ToString(_session.m_pi.uid) + "] pediu para enviar a resposta[ID=" + Convert.ToString(cosh.command_id) + "] para o outro Server[UID=" + Convert.ToString(cosh.send_server_uid_or_type) + "], mas nao encontrou ele.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.AUTH_SERVER,
                        3502, 0));
                }

                // Inicializa o Buffer do comando
                if (command_buff_size > 0)
                {

                    cosh.command.init(command_buff_size);

                    if (!cosh.command.is_good())
                    {
                        throw new exception("[AuthServer::requestSendReplyToOtherServer][Error] Server[UID=" + Convert.ToString(_session.m_pi.uid) + "] pediu para enviar a resposta[ID=" + Convert.ToString(cosh.command_id) + "] para o outro Server[UID=" + Convert.ToString(cosh.send_server_uid_or_type) + "], mas nao conseguiu alocar memoria para o comando buff[size=" + Convert.ToString(command_buff_size) + "]. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.AUTH_SERVER,
                            3503, 0));
                    }

                    // Ler o comando buffer do _packet, para enviar para o outro Server
                    cosh.command.buff = _packet.ReadBytes(cosh.command.size);

                }

                // Log
                _smp.message_pool.getInstance().push(new message("[AuthServer::requestSendReplyToOtherServer][Log] Server[UID=" + Convert.ToString(_session.m_pi.uid) + "] pediu para enviar a resposta[ID=" + Convert.ToString(cosh.command_id) + "] para o outro Server[UID=" + Convert.ToString(cosh.send_server_uid_or_type) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Envia para o Server
                p.init_plain((ushort)0x0E);

                // Quem pediu para enviar esse comando para o outro Server
                p.WriteUInt32(_session.m_pi.uid);

                // Comando ID
                p.WriteUInt16(cosh.command_id);

                if (command_buff_size > 0 && cosh.command.size > 0)
                {

                    // Comando buffer
                    p.WriteBytes(cosh.command.buff, cosh.command.size);

                }
                else
                {
                    p.WriteUInt16(0); // Comando buffer   vazio
                }

                packet_func.session_send(p,
                    s, 1);
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[AuthServer::requestSendReplyToOtherServer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestAuthenticPlayer(Player _session, packet _packet)
        {
            //REQUEST_BEGIN("AuthenticPlayer");

            try
            {

                _session.m_pi.tipo = _packet.ReadUInt32(); // Tipo do server
                _session.m_pi.uid = _packet.ReadUInt32(); // UID
                _session.m_pi.id = _packet.ReadString();
                string key = _packet.ReadString();
                var version_client = _packet.ReadString();
                var packet_version = _packet.ReadUInt32();
                // Passa para o nickname o id
                _session.m_pi.nickname = _session.m_pi.id;

                CmdAuthServerKey cmd_ask = new CmdAuthServerKey((int)_session.m_pi.uid); // Waiter

                snmdb.NormalManagerDB.getInstance().add(0,
                    cmd_ask, null, null);


                if (cmd_ask.getException().getCodeError() != 0)
                {
                    throw cmd_ask.getException();
                }

                var ask = cmd_ask.getInfo();

                if (!ask.checkKey(key))
                {
                    throw new exception("SERVER[UID=" + Convert.ToString(_session.m_pi.uid) + "] key[KEY=" + key + "] is not valid. Key[KEY=" + (ask.key) + ", VALID=" + Convert.ToString((ushort)ask.valid) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_AS,
                        350, 0));
                }

                ask.valid = 0; // J  usou a chave atualiza no banco de dados

                // Update Auth Server Key of Server
                snmdb.NormalManagerDB.getInstance().add(2,
                    new CmdUpdateAuthServerKey(ask),
                    SQLDBResponse,
                    this);

                // Logou com sucesso [Por Hora vou deixar assim]
                _session.m_is_authorized = true; // Autorizado a ficar connectado, por bastante tempo

                // UPDATE TO CLIENTE
                var p = new PangyaBinaryWriter((ushort)0x01);

                p.WriteInt32(_session.m_oid); // OID

                packet_func.session_send(p, _session, 1);
                _smp.message_pool.getInstance().push(new message($"[AuthServer::requestAuthenticPlayer][Sucess] SERVER[OID: {_session.m_oid}, UID: {_session.m_pi.uid}, TYPE: {_session.m_pi.tipo}]", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[AuthServer::requestAuthenticPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Log

                DisconnectSession(_session);
            }
        }

        public override void SQLDBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg)
        {
            if (_pangya_db is CmdServerList cmdServerList)
            {
                base.SQLDBResponse(_msg_id, cmdServerList, _arg);
                return;
            }

            if (_arg == null)
            {
                _smp.message_pool.getInstance().push(new message("[AuthServer::SQLDBResponse][WARNING] _arg is nullptr, na msg_id = " + Convert.ToString(_msg_id), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            // Por Hora s  sai, depois fa o outro tipo de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.message_pool.getInstance().push(new message("[AuthServer::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            var _server = (AuthServer)(_arg);

            switch (_msg_id)
            {
                case 1: // Update Command
                    {

                        var cmd_uc = (CmdUpdateCommand)(_pangya_db);
                        break;
                    }
                case 2: // Update Auth Server Key
                    {

                        var cmd_uask = (CmdUpdateAuthServerKey)(_pangya_db);
                        break;
                    }
                case 3: // Update Guild Ranking
                    {
                        break;
                    }
                case 0:
                default:

                    break;
            }
        }
         
        public void requestSendPongInfo(Player session, packet _packet)
        {
            session.last_activity = DateTime.Now;

            var p = new PangyaBinaryWriter(0xFE); // PONG
            p.WriteTime(session.last_activity); 
            packet_func.session_send(p, session, 1);
        }

        public bool CheckCommand(Queue<string> _command)
        {
            Console.ResetColor();

            if (_command.Count == 0)
            {
                _smp.message_pool.getInstance().push(new message("[AuthServer::CheckCommand][Error] Missing parameter", type_msg.CL_ONLY_CONSOLE));
                return true;
            }

            string s = _command.Dequeue();

            if (!string.IsNullOrEmpty(s) && s == "exit")
            {
                Environment.Exit(-1);
                return true;
            }  
            else if (s == "cls" || s == "clear")
            {
                Console.Clear();
                ConsoleEx.Log();
                return true;
            }
            else
            {
                _smp.message_pool.getInstance().push(new message($"[AuthServer::CheckCommand][Error] Command No Exist-> {s}", type_msg.CL_ONLY_CONSOLE));
                return false;
            }
        }
    } 
}

// Server Static 
namespace sas
{
    //as nao pode, é ref
    public class @as : Singleton<Pangya_AuthServer.AuthServerTcp.AuthServer>
    {
    }
}