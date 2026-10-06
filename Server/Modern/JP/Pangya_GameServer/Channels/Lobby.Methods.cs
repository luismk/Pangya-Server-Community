using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models.Game;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Roms;
using Pangya_GameServer.Session;
using PangyaAPI.DataBase;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities.Models;
using snmdb;
using static Pangya_GameServer.Channels.Channel;
namespace Pangya_GameServer.Channels
{
    public partial class Lobby
    {
        #region Player Sessions & Search
        public Player FindSessionByOID(uint _oid) => _Channel.FindSessionByOID(_oid);

        public Player FindSessionByUID(int _uid) => _Channel.FindSessionByUID(_uid);

        public Player FindSessionByNickname(string _nickname) => _Channel.FindSessionByNickname(_nickname);

        public int findIndexSession(Player _session)
        {
            if (_session == null) return -1;
            return _Channel.findIndexSession(_session);
        }

        public PlayerLobbyInfo GetPlayerInfo(Player _session) => _Channel.GetPlayerInfo(_session);
        #endregion

        #region Room Management
        public void AddRoom(Room room) => _Channel.m_rm.addRoom(room);

        public void UnlockRoom(Room room) => _Channel.m_rm.UnlockRoom(room);

        private Room? FindRoom(short id) => _Channel.FindRoom(id);
        private Room? FindRoom(Room room) => _Channel.FindRoom(room);

        public LEAVE_ROOM_STATE LeaveRoom(Player _session, int _option)
            => _Channel.LeaveRoom(_session, _option);

        public LEAVE_ROOM_STATE LeaveRoomMultiPlayer(Player _session, int _option)
            => _Channel.LeaveRoomMultiPlayer(_session, _option);

        public LEAVE_ROOM_STATE LeaveRoomGrandPrix(Player _session, int _option)
            => _Channel.LeaveRoomGrandPrix(_session, _option);

        public LEAVE_ROOM_STATE KickPlayerRoom(Player _session, byte force)
            => _Channel.KickPlayerRoom(_session, force);
        #endregion
          
        #region Broadcast & Client Updates
         
        public void SendBroadCast(List<Packet> p) => _Channel.SendBroadcast(p);
         
        public void SendUpdateRoomInfo(GameRoomInfoModel _ri, int _option)
        {
            _Channel.SendUpdateRoomInfo(_ri, _option);
        }

        public void SendUpdatePlayerInfo(Player _session, int _option)
        {
            _Channel.SendUpdatePlayerInfo(_session, _option);
        }

        public void UpdatePlayerInfo(Player _session)
        {
            _Channel.UpdatePlayerInfo(_session);
        }
        #endregion
         
