using System;
using System.Collections.Generic;
using System.Linq;
using Pangya_GameServer.Models;

using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;


namespace Pangya_GameServer.Game.Manager
{
    public class DuplaManager
    {
        public DuplaManager()
        {
            this.v_duplas = new List<Dupla>();
        }

        public void init_duplas(Guild _g1, Guild _g2)
        {

            // Limpa duplas


            if (!v_duplas.Any())
            {
                v_duplas.Clear();
            }

            List<uint> a = new List<uint>();
            List<uint> b = new List<uint>();

            uint i = 0;

            for (i = 0; i < _g1.numPlayers(); ++i)
            {
                a.Add(i);
                b.Add(i);
            }

            for (i = 0; i < _g1.numPlayers(); ++i)
            {
                addDupla(_g1.getPlayerByIndex(a[(int)i]), _g2.getPlayerByIndex(b[(int)i]));
            }

            a.Clear();
            b.Clear();
        }

        public void addDupla(Player _p1, Player _p2)
        {

            if (_p1 == null || _p2 == null)
            {

                _smp.message_pool.getInstance().push(new message("[DuplaManager::addDupla][Error] _p" + (_p1 == null && _p2 == null ? "1 and _p2" : (_p1 == null ? "1" : "2")) + " is invalid", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }



            v_duplas.Add(new Dupla((byte)(v_duplas.Count + 1),
                _p1, _p2));
        }

        public void deleteDupla(Dupla _dupla)
        {

            if (_dupla == null)
            {

                _smp.message_pool.getInstance().push(new message("[DuplaManager::deleteDupla][Error] _dupla is invalid(null). Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }

            deleteDupla(_dupla.numero);
        }

        public void deleteDupla(byte _numero)
        {



            var it = v_duplas.FirstOrDefault(_el =>
            {
                return _el.numero == _numero;
            });

            if (it != null)
            {
                v_duplas.Remove(it);
            }
        }

        public Dupla findDuplaByPlayer(Player _session)
        {



            var it = v_duplas.FirstOrDefault(_el =>
            {
                return _el.p[0] == _session || _el.p[1] == _session;
            });

            return it;
        }

        public Dupla findDuplaByPlayerUID(uint _uid)
        {



            var it = v_duplas.FirstOrDefault(_el =>
            {
                return (_el.p[0] != null && _el.p[0].m_pi.uid == _uid) || (_el.p[1] != null && _el.p[1].m_pi.uid == _uid);
            });

            return it;
        }

        public Dupla findDuplaByNumero(byte _numero)
        {



            var it = v_duplas.FirstOrDefault(_el =>
            {
                return _el.numero == _numero;
            });

            return it;
        }

        public uint getNumDuplas()
        {
            return (uint)v_duplas.Count();
        }

        public uint getNumPlayersQuit()
        {

            uint count = 0;

            foreach (var _el in v_duplas)
            {
                if (_el.state[0] == Dupla.eSTATE.OUT_GAME)
                {
                    count++;
                }
                if (_el.state[1] == Dupla.eSTATE.OUT_GAME)
                {
                    count++;
                }
            }
            return (count);
        }

        public uint getNumPlayersQuitGuild(Guild _g)
        {

            if (_g == null)
            {

                _smp.message_pool.getInstance().push(new message("[DuplaManager::getNumPlayersQuitGuild][Error] _g is invalid(null). Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return 0;
            }

            return getNumPlayersQuitGuild(_g.getUID());
        }

        public uint getNumPlayersQuitGuild(uint _uid)
        {

            var count = 0u;



            count = (uint)v_duplas.Where(_el =>
            {
                return (_el.state[0] == Dupla.eSTATE.OUT_GAME && _el.p[0] != null && _el.p[0].m_pi.gi.uid == _uid) || (_el.state[1] == Dupla.eSTATE.OUT_GAME && _el.p[1] != null && _el.p[1].m_pi.gi.uid == _uid);
            }).ToList().Count();

            return (uint)count;
        }

        public void updateGuildDados(Guild _g1, Guild _g2)
        {

            int[] score = { 0, 0 };
            ulong[] pang = { 0Ul, new ulong() };
            uint[] pang_win = { 0u, new uint() };



            foreach (var el in v_duplas)
            {

                // Guild 1
                score[0] += el.sumScoreP1();
                pang[0] += el.pang[0];
                pang_win[0] += el.pang_win[0];

                //Guild 2
                score[1] += el.sumScoreP2();
                pang[1] += el.pang[1];
                pang_win[1] += el.pang_win[1];
            }

            // Guild 1
            if (_g1 != null)
            {

                _g1.setPoint(score[0]);
                _g1.setPang(pang[0]);
                _g1.setPangWin(pang_win[0]);
            }

            // Guild 2
            if (_g2 != null)
            {

                _g2.setPoint(score[1]);
                _g2.setPang(pang[1]);
                _g2.setPangWin(pang_win[1]);
            }
        }

        public void updatePangWinDuplas(Guild _g, uint _pang_win)
        {

            if (_g == null)
            {

                _smp.message_pool.getInstance().push(new message("[DuplaManager::updatePangWinDuplas][Error] _g is invalid(null). Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }

            updatePangWinDuplas(_g.getUID(), _pang_win);
        }

        public void updatePangWinDuplas(uint _uid, uint _pang_win)
        {



            foreach (var el in v_duplas)
            {

                if (el.p[0] != null && el.p[0].m_pi.gi.uid == _uid)
                {
                    el.pang_win[0] = _pang_win;
                }
                else if (el.p[1] != null && el.p[1].m_pi.gi.uid == _uid)
                {
                    el.pang_win[1] = _pang_win;
                }
            }
        }

        public bool oneGuildRest()
        {

            bool ret = false;

            uint[] count = { 0u, new uint() };



            foreach (var el in v_duplas)
            {

                if (el.p[0] != null && el.state[0] != Dupla.eSTATE.OUT_GAME)
                {
                    count[0]++;
                }

                if (el.p[1] != null && el.state[1] != Dupla.eSTATE.OUT_GAME)
                {
                    count[1]++;
                }
            }

            // Uma das duas guilds, seus membros sairam todos do jogo
            if (count[0] == 0u || count[1] == 0u)
            {
                ret = true;
            }

            return ret;
        }

        public void saveGuildMembersData()
        {

            // Update Guild Members Point and Pang Win
            GuildMemberPoints gmp = new GuildMemberPoints() { guild_uid = 0 };



            try
            {

                foreach (var el in v_duplas)
                {

                    // Player 1
                    if (el.p[0] != null)
                    {

                        gmp = new GuildMemberPoints();

                        gmp.guild_uid = el.p[0].m_pi.gi.uid;
                        gmp.member_uid = el.p[0].m_pi.uid;
                        gmp.pang = el.pang_win[0];
                        gmp.point = el.sumScoreP1();

                        // Update ON SERVER
                        el.p[0].m_pi.mi.guild_pang = (long)(el.p[0].m_pi.gi.pang += gmp.pang);
                        el.p[0].m_pi.mi.guild_point = (uint)(el.p[0].m_pi.gi.point += (uint)gmp.point);

                        // Update ON DB
                        //snmdb.NormalManagerDB.getInstance().add(1,
                        //    new CmdUpdateGuildMemberPoints(gmp),
                        //    DuplaManager.SQLDBResponse,
                        //    this);
                    }

                    // Player 2
                    if (el.p[1] != null)
                    {

                        gmp = new GuildMemberPoints();

                        gmp.guild_uid = el.p[1].m_pi.gi.uid;
                        gmp.member_uid = el.p[1].m_pi.uid;
                        gmp.pang = el.pang_win[1];
                        gmp.point = el.sumScoreP2();

                        // Update ON SERVER
                        el.p[1].m_pi.mi.guild_pang = (long)(el.p[1].m_pi.gi.pang += gmp.pang);
                        el.p[1].m_pi.mi.guild_point = el.p[1].m_pi.gi.point += (uint)gmp.point;

                        //// Update ON DB
                        //snmdb.NormalManagerDB.getInstance().add(1,
                        //    new CmdUpdateGuildMemberPoints(gmp),
                        //    DuplaManager.SQLDBResponse,
                        //    this);
                    }
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[DuplaManager::saveGuildMembersData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void initPacketDuplas(PangyaBinaryWriter _p)
        {

            _p.init_plain(0xBF);

            _p.WriteByte((byte)v_duplas.Count());

            foreach (var el in v_duplas)
            {

                _p.WriteByte(el.numero);
                _p.WriteInt32(el.p[0].m_oid);
                _p.WriteInt32(el.p[1].m_oid);
            }
        }

        public bool finishHoleDupla(PlayerGameInfo _pgi, ushort _seq_hole)
        {

            if (_seq_hole == ushort.MaxValue
                && _seq_hole > 18u
                || _seq_hole == 0u)
            {

                _smp.message_pool.getInstance().push(new message("[DuplaManager::finishHoleDupla][Error] _seq_hole is invalid[VALUE=" + Convert.ToString(_seq_hole) + "]. Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return false;
            }

            var dup = findDuplaByPlayerUID(_pgi.uid);

            if (dup != null)
            {

                var dup_p_index = (dup.p[0] != null && dup.p[0].m_pi.uid == _pgi.uid) ? 0 : 1;
                var other_p_index = (dup_p_index == 0u) ? 1 : 0;

                dup.dados[dup_p_index][_seq_hole - 1].tacada = _pgi.data.tacada_num;
                dup.pang[dup_p_index] = _pgi.data.pang;
                dup.dados[dup_p_index][_seq_hole - 1].finish = true;

                if (dup.state[other_p_index] == Dupla.eSTATE.OUT_GAME)
                {

                    dup.dados[dup_p_index][_seq_hole - 1].score = 2;

                    return true;

                }
                else if (dup.dados[other_p_index][_seq_hole - 1].finish)
                {

                    if (dup.dados[dup_p_index][_seq_hole - 1].tacada < dup.dados[other_p_index][_seq_hole - 1].tacada)
                    {
                        dup.dados[dup_p_index][_seq_hole - 1].score = 2;
                    }
                    else if (dup.dados[dup_p_index][_seq_hole - 1].tacada > dup.dados[other_p_index][_seq_hole - 1].tacada)
                    {
                        dup.dados[other_p_index][_seq_hole - 1].score = 2;
                    }
                    else
                    {
                        dup.dados[dup_p_index][_seq_hole - 1].score = 1;
                        dup.dados[other_p_index][_seq_hole - 1].score = 1;
                    }

                    return true;
                }
            }

            return false;
        }

        protected static void SQLDBResponse(uint _msg_id,
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
                _smp.message_pool.getInstance().push(new message("[DuplaManager::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            switch (_msg_id)
            {
                case 1: // Update Guild Members Points
                    {
                        break;
                    }
                case 0:
                default:
                    break;
            }
        }

        private List<Dupla> v_duplas = new List<Dupla>();
    }
}