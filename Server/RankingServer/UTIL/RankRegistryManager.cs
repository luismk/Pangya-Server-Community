using Pangya_RankingServer.Models;
using Pangya_RankingServer.PacketFunc;
using Pangya_RankingServer.Repository;
using Pangya_RankingServer.Session;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pangya_RankingServer.UTIL
{
    // Found Player typedef 
    using FoundPlayer = Tuple<uint /*Key*/ /*Position do player*/, int /*Value*/ /*Page or -1 error*/>;

    public class RankRegistryManager : IDisposable
    {
        public const uint LIMIT_REGISTRY_FOR_PAGE = 12;

        private static uint NUMBER_OF_PAGE_MENU_REGISTRY(uint __num_registrys)
        {
            return (__num_registrys % LIMIT_REGISTRY_FOR_PAGE) == 0
                ? __num_registrys / LIMIT_REGISTRY_FOR_PAGE
                : (__num_registrys / LIMIT_REGISTRY_FOR_PAGE) + 1;
        }

        public RankRegistryManager()
        {
            this.m_entry = new RankEntry();
            this.m_character_entry = new RankCharacterEntry();
            this.m_state = false;
            // Log
            prex = "";
            dir = "Log";
        }

        public virtual void Dispose()
        {

            clear();

            // Log
            close_log();

            prex = "";
            dir = "";
        }

        public void load()
        {
            if (isLoad())
            {
                clear();
            }

            initialize();
        }

        public bool isLoad()
        {

            bool ret = false;

            try
            {
                ret = (m_state && !m_entry.empty() && !m_character_entry.empty());

            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[RankRegistryManager::isLoad][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        // Coloca a p�gina que o player pediu no packet se ele tiver
        public void pageToPacket(PangyaBinaryWriter _packet, search_dados _sd)
        {

            if (!isLoad())
            {
                throw new exception("[RankRegistryManager::pageToPacket][Error] rank registry manager not loaded, please call load function first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER,
                    2, 0));
            }

            try
            {

                key_menu km = new key_menu(_sd.rank_menu, _sd.rank_menu_item);

                var it = m_entry.find(km);

                if (it.Key != null)
                {

                    // Encontrou
                    var range = getPage(new RankEntry(it.Key, it.Value), ref _sd.page);
                    if (range.Any()
                        && range.Count() > 0)
                    {

                        _packet.WriteUInt32(_sd.page); // P�gina atual
                        _packet.WriteUInt32(NUMBER_OF_PAGE_MENU_REGISTRY((uint)m_entry.FirstOrDefault(c => c.Key == km).Value.Count)); // P�ginas

                        // N�mero de registros
                        _packet.WriteUInt16((ushort)range.Count);///no maximo o count tem que ser 12
                        foreach (var rr in range) // kvEntry: Key=key_position, Value=RankRegistry
                        {
                            rr.toPacket(_packet);
                            if (m_character_entry.TryGetValue(rr.getUID(), out var chrEntry))
                            {
                                chrEntry.playerInfoToPacket(_packet);
                            }
                            else
                            {
                                _smp.message_pool.getInstance().push(new message(
                                    $"[RankRegistryManager::pageToPacket][WARNING] Não tem o Character Info do player[UID={rr.getUID()}], manda valor padrão.",
                                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                                _packet.WriteZeroByte(7);
                            }
                        }
                    }
                    else
                    {
                        _packet.WriteZeroByte(10); // 4 P�gina, 4 P�ginas, e 2 num entrys
                    }

                }
                else
                {
                    _packet.WriteZeroByte(10); // 4 P�gina, 4 P�ginas, e 2 num entrys
                }



            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[RankRegistryManager::pageToPacket][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        // Colocar a posi��o e o valor do player no packet se ele tiver
        public void playerPositionToPacket(PangyaBinaryWriter _packet,
            Player _session,
            search_dados _sd)
        {
            //CHECK_SESSION_BEGIN("playerPositionToPacket");

            if (!isLoad())
            {
                throw new exception("[RankRegistryManager::playerPositionToPacket][Error] rank registry manager not loaded, please call load function first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER,
                    2, 0));
            }

            try
            {

#if _WIN32
					
					
#endif

                key_menu km = new key_menu(_sd.rank_menu, _sd.rank_menu_item);

                var it_entry = m_entry.ToDictionary(c => c.Key == km);

                if (it_entry != null && it_entry.Count > 0)
                {
                    var it = it_entry.Values.FirstOrDefault(_el =>
                    {
                        return _el.Value.First().Value.m_uid == _session.m_pi.uid;
                    });

                    if (it.Key != null)
                    {

                        _packet.WriteByte(ePLAYER_POSITION_RANK_TYPE.PPRT_IN_TOP_RANK);

                        it.Value.First().Value.toPacket(_packet);


                        if (!m_character_entry.TryGetValue(it.Value.First().Value.getUID(), out var it_chr_entry))
                        {

                            // N�o tem character Info do player, WARNING
                            _smp.message_pool.getInstance().push(new message("[RankRegistryManager::playerPositionToPacket][WARNING] Nao tem o Character Info do player[UID=" + Convert.ToString(it.Value.First().Value.getUID()) + "], manda valor padrao para nao da erro no cliente.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            _packet.WriteZeroByte(7); // 1 Level, 2 Unknown, 2 size id, 2 size nickname
                        }
                        else
                        {
                            it_chr_entry.playerInfoToPacket(_packet);
                        }

                    }
                    else
                    {
                        _packet.WriteByte(ePLAYER_POSITION_RANK_TYPE.PPRT_NOT_RANK); // N�o tem registro do player nesse Menu->Item (Rank)
                    }

                }
                else
                {
                    _packet.WriteByte(ePLAYER_POSITION_RANK_TYPE.PPRT_NOT_RANK); // N�o tem registro nesse Menu->Item (Rank)
                }


            }
            catch (exception e)
            {



                _smp.message_pool.getInstance().push(new message("[RankRegistryManager::playerPositionToPacket][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        // Envia o info completo do player com character info e os overall completo
        public void sendPlayerFullInfo(Player _session, uint _uid)
        {
            //CHECK_SESSION_BEGIN("sendPlayerFullInfo");

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {
                if (!isLoad())
                {
                    throw new exception("[RankRegistryManager::sendPlayerFullInfo][Error] rank registry manager not loaded, please call load function first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER,
                        2, 0));
                }

                var it_chr_entry = m_character_entry.find(_uid);

                if (it_chr_entry.Key == 0)
                {
                    throw new exception("[RankRegistryManager::sendPlayerFullInfo][Error] Player[UID=" + Convert.ToString(_session.m_pi.uid) + "] Pediu o info completo do Player[UID=" + Convert.ToString(_uid) + "], mas nao tem o registro de character no Rank.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER,
                        3, 0));
                }

                var all_overall = getAllOverallInfoFromPlayer(_uid);

                if (all_overall.Count == 0)
                {
                    _smp.message_pool.getInstance().push(new message("[RankRegistryManager::sendPlayerFullInfo][Error] Player[UID=" + Convert.ToString(_session.m_pi.uid) + "] pediu o info completo do Player[UID=" + Convert.ToString(_uid) + "], mas nao tem nenhum registro do rank Overall.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                p.init_plain((ushort)0x138A);

                p.WriteByte(0); // OK

                it_chr_entry.Value.playerFullInfoPacket(p);
                it_chr_entry.Value.playerCharacterInfoToPacket(p);

                if (all_overall.Count > 0)
                {
                    p.WriteByte(0); // OK Tem os dados do Rank Overall
                }
                else
                {
                    p.WriteByte(1); // N�o tem os dados do Rank Overall
                }

                foreach (var el in all_overall)
                {
                    el.Value.toCompactPacket(p);
                }

                packet_func.session_send(p,
                    _session, 1);


            }
            catch (exception e)
            {


                _smp.message_pool.getInstance().push(new message("[RankRegistryManager::sendPlayerFullInfo][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain((ushort)0x138A);

                p.WriteByte(1); // Error

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        // Envia a p�gina em que o player procurado foi encontrado
        public void sendPageFoundPlayer(Player _session,
            FoundPlayer _fp,
            search_dados _sd)
        {
            //CHECK_SESSION_BEGIN("sendPageFoundPlayer");

            try
            {


                if (!isLoad())
                {
                    throw new exception("[RankRegistryManager::sendPageFoundPlayer][Error] rank registry manager not loaded, please call load function first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER,
                        2, 0));
                }

                PangyaBinaryWriter p = new PangyaBinaryWriter((ushort)0x138C);

                key_menu km = new key_menu(_sd.rank_menu, _sd.rank_menu_item);

                var it = m_entry.find(km);

                if (it.Key != null)
                {

                    // New Page, Page Found Player
                    _sd.page = (uint)_fp.Item2;

                    // Encontrou
                    var range = getPage(new RankEntry(it.Key, it.Value), ref _sd.page);
                    if (range.Any()
    && range.Count() > 0)
                    {

                        p.WriteByte(0); // OK

                        p.WriteByte(_sd.rank_menu);
                        p.WriteByte(_sd.rank_menu_item);

                        // Op��es descontinuadas no Fresh UP!, por�m ele ainda mant�m nos packet
                        p.WriteByte(_sd.term_s5_type);

                        // Op��es descontinuadas no Fresh UP!, por�m ele ainda mant�m nos packet
                        p.WriteByte(_sd.class_type);

                        p.WriteUInt32(_sd.page); // P�gina atual
                        p.WriteUInt32(NUMBER_OF_PAGE_MENU_REGISTRY((uint)range.Count)); // P�ginas

                        // N�mero de registros
                        p.WriteUInt16((ushort)range.Count);

                        foreach (var it_entry in range)
                        {

                            it_entry.toPacket(p);
                            if (m_character_entry.TryGetValue(it_entry.getUID(), out var it_chr_entry))
                            {
                                it_chr_entry.playerInfoToPacket(p);
                            }
                            else
                            {

                                // N�o tem character Info do player, WARNING
                                _smp.message_pool.getInstance().push(new message("[RankRegistryManager::sendPageFoundPlayer][WARNING] Nao tem o Character Info do player[UID=" + Convert.ToString(it_entry.getUID()) + "], manda valor padrao para nao da erro no cliente.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                p.WriteZeroByte(7); // 1 Level, 2 Unknown, 2 size id, 2 size nickname
                            }
                        }

                        // Player Found position
                        p.WriteUInt16((ushort)_fp.Item1);

                    }
                    else
                    {
                        p.WriteByte(1); // Error
                    }

                }
                else
                {
                    p.WriteByte(1); // Error
                }

                packet_func.session_send(p,
                    _session, 1);


            }
            catch (exception e)
            {


                _smp.message_pool.getInstance().push(new message("[RankRegistryManager::sendPageFoundPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Relan�a a exception por que quem d� a resposta de error para o cliente � quem chamou essa fun��o
                throw;
            }
        }

        // Procura um player pelo nickname e enviar a p�gina onde ele est� se ele estiver no rank
        public void searchPlayerByNicknameAndSendPage(Player _session,
            string _nickname,
            search_dados _sd)
        {
            //CHECK_SESSION_BEGIN("searchPlayerByNicknameAndSendPage");

            try
            {


                if (!isLoad())
                {
                    throw new exception("[RankRegistryManager::searchPlayerByNicknameAndSendPage][Error] rank registry manager not loaded, please call load function first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER,
                        2, 0));
                }

                if (_nickname.Length == 0)
                {
                    throw new exception("[RankRegistryManager::searchPlayerByNicknameAndSendPage][Error] Player[UID=" + Convert.ToString(_session.m_pi.uid) + "] esta procurando por um player, mas _nickname is invalid(empty). Search Rank:{\n" + _sd.toString() + "\n}.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER,
                        6, 0));
                }

                var found_player = searchPlayerByNickname(_nickname, _sd);

                if (found_player.Item2 == -1)
                {
                    throw new exception("[RankRegistryManager::searchPlayerByNicknameAndSendPage][Error] Player[UID=" + Convert.ToString(_session.m_pi.uid) + "] esta procurando por um player, mas nao encontrou o player[NICKNAME=" + _nickname + "] no rank. Search Rank:{\n" + _sd.toString() + "\n}.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER,
                        8, 0));
                }

                // Send Page
                sendPageFoundPlayer(_session,
                    found_player, _sd);



            }
            catch (exception e)
            {



                _smp.message_pool.getInstance().push(new message("[RankRegistryManager::searchPlayerByNicknameAndSendPage][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Relan�a que quem manda a resposta de erro para o cliente � a quem chamou essa fun��o
                throw;
            }
        }

        // Procura um player pela position e enviar a p�gina onde ele est� se ele estiver no rank
        public void searchPlayerByRankAndSendPage(Player _session,
            uint _position,
            search_dados _sd)
        {
            //CHECK_SESSION_BEGIN("searchPlayerByRankAndSendPage");

            try
            {



                if (!isLoad())
                {
                    throw new exception("[RankRegistryManager::searchPlayerByRankAndSendPage][Error] rank registry manager not loaded, please call load function first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER,
                        2, 0));
                }

                var found_player = searchPlayerByRank(_position, _sd);

                if (found_player.Item2 == -1)
                {
                    throw new exception("[RankRegistryManager::searchPlayerByRankAndSendPage][Error] Player[UID=" + Convert.ToString(_session.m_pi.uid) + "] esta procurando por um player, mas nao encontrou o player[POSITION=" + Convert.ToString(_position) + "] no rank. Search Rank:{\n" + _sd.toString() + "\n}.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER,
                        8, 0));
                }

                // Send Page
                sendPageFoundPlayer(_session,
                    found_player, _sd);



            }
            catch (exception e)
            {


                _smp.message_pool.getInstance().push(new message("[RankRegistryManager::searchPlayerByRankAndSendPage][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Relan�a que quem manda a resposta de erro para o cliente � a quem chamou essa fun��o
                throw;
            }
        }

        // Procura um player por nickname no Rank Menu->Item
        public FoundPlayer searchPlayerByNickname(string _nickname, search_dados _sd)
        {
            FoundPlayer ret = new FoundPlayer(0u, -1);

            try
            {



                if (!isLoad())
                {
                    throw new exception("[RankRegistryManager::searchPlayerByNickname][Error] rank registry manager not loaded, please call load function first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER,
                        2, 0));
                }

                if (_nickname.Length == 0)
                {
                    throw new exception("[RankRegistryManager::searchPlayerByNickname][Error] Search Rank:{\n" + _sd.toString() + "\n} _nickname is invalid(empty).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER,
                        6, 0));
                }

                // Find player By nickname, sem Case sensitive
                var it_player = m_character_entry.FirstOrDefault(_el =>
                {
                    return _el.Value.getNickname() != null && string.Compare(_el.Value.getNickname(), _nickname) == 0;
                });

                if (it_player.Key == 0)
                {



                    return ret; // N�o tem o player nos registros com esse nickname
                }
                key_menu km = new key_menu(_sd.rank_menu, _sd.rank_menu_item);

                if (!m_entry.TryGetValue(km, out var it_map))
                {
                    return ret; // Não tem nenhum registro nesse Rank Menu->Item
                }

                if (it_map.First().Value.getUID() != it_player.Value.getUID())
                {



                    return ret;// N�o tem registro do player no Rank Menu->item
                }

                // Calcule Page
                // Como it_map é RankRegistry único, é o próprio registro
                var it_entry = it_map;

                ret = new FoundPlayer((it_entry.First().Value.getCurrentPosition() - 1) % LIMIT_REGISTRY_FOR_PAGE, 0);


            }
            catch (exception e)
            {



                _smp.message_pool.getInstance().push(new message("[RankRegistryManager::searchPlayerByNickname][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Error
                ret = new FoundPlayer(0, -1);
            }

            return ret;
        }

        // Procura um player pelo rank dele no Rank Menu->Item
        public FoundPlayer searchPlayerByRank(uint _position, search_dados _sd)
        {
            FoundPlayer ret = new FoundPlayer(0u, -1);

            try
            {

#if _WIN32
					
					
#endif

                if (!isLoad())
                {
                    throw new exception("[RankRegistryManager::searchPlayerByRank][Error] rank registry manager not loaded, please call load function first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER,
                        2, 0));
                }

                key_menu km = new key_menu(_sd.rank_menu, _sd.rank_menu_item);
                var it_map = m_entry.ToDictionary(c => c.Key == km);

                if (it_map.Count == 0)
                {


                    return ret; // N�o tem nenhum registro nesse Rank Menu->Item
                }

                if (_position > it_map.Values.Count)
                {
                    throw new exception("[RankRegistryManager::searchPlayerByRank][Error] Search Rank:{\n" + _sd.toString() + "\n} _position eh maior que o numeros de registros do Rank. POSITION=" + Convert.ToString(_position) + ", NUM_REGISTROS=" + Convert.ToString(it_map.Values.Count()), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER,
                        7, 0));
                }

                var it_entry = it_map.Values.FirstOrDefault(_el =>
                {
                    return _el.Value.First().Value.getCurrentPosition() == _position;
                });

                if (it_entry.Key == null)
                { 
                    return ret; // N�o tem registro do player no Rank Menu->item
                }

                // Calcule Page
                var diff = it_map.Values.Count;

                ret = new FoundPlayer((it_entry.Value.First().Value.getCurrentPosition() - 1) % LIMIT_REGISTRY_FOR_PAGE/*// Possition relative at page*/, (int)(diff / LIMIT_REGISTRY_FOR_PAGE));

            }
            catch (exception e)
            {



                _smp.message_pool.getInstance().push(new message("[RankRegistryManager::searchPlayerByRank][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Error 
                ret = new FoundPlayer(0, -1);
            }

            return ret;
        }

        // Cria log de todos os registro em uma arquivo com data
        public void makeLog()
        {

            try
            {

                init_log();



                putLog("------------------------------------------------ Player Log -----------------------------------------\n");

                foreach (var el in m_character_entry)
                {
                    putLog("Player [UID=" + Convert.ToString(el.Value.getUID()) + ", ID=" + el.Value.getId() + ", NICKNAME=" + el.Value.getNickname() + ", LEVEL=" + Convert.ToString(el.Value.getLevel()) + "] CHARACTER[TYPEID=" + Convert.ToString(el.Value.getCharacterInfo()._typeid) + ", ID=" + Convert.ToString(el.Value.getCharacterInfo().id) + "] equiped.");
                }

                putLog("----------------------------------------------- Rank Log ---------------------------------------------\n");

                foreach (var el in m_entry)
                {

                    putLog("******************************************* Rank Menu[" + Convert.ToString((ushort)el.Key.m_menu) + "] Item[" + Convert.ToString((ushort)el.Key.m_item) + "] **************************************\n");

                    foreach (var el2 in m_entry.Values)
                    {
                        putLog("Player[UID=" + Convert.ToString(el2.First().Value.getUID()) + "] RANK[CURRENT=" + Convert.ToString(el2.First().Value.getCurrentPosition()) + ", LAST=" + Convert.ToString(el2.First().Value.getLastPosition()) + ", VALUE=" + Convert.ToString(el2.First().Value.getValue()) + "]");
                    }
                }



                close_log(); 
            }
            catch (exception e)
            {


                close_log();

                _smp.message_pool.getInstance().push(new message("[RankRegistryManager::makeLog][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void initialize()
        {
            m_state = true;
            try
            {
                CmdRankRegistryInfo cmd_rri = new CmdRankRegistryInfo(true); // Waiter

                snmdb.NormalManagerDB.getInstance().add(0,
                    cmd_rri, null, null);

                if (cmd_rri.getException().getCodeError() != 0)
                {
                    throw cmd_rri.getException();
                }

                m_entry = cmd_rri.getInfo();

                if (m_entry.empty())
                {
                    m_state = false;
                    return;
                }

                CmdRankRegistryCharacterInfo cmd_rrci = new CmdRankRegistryCharacterInfo(true); // Waiter

                snmdb.NormalManagerDB.getInstance().add(0,
                    cmd_rrci, null, null);

                if (cmd_rrci.getException().getCodeError() != 0)
                {
                    throw cmd_rrci.getException();
                }

                m_character_entry = cmd_rrci.getInfo();

                if (m_character_entry.empty())
                {
                     m_state = false;
                    return;
                } 
            }
            catch (exception e)
            {


                m_state = false;

                _smp.message_pool.getInstance().push(new message("[RankRegistryManager::initialize][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        protected void clear()
        {

            try
            {



                if (isLoad())
                {
                    m_entry.Clear();
                }

                m_state = false;


            }
            catch (exception e)
            {


                _smp.message_pool.getInstance().push(new message("[RankRegistryManager::clear][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        protected SortedDictionary<eRANK_OVERALL, RankRegistry> getAllOverallInfoFromPlayer(uint uid)
        {
            var v_rcr = new SortedDictionary<eRANK_OVERALL, RankRegistry>();

            try
            {
                lock (m_cs)
                {
                    if (!isLoad())
                    {
                        throw new exception(
                            "[RankRegistryManager::GetAllOverallInfoFromPlayer][Error] rank registry manager not loaded, please call load function first.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER, 2, 0));
                    }

                    for (var menu_item = eRANK_OVERALL.RO_TOTAL_POINTS;
                         menu_item <= eRANK_OVERALL.RO_ACHIEVEMENT_POINTS;
                         EnumOperator.ENUM_OPERATOR_PLUS_PLUS(ref menu_item))
                    {
                        // tenta achar o menu
                        if (m_entry.TryGetValue(new key_menu(eRANK_MENU.RM_OVERALL, (byte)menu_item), out var entry))
                        {
                            // procura player dentro do RankEntryValue
                            var found = entry.Values.FirstOrDefault(c => c.getUID() == (uid));

                            if (found != null)
                                v_rcr.Add(menu_item, found);
                            else
                                v_rcr.Add(menu_item, new RankRegistry());
                        }
                        else
                        {
                            // não existe o item -> adiciona vazio
                            v_rcr.Add(menu_item, new RankRegistry());
                        }
                    }
                }
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message(
                    "[RankRegistryManager::GetAllOverallInfoFromPlayer][ErrorSystem] " + e.getFullMessageError(),
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return new SortedDictionary<eRANK_OVERALL, RankRegistry>(v_rcr);
        }


        protected RankEntryValueRange getPage(RankEntry _registrys, ref uint _page)
        {
            RankEntryValueRange ret = new RankEntryValueRange();

            try
            {
                if (!isLoad())
                    throw new exception("[RankRegistryManager::getPage][Error] rank registry manager not loaded.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER, 2, 0));

                if (_registrys == null || _registrys.Count == 0)
                    throw new exception("[RankRegistryManager::getPage][Error] _registrys is invalid.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER, 4, 0));

                var num_registrys = _registrys.Values.FirstOrDefault().Values.Count;
                var firstEntry = _registrys.First();

                var num_pages = NUMBER_OF_PAGE_MENU_REGISTRY((uint)num_registrys);

                if (num_pages == 0)
                    throw new exception($"[RankRegistryManager::getPage][Error] No records in RANK_SERVER[MENU={firstEntry.Key.m_menu}, ITEM={firstEntry.Key.m_item}, PAGES={num_pages}, REGISTRYS={num_registrys}]",
                        (uint)ExceptionError.STDA_MAKE_ERROR((uint)STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER, 5, 0));

                if ((_page + 1) > num_pages)
                { 
                    _page = num_pages - 1;
                }

                var allRecords = _registrys.Values.SelectMany(innerDict => innerDict.Values).ToList();

                int first_el = (int)(_page * LIMIT_REGISTRY_FOR_PAGE);
                int last_el = first_el + (int)Math.Min(LIMIT_REGISTRY_FOR_PAGE, allRecords.Count - first_el);

                var pageValues = allRecords.Skip(first_el).Take(last_el - first_el).ToList();

                ret = new RankEntryValueRange(pageValues);
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(
                    new message("[RankRegistryManager::getPage][ErrorSystem] " + e.getFullMessageError(),
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }


        protected StreamWriter log;

        protected string prex = "";
        protected string dir = "";

        protected void init_log()
        {

            try
            {

                try
                {

                    // Dir do arquivo .ini ou padr�o se n�o tiver
                    var ini = new IniHandle("server.ini");

                    string tmp_dir = ini.ReadString("LOG", "DIR");

                    if (!string.IsNullOrWhiteSpace(tmp_dir))
                    {
                        if (Directory.Exists(tmp_dir))
                        {
                            dir = tmp_dir;
                        }
                        else
                        {
                            Directory.CreateDirectory(tmp_dir);
                            dir = tmp_dir;
                        }
                    }
                    else
                    {
                        throw new exception("[RankRegistryManager::init_log][Error] O diretorio do Arquivo .ini esta vazio.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER,
                            5001, 0));
                    }

                }
                catch (exception e)
                {

                    _smp.message_pool.getInstance().push(new message("[RankRegistryManager::init_log][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Log
                    _smp.message_pool.getInstance().push(new message(@"[RankRegistryManager::init_log][Log] Nao consguiu pegar o diretorio do Arquivo .ini, usando o diretorio padrao ""Log"".", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // N�o tem o log dir do arquivo ini, usa o padr�o
                    // Verifica se diret�rio padr�o est� criado, se n�o cria ele
                    dir = "Log";
                    DirectoryInfo directory = null;

                    if (!Directory.Exists(dir))
                        directory = Directory.CreateDirectory(dir);

                    if (directory == null)
                    {
                        _smp.message_pool.getInstance().push(new message("[RankRegistryManager::init_log][Error] Nao conseguiu criar o diretorio[" + dir + "] padrao.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }

                close_log();

                // Cria timestamp
                string datetime = DateTime.Now.ToString("ddMMyyyyHHmmss");
                if (!string.IsNullOrEmpty(prex))
                    datetime += " " + prex;

                string fullPath = Path.Combine(dir, $"log Registros {datetime}.log");

                // Cria FileStream para log
                log = new StreamWriter(new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.Read));
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[RankRegistryManager::init_log][ErrorSystem] + " + (e.getFullMessageError()), type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
        }

        protected void close_log()
        {

            try
            {

                try
                {

                    if (log != null)
                    {
                        log.Close();
                    }

                }
                catch (exception e)
                {

                    _smp.message_pool.getInstance().push(new message("[RankRegistryManager::close_log][Error][" + (e.getFullMessageError()) + "].", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[RankRegistryManager::close_log][ErrorSystem] + " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        protected void putLog(string _str_log)
        {

            try
            {

                if (log == null)
                    init_log();

                if (log != null)
                {
                    log.WriteLine(_str_log);
                    log.Flush(); // força escrita no disco
                }

            }
            catch (ObjectDisposedException)
            {
                _smp.message_pool.getInstance().push(new message(
                    "[RankRegistryManager::PutLog][Error] Stream já foi fechado.",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message(
                    $"[RankRegistryManager::PutLog][Error] {e.Message} Erro no arquivo de log.",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        protected RankEntry m_entry = new RankEntry();
        protected RankCharacterEntry m_character_entry = new RankCharacterEntry();

        protected bool m_state;

        protected object m_cs = new object();

    }

    public class sRankRegistryManager : Singleton<Pangya_RankingServer.UTIL.RankRegistryManager>
    {

    } 
}
 