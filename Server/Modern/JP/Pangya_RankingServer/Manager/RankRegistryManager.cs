using Pangya_RankingServer.Flags;
using Pangya_RankingServer.Models;
using Pangya_RankingServer.Repository;
using Pangya_RankingServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_RankingServer.Manager
{
    // Found Player typedef 
    using FoundPlayer = Tuple<uint /*Key*/ /*Position do player*/, int /*Value*/ /*Page or -1 error*/>;

    public class RankRegistryManager
    {
        public const uint LIMIT_REGISTRY_FOR_PAGE = 12;
        protected StreamWriter log;
        protected string prex = "";
        protected string dir = "";
        protected RankEntry m_entry = new RankEntry();
        protected RankCharacterEntry m_character_entry = new RankCharacterEntry();
        protected bool m_state;
        protected object m_cs = new object();
        uint NUMBER_OF_PAGE_MENU_REGISTRY(uint __num_registrys)
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
         
        public void load()
        {
            // Se já estiver carregado, limpa os dados antigos antes de reinicializar
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
                ret = (m_state && m_entry.Any() && m_character_entry.Any());
            }
            catch (exception e)
            {
                // Log padronizado com o Name da classe para rastreamento de erro no carregamento
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[{nameof(RankRegistryManager)}] [isLoad][ErrorSystem] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }
        // Coloca a página que o player pediu no packet se ele tiver
        public void pageToPacket(Packet _packet, SearchData _sd)
        {
            try
            {
                if (!isLoad())
                {
                    throw new exception($"[{nameof(RankRegistryManager)}] rank registry manager not loaded.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER, 2, 0));
                }

                KeyMenu km = new KeyMenu(_sd.rank_menu, _sd.rank_menu_item);

                // Busca o ranking solicitado (O(1))
                if (m_entry.TryGetValue(km, out var rankMap) && rankMap != null)
                {
                    // Obtém o range de registros para a página solicitada
                    var range = getPage(new RankEntry(km, rankMap), ref _sd.page);

                    if (range != null && range.Count > 0)
                    {
                        _packet.WriteUInt32(_sd.page); // Página atual

                        // Calcula o total de páginas baseado no total de registros deste ranking específico
                        _packet.WriteUInt32(NUMBER_OF_PAGE_MENU_REGISTRY((uint)rankMap.Count));

                        // Número de registros (Geralmente limitado a 12 no Pangya)
                        _packet.WriteUInt16((ushort)range.Count);

                        foreach (var rr in range)
                        {
                            // Dados do Ranking (Posição, Score, etc)
                            rr.toPacket(_packet);

                            // Dados do Personagem (Nickname, Guild, etc)
                            if (m_character_entry.TryGetValue(rr.getUID(), out var chrEntry))
                            {
                                chrEntry.playerInfoToPacket(_packet);
                            }
                            else
                            {
                                _smp.LogManager.Instance.push(new AppMessage(
                                    $"[{nameof(RankRegistryManager)}] [WARNING] Sem Character Info para UID: {rr.getUID()}. Preenchendo com zeros.",
                                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                                _packet.WriteZero(7); // 1 Level, 2 Unknown, 2 size Login, 2 size NickName
                            }
                        }
                    }
                    else
                    {
                        // Página vazia ou fora do range
                        _packet.WriteZero(10); // 4 Atual, 4 Total, 2 Num Entries
                    }
                }
                else
                {
                    // Ranking não encontrado
                    _packet.WriteZero(10); // 4 Atual, 4 Total, 2 Num Entries
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[{nameof(RankRegistryManager)}] [ErrorSystem] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        // Colocar a posição e o valor do player no packet se ele estiver no ranking
        public void playerPositionToPacket(Packet _packet, Player _session, SearchData _sd)
        {
            try
            {
                if (!isLoad())
                {
                    throw new exception($"[{nameof(RankRegistryManager)}] rank registry manager not loaded.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER, 2, 0));
                }

                KeyMenu km = new KeyMenu(_sd.rank_menu, _sd.rank_menu_item);

                // Busca o dicionário do ranking específico (O(1))
                if (m_entry.TryGetValue(km, out var rankMap) && rankMap != null && rankMap.Count > 0)
                {
                    // Localiza o registro do próprio player dentro deste ranking
                    // Nota: Se você tiver um dicionário reverso de UID para Posição, isso ficaria ainda mais rápido
                    var playerRecord = rankMap.Values.FirstOrDefault(r => r.getUID() == _session.UserInfo.UID);

                    if (playerRecord != null)
                    {
                        // Status: Player está no ranking
                        _packet.WriteByte((byte)Player_Pos_Rank_Type.PPRT_IN_TOP_RANK);

                        // Escreve dados da posição (Rank, Score, etc)
                        playerRecord.toPacket(_packet);

                        // Tenta buscar as informações visuais do personagem
                        if (m_character_entry.TryGetValue(playerRecord.getUID(), out var it_chr_entry))
                        {
                            it_chr_entry.playerInfoToPacket(_packet);
                        }
                        else
                        {
                            _smp.LogManager.Instance.push(new AppMessage(
                                $"[{nameof(RankRegistryManager)}] [WARNING] Sem Character Info para UID: {playerRecord.getUID()}. Preenchendo com zeros.",
                                type_msg.CL_FILE_LOG_AND_CONSOLE));

                            _packet.WriteZero(7); // 1 Level, 2 Unknown, 2 size Login, 2 size NickName
                        }
                    }
                    else
                    {
                        // Player não está ranqueado neste Menu/Item
                        _packet.WriteByte((byte)Player_Pos_Rank_Type.PPRT_NOT_RANK);
                    }
                }
                else
                {
                    // Ranking vazio ou inexistente
                    _packet.WriteByte((byte)Player_Pos_Rank_Type.PPRT_NOT_RANK);
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[{nameof(RankRegistryManager)}] [ErrorSystem] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        // Envia o info completo do player com character info e os overall completo
        public void sendPlayerFullInfo(Player _session, uint _uid)
        {
            Packet p = new Packet();

            try
            {
                // 1. Validação de carga do Manager
                if (!isLoad())
                {
                    throw new exception($"[{nameof(RankRegistryManager)}] rank registry manager not loaded.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER, 2, 0));
                }

                // 2. Busca as informações de personagem (Character Entry)
                if (!m_character_entry.TryGetValue(_uid, out var it_chr_entry) || it_chr_entry == null)
                {
                    throw new exception($"[{nameof(RankRegistryManager)}] Player[UID={_session.UserInfo.UID}] pediu info do Player[UID={_uid}], mas nao existe registro de character no Rank.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER, 3, 0));
                }

                // 3. Busca os dados de Overall do jogador
                var all_overall = getAllOverallInfoFromPlayer(_uid);

                if (all_overall == null || all_overall.Count == 0)
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[{nameof(RankRegistryManager)}] [WARNING] Player[UID={_session.UserInfo.UID}] pediu info do Player[UID={_uid}], mas nao ha registros Overall.",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                // 4. Montagem do Pacote 0x138A
                p.init_plain(0x138A);
                p.WriteByte(0); // Status OK para a busca de personagem

                it_chr_entry.playerFullInfoPacket(p);
                it_chr_entry.playerCharacterInfoToPacket(p);

                // 5. Seção de dados Overall
                if (all_overall != null && all_overall.Count > 0)
                {
                    p.WriteByte(0); // OK: Tem dados de Overall
                    foreach (var el in all_overall.Values)
                    {
                        el.toCompactPacket(p);
                    }
                }
                else
                {
                    p.WriteByte(1); // Não possui registros de Overall para enviar
                }

                _session.Send(p);
            }
            catch (exception e)
            {
                // Log de erro formatado
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[{nameof(RankRegistryManager)}] [ErrorSystem] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta de Erro para o Cliente
                p.init_plain(0x138A);
                p.WriteByte(1); // Error Status
                _session.Send(p);
            }
        }

        // Envia a página em que o player procurado foi encontrado
        public void sendPageFoundPlayer(Player _session, FoundPlayer _fp, SearchData _sd)
        {
            try
            {
                if (!isLoad())
                {
                    throw new exception($"[{nameof(RankRegistryManager)}] rank registry manager not loaded.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER, 2, 0));
                }

                // 0x138C - Resposta de Busca no Ranking
                Packet p = new Packet(0x138C);
                KeyMenu km = new KeyMenu(_sd.rank_menu, _sd.rank_menu_item);

                // Busca o ranking solicitado no dicionário m_entry
                if (m_entry.TryGetValue(km, out var rankMap) && rankMap != null)
                {
                    // Atualiza a página no SearchData com a página onde o player foi encontrado
                    _sd.page = (uint)_fp.Item2;

                    // Obtém o range de registros para a página específica
                    var range = getPage(new RankEntry(km, rankMap), ref _sd.page);

                    if (range != null && range.Count > 0)
                    {
                        p.WriteByte(0); // Status OK

                        // Cabeçalho do Packet
                        p.WriteByte(_sd.rank_menu);
                        p.WriteByte(_sd.rank_menu_item);
                        p.WriteByte(_sd.term_s5_type); // Descontinuado no Fresh Up!, mas mantido no packet
                        p.WriteByte(_sd.class_type);   // Descontinuado no Fresh Up!, mas mantido no packet

                        p.WriteUInt32(_sd.page); // Página atual
                        p.WriteUInt32(NUMBER_OF_PAGE_MENU_REGISTRY((uint)rankMap.Count)); // Total de páginas baseada no total do rank
                        p.WriteUInt16((ushort)range.Count); // Número de registros nesta página

                        foreach (var it_entry in range)
                        {
                            // Escreve dados do Ranking (Posição, Score, etc)
                            it_entry.toPacket(p);

                            // Tenta escrever os dados visuais do personagem (Nickname, Rank, etc)
                            if (m_character_entry.TryGetValue(it_entry.getUID(), out var it_chr_entry))
                            {
                                it_chr_entry.playerInfoToPacket(p);
                            }
                            else
                            {
                                // Se não encontrar informações do char, loga o aviso e preenche com zeros para não quebrar o cliente
                                _smp.LogManager.Instance.push(new AppMessage(
                                    $"[{nameof(RankRegistryManager)}] [WARNING] Sem Character Info para UID: {it_entry.getUID()}. Preenchendo com zeros.",
                                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                                p.WriteZero(7); // 1 Level, 2 Unknown, 2 size Login, 2 size NickName
                            }
                        }

                        // Escreve a posição relativa na página (onde o cursor/seleção deve ficar)
                        p.WriteUInt16((ushort)_fp.Item1);
                    }
                    else
                    {
                        p.WriteByte(1); // Erro: Range de página vazio
                    }
                }
                else
                {
                    p.WriteByte(1); // Erro: Ranking não encontrado
                }

                _session.Send(p);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[{nameof(RankRegistryManager)}] [ErrorSystem] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Relança para que o chamador possa tratar se necessário
                throw;
            }
        }

        // Procura um player pelo NickName e enviar a página onde ele está se ele estiver no rank
        public void searchPlayerByNicknameAndSendPage(Player _session, string _nickname, SearchData _sd)
        {
            try
            {
                // 1. Validação de carregamento
                if (!isLoad())
                {
                    throw new exception($"[{nameof(RankRegistryManager)}] rank registry manager not loaded.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER, 2, 0));
                }

                // 2. Validação do input
                if (string.IsNullOrWhiteSpace(_nickname))
                {
                    throw new exception($"[{nameof(RankRegistryManager)}] Player[UID={_session.UserInfo.UID}] tentou buscar, mas o NickName esta vazio.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER, 6, 0));
                }

                // 3. Tenta localizar o player no ranking
                var found_player = searchPlayerByNickname(_nickname, _sd);

                // Se o Item2 (PageIndex) for -1, o player não foi encontrado no rank especificado
                if (found_player.Item2 == -1)
                {
                    throw new exception($"[{nameof(RankRegistryManager)}] Player[UID={_session.UserInfo.UID}] nao encontrou o player[NICKNAME={_nickname}] no rank. SearchData: {_sd.toString()}",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER, 8, 0));
                }

                // 4. Envia a página encontrada para o cliente
                sendPageFoundPlayer(_session, found_player, _sd);

                // Log informativo para o console
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[{nameof(RankRegistryManager)}] Player[UID={_session.UserInfo.UID}] localizou Nickname '{_nickname}' na pagina {found_player.Item2}.",
                    type_msg.CL_ONLY_CONSOLE));
            }
            catch (exception e)
            {
                // Log de erro formatado
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[{nameof(RankRegistryManager)}] [ErrorSystem] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Relança para o Handle (0x138C)
                throw;
            }
        }

        // Procura um player pela position e enviar a página onde ele está se ele estiver no rank
        public void searchPlayerByRankAndSendPage(Player _session, uint _position, SearchData _sd)
        {
            try
            {
                // 1. Validação de carga do Manager
                if (!isLoad())
                {
                    throw new exception($"[{nameof(RankRegistryManager)}] rank registry manager not loaded.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER, 2, 0));
                }

                // 2. Tenta localizar o player no ranking pela posição
                // Retorna Tuple ou struct FoundPlayer (PosOnPage, PageIndex)
                var found_player = searchPlayerByRank(_position, _sd);

                // Item2 (PageIndex) ser -1 indica que não foi encontrado
                if (found_player.Item2 == -1)
                {
                    throw new exception($"[{nameof(RankRegistryManager)}] Player[UID={_session.UserInfo.UID}] procurando por posicao, mas nao encontrou [POSITION={_position}]. SearchData: {_sd.toString()}",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER, 8, 0));
                }

                // 3. Envia a página encontrada para o cliente
                sendPageFoundPlayer(_session, found_player, _sd);

                // Log de sucesso opcional para o console
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[{nameof(RankRegistryManager)}] Player[UID={_session.UserInfo.UID}] encontrou posicao {_position} na pagina {found_player.Item2}.",
                    type_msg.CL_ONLY_CONSOLE));
            }
            catch (exception e)
            {
                // Log de erro centralizado
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[{nameof(RankRegistryManager)}] [ErrorSystem] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Relança para o Handle tratar a resposta de erro (Packet 0x138C)
                throw;
            }
        }

        // Procura um player por NickName no Rank Menu->Item
        public FoundPlayer searchPlayerByNickname(string _nickname, SearchData _sd)
        {
            // Padrão de retorno: Pos 0, Page -1 (indica não encontrado ou erro)
            FoundPlayer ret = new FoundPlayer(0u, -1);

            try
            {
                // 1. Validações Iniciais
                if (!isLoad())
                {
                    throw new exception($"[{nameof(RankRegistryManager)}] rank registry manager not loaded.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER, 2, 0));
                }

                if (string.IsNullOrWhiteSpace(_nickname))
                {
                    throw new exception($"[{nameof(RankRegistryManager)}] _nickname is invalid(empty). SearchData: {_sd.toString()}",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER, 6, 0));
                }

                // 2. Localiza o Player pelo Nickname (Case-Insensitive)
                // Dica: Se m_character_entry for muito grande, considere um Dictionary<string, Player> para O(1)
                var playerEntry = m_character_entry.Values.FirstOrDefault(p =>
                    string.Equals(p.getNickname(), _nickname, StringComparison.OrdinalIgnoreCase));

                if (playerEntry == null)
                {
                    return ret; // Player não existe no cache de personagens
                }

                // 3. Localiza o Rank solicitado
                KeyMenu km = new KeyMenu(_sd.rank_menu, _sd.rank_menu_item);

                if (!m_entry.TryGetValue(km, out var rankMap) || rankMap == null || rankMap.Count == 0)
                {
                    return ret; // Rank vazio ou inexistente
                }

                // 4. Localiza o registro do player específico dentro deste Rank
                // Aqui assumimos que rankMap contém a lista de posições para aquele menu
                var playerRankRecord = rankMap.Values.FirstOrDefault(r => r.getUID() == playerEntry.getUID());

                if (playerRankRecord == null)
                {
                    return ret; // O player existe, mas não tem pontuação/registro neste ranking específico
                }

                // 5. Cálculo da Página e Posição Relativa
                // Ex: Posição 15, Limite 10 -> Página 1, Index 4 (0-based)
                uint currentPos = playerRankRecord.getCurrentPosition();

                uint page = (currentPos - 1) / LIMIT_REGISTRY_FOR_PAGE;
                uint posOnPage = (currentPos - 1) % LIMIT_REGISTRY_FOR_PAGE;

                ret = new FoundPlayer(posOnPage, (int)page);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[{nameof(RankRegistryManager)}] [ErrorSystem] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                ret = new FoundPlayer(0, -1);
            }
            catch (Exception ex)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[{nameof(RankRegistryManager)}] [CriticalError] {ex.Message}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        // Procura um player pelo rank dele no Rank Menu->Item
        public FoundPlayer searchPlayerByRank(uint _position, SearchData _sd)
        {
            FoundPlayer ret = new FoundPlayer(0u, -1);

            try
            {
                // 1. Validação de carregamento
                if (!isLoad())
                {
                    throw new exception($"[{nameof(RankRegistryManager)}] rank registry manager not loaded.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER, 2, 0));
                }

                // 2. Busca o Rank Menu (Dicionário de registros deste Ranking)
                KeyMenu km = new KeyMenu(_sd.rank_menu, _sd.rank_menu_item);

                if (!m_entry.TryGetValue(km, out var rankMap) || rankMap == null || rankMap.Count == 0)
                {
                    return ret; // O Ranking solicitado não possui dados
                }

                // 3. Busca Direta pela Posição usando KeyPosition (O(1))
                // Criamos a chave de busca baseada na posição solicitada
                var keyPos = new KeyPosition(_position);

                if (!rankMap.TryGetValue(keyPos, out var rankRecord) || rankRecord == null)
                {
                    // Se não encontrou a chave da posição, o registro não existe
                    return ret;
                }

                // 4. Cálculo da Página e Index (0-based)
                // Usamos a posição confirmada no registro encontrado
                uint currentPos = rankRecord.getCurrentPosition();

                uint page = (currentPos - 1) / LIMIT_REGISTRY_FOR_PAGE;
                uint posOnPage = (currentPos - 1) % LIMIT_REGISTRY_FOR_PAGE;

                ret = new FoundPlayer(posOnPage, (int)page);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[{nameof(RankRegistryManager)}] [ErrorSystem] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                ret = new FoundPlayer(0, -1);
            }
            catch (Exception ex)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[{nameof(RankRegistryManager)}] [CriticalError] {ex.Message}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
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

                _smp.LogManager.Instance.push(new AppMessage("[RankRegistryManager::makeLog][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public async void initialize()
        {
            m_state = true;
            try
            {
                CmdRankRegistryInfo cmd_rri = new CmdRankRegistryInfo();

                snmdb.NormalManagerDB.Instance.add(0,
                     cmd_rri, null, null);

                if (cmd_rri.getException().getCodeError() != 0)
                {
                    throw cmd_rri.getException();
                }

                m_entry = cmd_rri.getInfo();

                if (m_entry.Count() == 0)
                {
                    m_state = false;
                    return;
                }

                CmdRankRegistryCharacterInfo cmd_rrci = new CmdRankRegistryCharacterInfo();

                snmdb.NormalManagerDB.Instance.add(0,
                     cmd_rrci, null, null);

                if (cmd_rrci.getException().getCodeError() != 0)
                {
                    throw cmd_rrci.getException();
                }

                m_character_entry = cmd_rrci.getInfo();

                if (!m_character_entry.Any())
                {
                    m_state = false;
                    return;
                }
            }
            catch (exception e)
            {


                m_state = false;

                _smp.LogManager.Instance.push(new AppMessage("[RankRegistryManager::initialize][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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
                _smp.LogManager.Instance.push(new AppMessage("[RankRegistryManager::clear][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        protected SortedDictionary<Rank_Type, RankRegistry> getAllOverallInfoFromPlayer(uint uid)
        {
            var v_rcr = new SortedDictionary<Rank_Type, RankRegistry>();

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

                    for (var menu_item = Rank_Type.RO_TOTAL_POINTS;
                         menu_item <= Rank_Type.RO_ACHIEVEMENT_POINTS;
                         EnumOperator.ENUM_OPERATOR_PLUS_PLUS(ref menu_item))
                    {
                        // tenta achar o menu
                        if (m_entry.TryGetValue(new KeyMenu(RankMenuFlags.RM_OVERALL, (byte)menu_item), out var entry))
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
                _smp.LogManager.Instance.push(new AppMessage(
                    "[RankRegistryManager::GetAllOverallInfoFromPlayer][ErrorSystem] " + e.getFullMessageError(),
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return v_rcr;
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
                _smp.LogManager.Instance.push(
                    new AppMessage("[RankRegistryManager::getPage][ErrorSystem] " + e.getFullMessageError(),
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }


        protected void init_log()
        {
            try
            {
                try
                {
                    // Tenta ler o diretório do log no arquivo .ini
                    var ini = new IniHandle("Log.ini");
                    string tmp_dir = ini.ReadString("LOG", "DIR");

                    if (!string.IsNullOrWhiteSpace(tmp_dir))
                    {
                        // Se o diretório não existir, tenta criar
                        if (!Directory.Exists(tmp_dir))
                        {
                            Directory.CreateDirectory(tmp_dir);
                        }
                        dir = tmp_dir;
                    }
                    else
                    {
                        throw new exception($"[{nameof(RankRegistryManager)}] O diretorio do Arquivo .ini esta vazio.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_REGISTRY_MANAGER, 5001, 0));
                    }
                }
                catch (exception e)
                {
                    // Caso falhe a leitura do .ini, reporta e tenta usar o diretório padrão "Log"
                    _smp.LogManager.Instance.push(new AppMessage($"[{nameof(RankRegistryManager)}] [init_log][ErrorSystem] {e.getFullMessageError()}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    _smp.LogManager.Instance.push(new AppMessage($"[{nameof(RankRegistryManager)}] [init_log] Usando diretorio padrao 'Log'.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    dir = "Log";
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                }

                // Garante que qualquer log anterior seja fechado antes de iniciar um novo
                close_log();

                // Gera o Name do arquivo com Timestamp: ddMMyyyyHHmmss
                string datetime = DateTime.Now.ToString("ddMMyyyyHHmmss");

                // Se houver um prefixo (prex), adiciona ao Name do arquivo
                string fileName = string.IsNullOrEmpty(prex)
                    ? $"log Registros {datetime}.log"
                    : $"log Registros {datetime} {prex}.log";

                string fullPath = Path.Combine(dir, fileName);

                // Cria o StreamWriter com FileShare.Read para permitir abrir o log enquanto o servidor roda
                log = new StreamWriter(new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    AutoFlush = true // Garante que o log seja gravado no disco imediatamente
                };
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage($"[{nameof(RankRegistryManager)}] [init_log][Critical Error] {e.getFullMessageError()}", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        protected void close_log()
        {
            try
            {
                if (log != null)
                {
                    // O Close() já chama o Flush() internamente para garantir que nada se perca
                    log.Close();
                    log = null; // Boa prática: anular a referência após fechar
                }
            }
            catch (exception e)
            {
                // Log padronizado com o Name da classe
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[{nameof(RankRegistryManager)}] [close_log][Error] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            catch (Exception ex)
            {
                // Catch para exceções genéricas do sistema (IOException, etc)
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[{nameof(RankRegistryManager)}] [close_log][Critical Error] {ex.Message}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        protected void putLog(string _str_log)
        {
            try
            {
                // Se o log for nulo, tenta inicializar
                if (log == null)
                {
                    init_log();
                }

                // Verifica novamente após a tentativa de init_log
                if (log != null)
                {
                    // Escreve a linha e garante que os dados saiam do buffer para o arquivo
                    log.WriteLine(_str_log);
                    log.Flush();
                }
            }
            catch (ObjectDisposedException)
            {
                // Se o objeto foi descartado, tentamos anular a referência para que o próximo log tente reabrir
                log = null;
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[{nameof(RankRegistryManager)}] [putLog][Error] Stream ja foi fechado. Tentando resetar referencia.",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[{nameof(RankRegistryManager)}] [putLog][Error] {e.Message} Erro ao escrever no arquivo de log.",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
    } 

    public class sRankRegistryManager : Singleton<RankRegistryManager>
    {

    } 
    public static class EnumOperator
    {
        public static void ENUM_OPERATOR_PLUS_PLUS<T>(ref T element) where T : struct, Enum
        {
            int value = Convert.ToInt32(element); // converte seguro
            value++;
            element = (T)Enum.ToObject(typeof(T), value);
        }
    }
}