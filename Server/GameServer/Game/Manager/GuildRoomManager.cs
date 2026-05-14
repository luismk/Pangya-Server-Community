using System;
using System.Collections.Generic;
using System.Linq;
using Pangya_GameServer.Models;

using PangyaAPI.SQL;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer.Game.Manager
{
    // Guild Room Manager
    public class GuildRoomManager
    {
        public enum eGUILD_WIN : byte
        {
            RED,
            BLUE,
            DRAW
        }

        public GuildRoomManager()
        {
            this.m_dupla_manager = new DuplaManager();
            this.v_guilds = new List<Guild>();
            this.m_guild_win = eGUILD_WIN.DRAW;
        }
        public void Dispose()
        {

            if (v_guilds.Any())
            {
                v_guilds.Clear();
            }
        }
        public Guild addGuild(Guild.eTEAM _team, int _uid)
        {

            Guild guild = null;


            v_guilds.Add(new Guild(_uid, _team));

            guild = (v_guilds.FirstOrDefault(c => c.getUID() == _uid));
            return guild;
        }

        public Guild addGuild(Guild _guild)
        {
            v_guilds.Add(_guild);

            return _guild;
        }

        public void deleteGuild(Guild _guild)
        {

            if (_guild == null)
            {

                _smp.message_pool.getInstance().push(new message("[GuildRoomManager::deleteGuild][Error] _guild is invalid(null). Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }



            var it = v_guilds.FirstOrDefault(_el =>
            {
                return _el.getUID() == _guild.getUID() && (_el) == _guild;
            });

            if (it != null)
            {
                v_guilds.Remove(it);
            }
        }

        public uint getNumGuild()
        {
            return (uint)v_guilds.Count;
        }

        public GuildRoomManager.eGUILD_WIN getGuildWin()
        {
            return m_guild_win;
        }

        public Guild findGuildByTeam(Guild.eTEAM _team)
        {



            var it = v_guilds.FirstOrDefault(_el =>
            {
                return _el.getTeam() == _team;
            });



            return it;
        }

        public Guild findGuildByUID(uint _uid)
        {



            var it = v_guilds.FirstOrDefault(_el =>
            {
                return _el.getUID() == _uid;
            });



            return it;
        }

        public Guild findGuildByPlayer(Player _session)
        {



            var it = v_guilds.FirstOrDefault(_el =>
            {
                return _el.findPlayerByUID(_session.m_pi.uid) != null;
            });



            return it;
        }

        public Dupla findDupla(Player _session)
        {
            return m_dupla_manager.findDuplaByPlayer(_session);
        }

        public void init_duplas()
        {

            if (v_guilds.Count != 2)
            {

                _smp.message_pool.getInstance().push(new message("[GuildRoomManager::init_duplas][Error] nao tem duas guilds[NUM=" + Convert.ToString(v_guilds.Count) + "] para inicializar as duplas. Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }

            m_dupla_manager.init_duplas(v_guilds.FirstOrDefault(), v_guilds.LastOrDefault());

            m_guild_win = eGUILD_WIN.DRAW;
        }

        // Verifica se tem a quantidade de jogadores para come�ar o Guild Battle
        public int isGoodToStart()
        {

            // S� tem uma ou nenhuma guild na sala
            if (v_guilds.Count <= 1)
            {
                return 0;
            }

            var last_players = -1;

            int ret = 1;



            foreach (var el in v_guilds)
            {

                // Não tem o mesmo número de jogadores na sala as guilds
                if (last_players != -1 && last_players != el.numPlayers())
                {

                    ret = -1;

                    break;
                }

                last_players = (int)el.numPlayers();

                // Uma Guild tem menos que 2 jogadores na sala
                if (last_players < 2)
                {

                    ret = -2;

                    break;
                }
            }
            return ret;
        }

        // Verifica se sobrou s� players de uma guild s�
        public bool oneGuildRest()
        {

            if (v_guilds.Count <= 1)
            {
                return true;
            }

            return m_dupla_manager.oneGuildRest();
        }

        // update dados guilds
        public void update()
        {

            if (v_guilds.Count > 0)
            {
                m_dupla_manager.updateGuildDados((v_guilds.First()), (v_guilds.Count == 2 ? (v_guilds.Last()) : null));
            }
        }

        public void calcGuildWin()
        {

            m_guild_win = eGUILD_WIN.DRAW;

            // Calcula Guild Win
            if (v_guilds.Count > 1u)
            {

                if (v_guilds.Last().numPlayers() == 0u
                    || m_dupla_manager.getNumPlayersQuitGuild(v_guilds.Last().getUID()) == v_guilds.Last().numPlayers()
                    || (v_guilds.First().getPoint() > v_guilds.Last().getPoint() || (v_guilds.First().getPoint() == v_guilds.Last().getPoint() && v_guilds.First().getPang() > v_guilds.Last().getPang())))
                {
                    m_guild_win = (eGUILD_WIN)v_guilds.First().getTeam();
                }
                else if (v_guilds.First().numPlayers() == 0u || m_dupla_manager.getNumPlayersQuitGuild(v_guilds.First().getUID()) == v_guilds.First().numPlayers() || (v_guilds.Last().getPoint() > v_guilds.First().getPoint() || (v_guilds.Last().getPoint() == v_guilds.First().getPoint() && v_guilds.Last().getPang() > v_guilds.First().getPang())))
                {
                    m_guild_win = (eGUILD_WIN)v_guilds.LastOrDefault().getTeam();
                }

            }
            else if (v_guilds.Count > 0u)
            {
                m_guild_win = (eGUILD_WIN)v_guilds.First().getTeam();
            }

            // Calcula Guild Pang Win
            uint pang_winner = (m_dupla_manager.getNumDuplas() + m_dupla_manager.getNumPlayersQuit()) * 50u;
            uint pang_loser = m_dupla_manager.getNumPlayersQuit() * 50u + 50u;

            foreach (var el in v_guilds)
            {
                m_dupla_manager.updatePangWinDuplas(el.getUID(), ((byte)el.getTeam() == (byte)m_guild_win) ? pang_winner : pang_loser);
            }

            update();
        }

        public void saveGuildsData()
        {

            // Update Guild Point and Pang Win
            GuildPoints gp = new GuildPoints() { uid = 0u };


            foreach (var el in v_guilds)
            {

                gp.clear();

                gp.uid = el.getUID();
                gp.point = (uint)el.getPoint();
                gp.pang = el.getPangWin();
                gp.win = (m_guild_win == GuildRoomManager.eGUILD_WIN.DRAW ? GuildPoints.eGUILD_WIN.DRAW : ((byte)el.getTeam() == (byte)m_guild_win ? GuildPoints.eGUILD_WIN.WIN : GuildPoints.eGUILD_WIN.LOSE));

                //snmp.NormalManagerDB.getInstance().add(2,
                //    new CmdUpdateGuildPoints(gp),
                //    SQLDBResponse,
                //    this);
            }

            // Update Guild Members Point and Pang Win
            m_dupla_manager.saveGuildMembersData();

            // Register Guild Match
            if (v_guilds.Count > 1)
            {

                GuildMatch match = new GuildMatch();

                // Guild 1
                match.uid[0] = v_guilds.First().getUID();
                match.pang[0] = (uint)v_guilds.First().getPang();
                match.point[0] = (uint)v_guilds.First().getPoint();

                // Guild 2
                match.uid[1] = v_guilds.Last().getUID();
                match.pang[1] = (uint)v_guilds.Last().getPang();
                match.point[1] = (uint)v_guilds.Last().getPoint();

                //snmp.NormalManagerDB.getInstance().add(1,
                //    new CmdRegisterGuildMatch(match),
                //    SQLDBResponse,
                //    this);
            }
        }

        public void initPacketDuplas(ref PangyaBinaryWriter _p)
        {

            m_dupla_manager.initPacketDuplas(_p);
        }

        public bool finishHoleDupla(PlayerGameInfo _pgi, ushort _seq_hole)
        {

            if (m_dupla_manager.finishHoleDupla(_pgi, _seq_hole))
            {

                update();

                return true;
            }

            return false;
        }

        protected static void SQLDBResponse(int _msg_id,
            Pangya_DB _pangya_db,
            object _arg)
        {

            if (_arg == null)
            {
                return;
            }

            // Por Hora s� sai, depois fa�o outro tipo de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.message_pool.getInstance().push(new message("[GuildRoomManager::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            var _channel = (GuildRoomManager)(_arg);

            switch (_msg_id)
            {
                case 1: // Register Guild Match
                    {
                        break;
                    }
                case 2: // Guild Update Points
                    {
                        break;
                    }
                case 0:
                default:
                    break;
            }
        }

        protected List<Guild> v_guilds = new List<Guild>(); // Guilds

        protected DuplaManager m_dupla_manager = new DuplaManager(); // Dupla Manager

        protected eGUILD_WIN m_guild_win = new eGUILD_WIN(); // Guild Win
    }
}