        public void EnterMultiPlayer(Player _session)
        {

            try
            {
                // Enter Lobby
                EnterLobby(_session, 1);
                _session.Send(Handle_PACKET_RESPONSE.pacote0F5());
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[Channel::EnterMultiPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        }

        public void EnterGrandPrix(Player _session)
        { 
            try
            {
                // Enter Lobby
                EnterLobby(_session, 176); 
            }
            catch (exception e)
            { 
                _smp.LogManager.Instance.push(new AppMessage("[Channel::EnterGrandPrix][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        }   

        public void LeaveMultiPlayer(Player _session)
        {
            try
            {
                LeaveLobby(_session);
                _session.Send(Handle_PACKET_RESPONSE.pacote0F6());
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::LeaveMultiPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void LeaveGrandPrix(Player _session)
        {
            try
            {
                LeaveLobby(_session);

                // Sai Lobby Grand Prix
                var p = new Packet((ushort)0x251);

                p.WriteUInt32(0u); // OK 

                _session.Send(p);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::LeaveGrandPrix][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public void DestroyRoom(Room r)
        {
            try
            {
                if (r == null)
                {
                    throw new exception("[Lobby.Room::destroyRoom][Error] Channel[ID=" + LobbyInfo.id + "] tentou destruir a sala[NUMERO=" + r.GetRoomId() + "], mas a sala nao existe.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        16, 0x5700100));
                }

                // Kick All of Room And Automatic Room Destroyed 
                if (_Channel.Sessions.Count == 0)
                {

                    GameRoomInfoModel ri = r.GetInfo();

                   this._Channel.DestroyRoom(r);

                    SendUpdateRoomInfo(ri, 2);

                }
                else
                {

                    // Kick all player e destroi a sala
                    foreach (var el in _Channel.Sessions)
                    {
                        KickPlayerRoom(el, 0);
                    }
                }

                // Log
                _smp.LogManager.Instance.push(new AppMessage("[Lobby.Room::destroyRoom][Sucess] Channel[ID=" + LobbyInfo.id + "] destruiu a sala[NUMERO=" + r.GetRoomId() + "] no canal[NOME=" + (LobbyInfo.name) + "].", type_msg.CL_FILE_LOG_AND_CONSOLE));



            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[Lobby.Room::destroyRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        /// <summary>
        /// Sincroniza o fechamento do tempo de entrada da sala com o Lobby do Canal.
        /// </summary>
        public static void OnEntryTimeExpired(object? channelArg, object? roomArg)
        {
            // 1. Defesa contra argumentos nulos usando Pattern Matching
            if (channelArg is not Channel channel || roomArg is not Room room)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Lobby::Critical] Falha no Timeout: Argumentos inválidos (Channel: {channelArg != null}, Room: {roomArg != null})",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            try
            {
                if (channel != null && room != null)
                {
                    // Reset direto na instância capturada
                    room.SetState(0);
                    room.SetFlag(0);
                    room.CurrentGame?.RequestEndAfterEnter();

                    // Atualiza o Lobby do Canal
                    channel.SendListUpdateRooms(room.GetInfo());

                    _smp.LogManager.Instance.push(new AppMessage(
                    $"[Lobby::Sync] Sala #{room.GetRoomId()} no Canal {channel.getId()} resetada com sucesso.",
                    type_msg.CL_ONLY_CONSOLE));
                }
            }
            catch (Exception ex)
            {
                _smp.LogManager.Instance.push(new AppMessage($"[Lobby::Error] {ex.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
         
        public static void SQLDBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg)
        {

            if (_arg == null)
            {
                return;
            }

            // Por Hora só sai, depois faço outro Type de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            switch (_msg_id)
            {
                case 1: // Update Dolfini Locker Pass
                    {
                        var cmd_udlp = Tools.reinterpret_cast<CmdUpdateDolfiniLockerPass>(_pangya_db);

                        // _smp.LogManager.Instance.push(new AppMessage("[Lobby::SQLDBResponse][Sucess] Atualizou a Password[value=" + cmd_udlp.getPass() + "] do Dolfini Locker do Normal [UID=" + (cmd_udlp.getUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    }
                case 2: // Update Dolfini Locker Mode
                    {
                        var cmd_udlm = Tools.reinterpret_cast<CmdUpdateDolfiniLockerMode>(_pangya_db);

                        // _smp.LogManager.Instance.push(new AppMessage("[Lobby::SQLDBResponse][Sucess] Atualizou o Modo[locker=" + ((ushort)cmd_udlm.getLocker()) + "] do Dolfini Locker do Normal [UID=" + (cmd_udlm.getUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    }
                case 3: // Update Dolfini Locker Pang
                    {
                        var cmd_udlp = Tools.reinterpret_cast<CmdUpdateDolfiniLockerPang>(_pangya_db);

                        // _smp.LogManager.Instance.push(new AppMessage("[Lobby::SQLDBResponse][Sucess] Atualizou o Pang[value=" + (cmd_udlp.getPang()) + "] do Dolfini Locker do Normal [UID=" + (cmd_udlp.getUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    }
                case 4: // Delete Dolfini Locker Item
                    {
                        var cmd_ddli = Tools.reinterpret_cast<CmdDeleteDolfiniLockerItem>(_pangya_db);

                        // _smp.LogManager.Instance.push(new AppMessage("[Lobby::SQLDBResponse][Sucess] Deletou o Dolfini Locker Item[index=" + (cmd_ddli.getIndex()) + "] do Normal [UID=" + (cmd_ddli.getUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    }
                case 5: // Extend Part Rental
                    {
                        var cmd_er = Tools.reinterpret_cast<CmdExtendRental>(_pangya_db);

                        // _smp.LogManager.Instance.push(new AppMessage("[Lobby::SQLDBResponse][Sucess] Extendeu Part Rental[ID=" + (cmd_er.getItemID()) + "] ate o a date[value=" + cmd_er.getDate() + "] para o Normal [UID=" + (cmd_er.getUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 6: // Delete Part Rental
                    {
                        var cmd_dr = Tools.reinterpret_cast<CmdDeleteRental>(_pangya_db);

                        // _smp.LogManager.Instance.push(new AppMessage("[Lobby::SQLDBResponse][Sucess] Deletou Part Rental[ID=" + (cmd_dr.getItemID()) + "] do Normal [UID=" + (cmd_dr.getUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 7: // Update Character PCL
                    {
                        var cmd_ucp = Tools.reinterpret_cast<CmdUpdateCharacterPCL>(_pangya_db);

                        // _smp.LogManager.Instance.push(new AppMessage("[Lobby::SQLDBResponse][Sucess] Atualizou Character[TYPEID=" + (cmd_ucp.getInfo()._typeid) + ", ID=" + (cmd_ucp.getInfo().Login) + "] PCL[C0=" + ((ushort)cmd_ucp.getInfo().pcl[(int)CharacterInfo.Stats.S_POWER]) + ", C1=" + ((ushort)cmd_ucp.getInfo().pcl[(int)CharacterInfo.Stats.S_CONTROL]) + ", C2=" + ((ushort)cmd_ucp.getInfo().pcl[(int)CharacterInfo.Stats.S_ACCURACY]) + ", C3=" + ((ushort)cmd_ucp.getInfo().pcl[(int)CharacterInfo.Stats.S_SPIN]) + ", C4=" + ((ushort)cmd_ucp.getInfo().pcl[(int)CharacterInfo.Stats.S_CURVE]) + "] do Normal [UID=" + (cmd_ucp.getUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 8: // Update ClubSet Stats
                    {
                        var cmd_ucss = Tools.reinterpret_cast<CmdUpdateClubSetStats>(_pangya_db);

                        // _smp.LogManager.Instance.push(new AppMessage("[Lobby::SQLDBResponse][Sucess] Atualizou ClubSet[TYPEID=" + (cmd_ucss.getInfo()._typeid) + ", ID=" + (cmd_ucss.getInfo().Login) + "] Stats[C0=" + ((ushort)cmd_ucss.getInfo().c[(int)CharacterInfo.Stats.S_POWER]) + ", C1=" + ((ushort)cmd_ucss.getInfo().c[(int)CharacterInfo.Stats.S_CONTROL]) + ", C2=" + ((ushort)cmd_ucss.getInfo().c[(int)CharacterInfo.Stats.S_ACCURACY]) + ", C3=" + ((ushort)cmd_ucss.getInfo().c[(int)CharacterInfo.Stats.S_SPIN]) + ", C4=" + ((ushort)cmd_ucss.getInfo().c[(int)CharacterInfo.Stats.S_CURVE]) + "] do Normal [UID=" + (cmd_ucss.getUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 9: // Update Character Mastery
                    {
                        var cmd_ucm = Tools.reinterpret_cast<CmdUpdateCharacterMastery>(_pangya_db);

                        // _smp.LogManager.Instance.push(new AppMessage("[Lobby::SQLDBResponse][Sucess] Atualizou Character[TYPEID=" + (cmd_ucm.getInfo()._typeid) + ", ID=" + (cmd_ucm.getInfo().Login) + "] Mastery[value=" + (cmd_ucm.getInfo().mastery) + "] do Normal [UID=" + (cmd_ucm.getUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 10: // Equipa Card
                    {
                        var cmd_ec = Tools.reinterpret_cast<CmdEquipCard>(_pangya_db);

                        // _smp.LogManager.Instance.push(new AppMessage("[Lobby::SQLDBResponse][Sucess] Equipou Card[TYPEID=" + (cmd_ec.getInfo()._typeid) + "] no Character[TYPEID=" + (cmd_ec.getInfo().parts_typeid) + ", ID=" + (cmd_ec.getInfo().parts_id) + "] do Normal [UID=" + (cmd_ec.getUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 11: // Desequipa Card
                    {
                        var cmd_rec = Tools.reinterpret_cast<CmdRemoveEquipedCard>(_pangya_db);
                        break;
                    }
                case 12: // Update ClubSet Workshop
                    {
                        var cmd_ucw = Tools.reinterpret_cast<CmdUpdateClubSetWorkshop>(_pangya_db);
                        break;
                    }
                case 13: // Update Tutorial
                    {
                        var cmd_ut = Tools.reinterpret_cast<CmdUpdateTutorial>(_pangya_db);
                        break;
                    }
                case 14: // Tutorial Event Clear
                    {
                        var cmd_tec = Tools.reinterpret_cast<CmdTutoEventClear>(_pangya_db);
                        break;
                    }
                case 15: // Use Item Buff
                    {
                        var cmd_uib = Tools.reinterpret_cast<CmdUseItemBuff>(_pangya_db);
                        break;
                    }
                case 16: // Update Item Buff
                    {
                        var cmd_uib = Tools.reinterpret_cast<CmdUpdateItemBuff>(_pangya_db);

                        //// _smp.LogManager.Instance.push(new AppMessage("[Lobby::SQLDBResponse][Sucess] Normal [UID=" + (cmd_uib.getUID()) + "] Atualizou o tempo do Item Buff[INDEX=" + (cmd_uib.getInfo().index) + ", TYPEID=" + (cmd_uib.getInfo()._typeid) + ", TIPO=" + (cmd_uib.getInfo().Type) + ", DATE{REG_DT: " + _formatDate(cmd_uib.getInfo().use_date) + ", END_DT: " + _formatDate(cmd_uib.getInfo().end_date) + "}]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 17: // Update Card Special Time
                    {
                        var cmd_ucst = Tools.reinterpret_cast<CmdUpdateCardSpecialTime>(_pangya_db);

                        // // _smp.LogManager.Instance.push(new AppMessage("[Lobby::SQLDBResponse][Sucess] Normal [UID=" + (cmd_ucst.getUID()) + "] Atualizou o tempo do Card Special[index=" + (cmd_ucst.getInfo().index) + ", TYPEID=" + (cmd_ucst.getInfo()._typeid) + ", EFEITO{TYPE: " + (cmd_ucst.getInfo().efeito) + ", QNTD: " + (cmd_ucst.getInfo().efeito_qntd) + "}, TIPO=" + (cmd_ucst.getInfo().Type) + ", DATE{REG_DT: " + _formatDate(cmd_ucst.getInfo().use_date) + ", END_DT: " + _formatDate(cmd_ucst.getInfo().end_date) + "}]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 18: // Update Player Papel Shop Limit
                    {
                        var cmd_upsl = Tools.reinterpret_cast<CmdUpdatePapelShopInfo>(_pangya_db);

                        // _smp.LogManager.Instance.push(new AppMessage("[Lobby::SQLDBResponse][Sucess] Normal [UID=" + (cmd_upsl.getUID()) + "] Atualizou o Papel Shop Limit[current_cnt=" + (cmd_upsl.getInfo().CurrentCount) + ", remain_cnt=" + (cmd_upsl.getInfo().RemainCount) + ", limit_cnt=" + (cmd_upsl.getInfo().LimitCount) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 19: // Insert Papel Shop Rare Win Log
                    {
                        var cmd_ipsrwl = Tools.reinterpret_cast<CmdInsertPapelShopRareWinLog>(_pangya_db);

                        // _smp.LogManager.Instance.push(new AppMessage("[Lobby::SQLDBResponse][Sucess] Normal [UID=" + (cmd_ipsrwl.getUID()) + "] Adicionou Papel Shop Rare Win Log[TYPEID=" + (cmd_ipsrwl.getInfo().ctx_psi._typeid) + ", QNTD=" + (cmd_ipsrwl.getInfo().qntd) + ", COLOR=" + (cmd_ipsrwl.getInfo().color) + ", PROBABILIDADE=" + (cmd_ipsrwl.getInfo().ctx_psi.probabilidade) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 20: // Pay Caddie Holy Day (Paga as ferias do Caddie)
                    {
                        var cmd_pchd = Tools.reinterpret_cast<CmdPayCaddieHolyDay>(_pangya_db);

                        // _smp.LogManager.Instance.push(new AppMessage("[Lobby::SQLDBResponse][Sucess] Normal [UID=" + (cmd_pchd.getUID()) + "] Pagou as ferias do Caddie[ID=" + (cmd_pchd.getId()) + "] ate " + cmd_pchd.getEndDate(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 21: // Set Notice Caddie Holy Day (Seta Aviso de ferias do Caddie)
                    {
                        var cmd_snchd = Tools.reinterpret_cast<CmdSetNoticeCaddieHolyDay>(_pangya_db);

                        //_smp.LogManager.Instance.push(new AppMessage("[Lobby::SQLDBResponse][Sucess] Normal [UID=" + (cmd_snchd.getUID()) + "] setou Aviso[check=" + (cmd_snchd.getCheck() ? "ON" : "OFF") + "] de ferias do Caddie[ID=" + (cmd_snchd.getId()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 22: // Insert Box Rare Win Log
                    {
                        var cmd_ibrwl = Tools.reinterpret_cast<CmdInsertBoxRareWinLog>(_pangya_db);

                        // _smp.LogManager.Instance.push(new AppMessage("[Lobby::SQLDBResponse][Sucess] Normal [UID=" + (cmd_ibrwl.getUID()) + "] Inseriu Box[TYPEID=" + (cmd_ibrwl.getBoxTypeid()) + "] Rare[TYPEID=" + (cmd_ibrwl.getInfo()._typeid) + ", QNTD=" + (cmd_ibrwl.getInfo().qntd) + ", RARIDADE=" + ((ushort)cmd_ibrwl.getInfo().raridade) + "] Win Log", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 23: // Insert Spinning Cube Super Rare Win Broadcast
                    {
                        var cmd_ispcsrwb = Tools.reinterpret_cast<CmdInsertSpinningCubeSuperRareWinBroadcast>(_pangya_db);

                        // _smp.LogManager.Instance.push(new AppMessage("[Lobby::SQLDBResponse][Sucess] Inseriu Spinning Cube Super Rare Win Broadcast[MSG=" + cmd_ispcsrwb.getMessage() + ", OPT=" + ((ushort)cmd_ispcsrwb.getOpt()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 24: // Insert Memorial Shop Rare Win Log
                    {
                        var cmd_imrwl = Tools.reinterpret_cast<CmdInsertMemorialRareWinLog>(_pangya_db);

                        // _smp.LogManager.Instance.push(new AppMessage("[Lobby::SQLDBResponse][Sucess] Normal [UID=" + (cmd_imrwl.getUID()) + "] Inseriu Memorial Shop[COIN=" + (cmd_imrwl.getCoinTypeid()) + "] Rare[TYPEID=" + (cmd_imrwl.getInfo()._typeid) + ", QNTD=" + (cmd_imrwl.getInfo().qntd) + ", RARIDADE=" + (cmd_imrwl.getInfo().Type) + "] Win Log", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 26: // Update Mascot Info
                    {

                        var cmd_umi = Tools.reinterpret_cast<CmdUpdateMascotInfo>(_pangya_db);

                        //// _smp.LogManager.Instance.push(new AppMessage("[Lobby::SQLDBResponse][Sucess] Normal [UID=" + (cmd_umi.getUID()) + "] Atualizar Mascot Info[TYPEID=" + (cmd_umi.getInfo()._typeid) + ", ID=" + (cmd_umi.getInfo().Login) + ", LEVEL=" + ((ushort)cmd_umi.getInfo().Level) + ", EXP=" + (cmd_umi.getInfo().Experience) + ", FLAG=" + ((ushort)cmd_umi.getInfo().type) + ", TIPO=" + (cmd_umi.getInfo().Type) + ", IS_CASH=" + ((ushort)cmd_umi.getInfo().is_cash) + ", PRICE=" + (cmd_umi.getInfo().price) + ", MESSAGE=" + (cmd_umi.getInfo().AppMessage) + ", END_DT=" + _formatDate(cmd_umi.getInfo().data) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    }
                case 27: // Atualizou Guild Update Activity
                    {
                        // var cmd_uguai = Tools.reinterpret_cast<CmdUpdateGuildUpdateActiviy>(_pangya_db);

                        //_smp.LogManager.Instance.push(new AppMessage("[Lobby::SQLDBResponse][Sucess] Atualizou Guild Update Activity[INDEX=" + (cmd_uguai.getIndex()) + "] com sucesso.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    }
                case 28: // Atualizou Legacy Tiki Shop Point
                    {
                        var cmd_ultp = Tools.reinterpret_cast<CmdUpdateLegacyTikiShopPoint>(_pangya_db);

                        // _smp.LogManager.Instance.push(new AppMessage("[Lobby::SQLDBResponse][Sucess] Normal [UID=" + (cmd_ultp.getUID()) + "] atualizou Legacy Tiki Shop Point(" + (cmd_ultp.getTikiShopPoint()) + ")", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    }
                case 0:
                default: // 25 é update item equipado slot
                    break;
            }

        }
    }
}
