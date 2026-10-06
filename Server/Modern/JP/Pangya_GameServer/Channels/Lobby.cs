using Pangya_GameServer.Engine;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Models.Game;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using static Pangya_GameServer.Models.ChangePlayerItemRoom;
namespace Pangya_GameServer.Channels
{
    public partial class Lobby
    {
        private readonly Channel _Channel;
        public FilterHacker _FilterHacker;//contem a logica das salas....
        public Lobby(Channel channel)
        {
            //aqui eu posso criar as permissoes, somente gm, somente better tests
            //roockies, etc..
            //por hora eu vou deixar assim..
            _FilterHacker = new FilterHacker();
            _Channel = channel;
        }

        public ChannelInfo LobbyInfo => _Channel.m_ci; 

        /// <summary>
        /// Realiza a entrada do jogador em uma lobby específica (ID)
        /// </summary>
        public async void EnterLobby(Player _session, byte _lobbyId)
        {
            try
            {
                // 1. Atualiza localização básica
                _session.UserInfo.Lobby = _lobbyId;
                _session.SetRoom(null);
                _session.SetChannel(_Channel);
                // 2. Sincroniza informações no Dicionário do Canal e DB
                UpdatePlayerInfo(_session);

                // 3. Coleta dados para os pacotes
                var sessionsInLobby = _Channel.GetSessions(_lobbyId);

                var pciList = sessionsInLobby
                    .Select(s => _Channel.GetPlayerInfo(s))
                    .Where(p => p != null)
                    .ToList();

                var rooms = _Channel.getRoomsInfo().getRoomsInfo();

                //envia o LIST Normal-LOBBY(EVITA BUG VISUAL)
                _session.Send(Handle_PACKET_RESPONSE.MakePlayerLobby(new List<PlayerLobbyInfo>() { pciList[0] }, 4));
                //envia o LIST Normal-LOBBY(LIST)
                _session.Send(Handle_PACKET_RESPONSE.MakePlayerLobby(pciList, 5));
                //envia o LIST ROOM mesmo se nao tiver salas(EVITA BUG VISUAL)
                _session.Send(Handle_PACKET_RESPONSE.MakeGameRoomList(rooms, 0)); 
                //envia O CREATE-Normal-LOBBY(Faz parece na lobby)
                _Channel.SendBroadcast(Handle_PACKET_RESPONSE.MakePlayerLobby(new List<PlayerLobbyInfo> { _Channel.GetPlayerInfo(_session) }, 1), _lobbyId);
                //limpa a lista por causa da memoria...
                pciList.Clear();
                pciList = null;
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::EnterLobby][Error] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        /// <summary>
        /// Retira o jogador da visualização da lobby
        /// </summary>
        public async void LeaveLobby(Player _session)
        {
            try
            {
                // 1. Tira da sala se ele estiver em uma (Regra de negócio do Canal)
                if (_session.GetChannel() != null || _session.UserInfo.Member.RoomID != -1)
                    _Channel.LeaveRoom(_session, 0);

                var lobbyAnterior = _session.UserInfo.Lobby;

                // 2. Reseta os estados de localização
                _session.UserInfo.Lobby = 255;
                _session.SetRoom(null);
                // 3. Atualiza o dicionário PlayerInfo no Channel
                UpdatePlayerInfo(_session);

                // 4. Avisa os OUTROS jogadores daquela lobby específica que ele saiu
                // O pacote 0x46 com Type 2 costuma ser o "Remove Player" no Pangya
                var packetRemover = Handle_PACKET_RESPONSE.MakePlayerLobby([_Channel.GetPlayerInfo(_session)], 2);

                // Usamos o Broadcast que criamos, filtrando pela lobby onde ele estava
                _Channel.SendBroadcast(packetRemover, lobbyAnterior);

                _smp.LogManager.Instance.push(new AppMessage($"[Lobby::LeaveLobby][Warning] Normal[UID: {_session.UserInfo.UID}] EXIT TO LOBBY.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::LeaveLobby][Error] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public List<stPlayerReward> getAllEligibleToGoldenTime()
        {
            List<stPlayerReward> players = new List<stPlayerReward>();


            // Channel verifica se o player está elegível a participar do Golden Time Event
            // Verifica se o player está em sala jogando ou no Lounge, practice e Grand Prix Rookie não conta
            // [Lambda] get Room Info
            (bool isGaming, GameRoomInfoModel info) getRoomInfoLambda(Player _p)
            {
                var r = _p.GetRoom();

                if (r != null)
                {
                    return (r.CurrentGame != null, r.GetInfo());
                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[Lobby.Room::isGoldenTimeGood::lambda(getRoomInfo)][Error][WARNNING] Normal [UID={_p.UserInfo.UID}] esta na sala[NUMERO={_p.UserInfo.Member.RoomID}], mas ela nao existe. Hacker ou Bug",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
                return (false, null);
            }
            foreach (var p in _Channel.Sessions)
            {
                // Invalid Player
                if (p?.UserInfo == null) continue;

                // Não está no lobby (pode estar carregando ou trocando de canal)
                if (p.UserInfo.Lobby == 255) continue;

                // Não está em nenhuma sala
                if (p.UserInfo.Member.RoomID == -1) continue;

                var (isPlaying, ri) = getRoomInfoLambda(p);

                // Não encontrou a sala ou GameRoomInfoModel inválido
                if (ri == null || ri.RoomID == -1) continue;

                // 1. Filtro: Practice ou Grand Zodiac Practice não contam
                if (ri.GetRoomType() == RoomTypeFlags.PRACTICE ||
                    ri.GetRoomType() == RoomTypeFlags.GRAND_ZODIAC_PRACTICE)
                    continue;

                // 2. Filtro: Grand Prix Rookie (Tutorial) não conta
                if (ri.GetRoomType() == RoomTypeFlags.GRAND_PRIX)
                {
                    var aba = sIff.Instance.getGrandPrixAba(ri.grand_prix.dados_typeid);
                    bool isNormal = sIff.Instance.isGrandPrixNormal(ri.grand_prix.dados_typeid);

                    if (aba == 0 && isNormal)
                        continue;
                }

                // 3. Regra do Lounge: Lounge conta sempre. Outros modos só se estiver "In-Game" (Playing)
                if (ri.GetRoomType() != RoomTypeFlags.LOUNGE && !isPlaying)
                    continue;

                // Se passou em todos os filtros, adiciona à lista de recompensa
                players.Add(new stPlayerReward
                {
                    uid = p.UserInfo.UID,
                    is_premium = true,
                    is_playing = isPlaying
                });
            }
            return players;
        }

        public async void SendFireWorksWinnerGoldenTime(List<stPlayerReward> _winners)
        {
            foreach (var el in _winners)
            {
                Player p = null;
                try
                {

                    if ((p = FindSessionByUID((int)el.uid)) == null)
                    {
                        continue;
                    }

                    if (p.UserInfo.Member.RoomID == -1 || p.UserInfo.Lobby == 255)
                    {
                        continue;
                    }

                    var r = p.GetRoom();

                    if (r != null)
                    {

                        if (r.GetTipo() == RoomTypeFlags.LOUNGE)
                        {

                            // send "chat" da sala fogos de artifícios em cima da cabela do player(*p)
                            r.SendBroadCast(Handle_PACKET_RESPONSE.pacote04B(p, (byte)TYPE_CHANGE.TC_ITEM_EFFECT_LOUNGE, 0, (int)stItemEffectLounge.TYPE_EFFECT.TE_TWILIGHT));
                        }

                    }
                    else
                    {
                        _smp.LogManager.Instance.push(new AppMessage("[Lobby.Room::sendFireWorksWinnerGoldenTime][Error][WARNNING] Normal [UID=" + (p.UserInfo.UID) + "] esta na sala[NUMERO=" + (p.UserInfo.Member.RoomID) + "], mas ela nao existe. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
                catch (exception e)
                {

                    _smp.LogManager.Instance.push(new AppMessage("[Lobby.Room::sendFireWorksWinnerGoldenTime][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
        }
    }
}
