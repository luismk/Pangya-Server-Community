using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using PangyaAPI.Network.Repository;
using snmdb;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_LOGIN : HandleBase<Player, Packet_PLAYER_LOGIN>
    {
        public override async Task Handle()
        {
            Packet p = null;

            try
            {
                // --- Variáveis / estruturas --- 
                // Injeta os Info limpos Do PacketResult no Player
                Player.UserInfo.Login = PacketResult.Login; 
                Player.UserInfo.UID = PacketResult.UID;
                Player.MacAdress = PacketResult.MacAddress;
                Player.ResetHandShake();
                // Validação De integridade dos parâmetros Do pacote
                if (!ValidateLoginPacket(PacketResult.HasClientVersion, PacketResult.ClientVersion, PacketResult.PacketVersion, PacketResult.HasLKey, PacketResult.LKey, PacketResult.HasMAC, Player.MacAdress, PacketResult.HasGKey, PacketResult.GKey))
                {
                    _smp.LogManager.Instance.push(new AppMessage("[HANDLE_PLAYER_LOGIN][Warning] Normal[UID=" + (Player.UserInfo.UID) + $", UserID= {Player.UserInfo.Login}, AuthKey[1]= {PacketResult.LKey},  AuthKey[2]= {PacketResult.GKey}, NtreevUID= {PacketResult.Command}, CVersion= {PacketResult.ClientVersion}]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    Player.Authorized = false;
                    SendLoginAck(eLoginAck.ACK_INVALID_VERSION);
                    return;
                }

                // --- Ban checks (IP / MAC) ---
                if (GameServer.Instance.haveBanList(Player.GetIP(), Player.MacAdress))
                    throw new exception($"Normal[UID={Player.UserInfo.UID}, IP={Player.GetIP()}, MAC={Player.MacAdress}] blocked by banlist.");

                // --- sanity: Login non-empty ---
                if (string.IsNullOrEmpty(Player.UserInfo.Login))
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[HANDLE_PLAYER_LOGIN][Warning] Normal[UID={Player.UserInfo.UID}, IP={Player.GetIP()}] invalid Login: {Player.UserInfo.Login}",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));

                    Player.Authorized = false;
                    SendLoginAck(eLoginAck.ACK_INVALID_VERSION);
                    return;
                }

                // --- Retrieve Player info from DB ---
                var cmdPi = new CmdPlayerInfo(Player.UserInfo.UID); // waiter
                NormalManagerDB.Instance.add(0, cmdPi);
                if (cmdPi.getException().getCodeError() != 0) throw cmdPi.getException();

                Player.UserInfo.Set(cmdPi.getInfo());

                if (Player.UserInfo.UID <= 0)
                {
                    _smp.LogManager.Instance.push(new AppMessage($"[HANDLE_PLAYER_LOGIN][Warning] Normal[UID={Player.UserInfo.UID}] not found in DB", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    Player.Authorized = false;
                    SendLoginAck(eLoginAck.ACK_INVALID_ID);
                    return;
                }

                // --- Anti-hack: verify client-supplied ID matches DB ID ---
                if (!string.Equals(cmdPi.getInfo().Login, Player.UserInfo.Login, StringComparison.Ordinal))
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[HANDLE_PLAYER_LOGIN][Warning] Normal[UID={Player.UserInfo.UID}] client ID mismatch: client={Player.UserInfo.Login}, db={cmdPi.getInfo().Login}",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));

                    Player.Authorized = false;
                    SendLoginAck(eLoginAck.ACK_INVALID_ID);
                    return;
                }

                // --- Account block checks (temporary / forever / all-IpAddress) ---
                CheckAccountBlock();

                // --- Packet BuildVersion validation (after decrypt) --- 
                var serverPacketVersion = GameServer.Instance.getInfo().VersionPacket;
                if (!GameServer.Instance.canSameIDLogin() && PacketResult.PacketVersion != serverPacketVersion)
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[HANDLE_PLAYER_LOGIN][Warning] Normal[UID={Player.UserInfo.UID}]. Client Packet Version not Match. Server: {serverPacketVersion} != Client: {PacketResult.PacketVersion}",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));

                    Player.Authorized = false;
                    SendLoginAck(eLoginAck.ACK_INVALID_VERSION);
                    return;
                }

                // --- AuthKey (login) check ---
                var cmdAkli = new CmdAuthKeyLoginInfo((int)Player.UserInfo.UID);
                NormalManagerDB.Instance.add(0, cmdAkli);
                if (cmdAkli.getException().getCodeError() != 0) throw cmdAkli.getException();

                // NOTE: security: previously code used bitwise & and inverted booleans -> fixed
                if (!GameServer.Instance.canSameIDLogin() && (!string.Equals(PacketResult.LKey, cmdAkli.getInfo().key, StringComparison.Ordinal) || cmdAkli.getInfo().valid == 0))
                {
                    _smp.LogManager.Instance.push(new AppMessage($"[HANDLE_PLAYER_LOGIN][Warning] Normal[UID={Player.UserInfo.UID}]. LKey invalid or reused.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    Player.Authorized = false;
                    SendLoginAck(eLoginAck.ACK_SECURITY_KEY);
                    return;
                }

                // --- AuthKey (game) check ---
                var cmdAkgi = new CmdAuthKeyGameInfo(Player.UserInfo.UID, (int)GameServer.Instance.getUID());
                NormalManagerDB.Instance.add(0, cmdAkgi);
                if (cmdAkgi.getException().getCodeError() != 0) throw cmdAkgi.getException();

                if (!GameServer.Instance.canSameIDLogin() && (!string.Equals(PacketResult.GKey, cmdAkgi.getInfo().key, StringComparison.Ordinal) || cmdAkgi.getInfo().valid == 0))
                {
                    _smp.LogManager.Instance.push(new AppMessage($"[HANDLE_PLAYER_LOGIN][Warning] Normal[UID={Player.UserInfo.UID}]. GKey invalid or reused.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    Player.Authorized = false;
                    SendLoginAck(eLoginAck.ACK_SECURITY_KEY);
                    return;
                }

                // --- Client BuildVersion checks (region/season/high/low) ---
                var cvServer = ClientVersion.MakeVersion(GameServer.Instance.m_si.ClientVersion);
                var cvClient = ClientVersion.MakeVersion(PacketResult.ClientVersion);
                EvaluateClientVersion(cvServer, cvClient);

                // --- Member Info ---
                var cmdMi = new CmdMemberInfo(Player.UserInfo.UID);
                NormalManagerDB.Instance.add(0, cmdMi);
                if (cmdMi.getException().getCodeError() != 0) throw cmdMi.getException();
                Player.setMemberInfo(cmdMi.getInfo());

                // --- GM handling ---
                Player.UserInfo.Member.OID = Player.ConnectionID;
                Player.UserInfo.Member.State.Visible = 1;
                Player.UserInfo.Member.State.Whisper = Player.UserInfo.WhisperState;
                Player.UserInfo.Member.State.Channel = (byte)(Player.UserInfo.WhisperState == 0 ? 1 : 0);
                if (Player.UserInfo.UserCapabilities.IsGameMaster)
                {
                    Player.m_gi.setGMUID(Player.UserInfo.UID);
                    Player.UserInfo.Member.State.Visible = Player.m_gi.visible;
                    Player.UserInfo.Member.State.Whisper = Player.m_gi.whisper;
                    Player.UserInfo.Member.State.Channel = Player.m_gi.channel;

                }

                // --- GS property checks (rookie, Mantle) ---
                if (GameServer.Instance.m_si.Property.OnlyRookies && Player.UserInfo.Level >= 6)
                    throw new exception($"Normal[UID={Player.UserInfo.UID}, LEVEL={Player.UserInfo.Level}] not allowed (rookie-only GS).");

                if (GameServer.Instance.m_si.Property.Mantle && !(Player.UserInfo.UserCapabilities.Mantle || Player.UserInfo.UserCapabilities.IsGameMaster))
                    throw new exception($"Normal[UID={Player.UserInfo.UID}] lacks Mantle Capability.");

                // --- Overlap: if another Player with same UID exists ---
                var alreadyLogged = GameServer.Instance.HasLoggedWithOuterSocket(Player);
                if (alreadyLogged != null)
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[HANDLE_PLAYER_LOGIN][Error] existing Player for UID={Player.UserInfo.UID}, disconnecting existing.",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));

                    GameServer.Instance.Disconnect(alreadyLogged);
                    //throw new exception($"Failed to disconnect existing Player UID={alreadyLogged.getUID()}");
                }

                // --- Merge block flags and authorize Player ---
                Player.UserInfo.BlockFlag.Flag.Value |= GameServer.Instance.m_si.Flag.Value;
                Player.Authorized = true;

                // --- DB registration: Player logged into GS ---
                NormalManagerDB.Instance.add(5, new CmdRegisterLogon(Player.UserInfo.UID, 0));

                NormalManagerDB.Instance.add(7, new CmdRegisterLogonServer(Player.UserInfo.UID, GameServer.Instance.m_si.UID));

                _smp.LogManager.Instance.push(new AppMessage($"[HANDLE_PLAYER_LOGIN][Sucess] Normal[OID={Player.ConnectionID}, UID={Player.UserInfo.UID}, NICK={Player.UserInfo.NickName}].", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Papel ShopRoom init
                sPapelShopSystem.Instance.InitPlayerPapelShopInfo(Player);

                // Create login manager task to load all data
                if (Player.Load()) 
                    SendCompleteData();
                // Anti-bot timestamp
                Player.TicketBot = Environment.TickCount;
                
                Player.Send(Handle_PACKET_RESPONSE.pacote044(GameServer.Instance.m_si, eLoginAck.ACK_AUTO_RECONNECT, Player));
            }
            catch (exception ex)
            {
                _smp.LogManager.Instance.push(new AppMessage($"[HANDLE_PLAYER_LOGIN][Error] {ex.getFullMessageError()}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                Player.Authorized = false;

                // Generic error response
                p = new Packet(0x44);
                p.WriteUInt32(300);
                Player.Send(p);
                // Disconnect Player to be safe
                GameServer.Instance.Disconnect(Player);
            }

        await Task.CompletedTask;
        }

        private bool ValidateLoginPacket(
            bool hasClientVersion, string cversion, uint packetVersion,
            bool hasAuthKeyLogin, string lkey, bool hasMacAddress, string mac,
            bool hasAuthKeyGame, string gkey)
        {
            // checks: patch present, UID present, auth keys exist, Login length sanity
            if (packetVersion == 0)
            {
                SendLoginAck(eLoginAck.ACK_INVALID_VERSION);
                return false;
            }

            if (Player.UserInfo.UID == 0)
            {
                SendLoginAck(eLoginAck.ACK_LOGIN_FAIL);
                return false;
            }

            if (!hasClientVersion || string.IsNullOrEmpty(cversion))
            {
                SendLoginAck(eLoginAck.ACK_INVALID_VERSION);
                return false;
            }

            if (!hasAuthKeyLogin || string.IsNullOrEmpty(lkey))
            {
                SendLoginAck(eLoginAck.ACK_SECURITY_KEY);
                return false;
            }

            if (!hasMacAddress || string.IsNullOrEmpty(mac))
            {
                SendLoginAck(eLoginAck.ACK_BLOCKED_IP_ADDR);
                return false;
            }

            if (!hasAuthKeyGame || string.IsNullOrEmpty(gkey))
            {
                SendLoginAck(eLoginAck.ACK_INVALID_VERSION);
                return false;
            }
            if (string.IsNullOrEmpty(Player.UserInfo.Login) || Player.UserInfo.Login.Length >= 0x40)
            {
                SendLoginAck(eLoginAck.ACK_INVALID_ID);
                return false;
            }
            return true;
        }

        private void SendLoginAck(eLoginAck ack)
        {
            using (var p = new Packet(0x44))
            {
                p.WriteUInt32((byte)ack);
                Player.Send(p);
            }

            GameServer.Instance.Disconnect(Player);
        }

        private async void CheckAccountBlock()
        {
            // Verifica aqui se a conta do Player está bloqueada
            if (Player.UserInfo.BlockFlag.State.Value != 0)
            {

                if (Player.UserInfo.BlockFlag.State.BlockByTime && (Player.UserInfo.BlockFlag.State.TimeBlock == -1 || Player.UserInfo.BlockFlag.State.TimeBlock > 0))
                {

                    throw new exception("[HANDLE_PLAYER_LOGIN][Error] Bloqueado por tempo[Time="
                            + (Player.UserInfo.BlockFlag.State.TimeBlock == -1 ? ("indeterminado") : ((Player.UserInfo.BlockFlag.State.TimeBlock / 60)
                            + "min " + (Player.UserInfo.BlockFlag.State.TimeBlock % 60) + "sec"))
                            + "]. Player [UID=" + (Player.UserInfo.UID) + ", ID=" + (Player.UserInfo.Login) + "]");

                }
                else if (Player.UserInfo.BlockFlag.State.BlockForever)
                {

                    throw new exception("[HANDLE_PLAYER_LOGIN][Error] Bloqueado permanente. Player [UID=" + (Player.UserInfo.UID)
                            + ", ID=" + (Player.UserInfo.Login) + "]");
                }

                else if (Player.UserInfo.BlockFlag.State.BlockInIPAll)
                {

                    // Bloquea todos os IP que o Player logar e da error de que a area dele foi bloqueada

                    // Add o IpAddress do Player para a lista de IpAddress banidos
                    NormalManagerDB.Instance.add(9, new CmdInsertBlockIp(Player.GetIP(), Player.MacAdress));

                    // Resposta
                    throw new exception("[HANDLE_PLAYER_LOGIN][Error] Normal[UID=" + (Player.UserInfo.UID) + ", IP=" + (Player.GetIP())
                            + "] Block ALL IP que o Player fizer login.");
                }
                else if (Player.UserInfo.BlockFlag.State.BlockInAddressMac)
                {

                    // Bloquea o MAC Address que o Player logar e da error de que a area dele foi bloqueada

                    // Add o MAC Address do Player para a lista de MAC Address banidos
                    NormalManagerDB.Instance.add(10, new CmdInsertBlockMac(Player.MacAdress));

                    // Resposta
                    throw new exception("[HANDLE_PLAYER_LOGIN][Error] Normal[UID=" + (Player.UserInfo.UID)
                            + ", IP=" + (Player.GetIP()) + ", MAC=" + Player.MacAdress + "] Block MAC Address que o Player fizer login.");

                }
            }
        }
         
        private void EvaluateClientVersion(ClientVersion serverVer, ClientVersion clientVer)
        {
            if (clientVer.flag == ClientVersion.COMPLETE_VERSION &&
                string.Equals(clientVer.region, serverVer.region) &&
                string.Equals(clientVer.season, serverVer.season))
            {
                if (clientVer.high != serverVer.high || clientVer.low < serverVer.low)
                    Player.UserInfo.BlockFlag.Flag.AllGame = true;
            }
            else
            {
                if (clientVer.high != serverVer.high || clientVer.low < serverVer.low)
                    Player.UserInfo.BlockFlag.Flag.AllGame = true;
            }
        }




        void SendCompleteData()
        {
            //// Check All Character All Item Equiped is on Warehouse Item of Player
            foreach (var el in Player.Inventory.Characters)
            {
                // Check Parts of Character e Check Aux Part of Character
                Player.CheckCharacterAllItemEquiped(el.Value);
            }

            // Check All Item Equiped
            Player.CheckAllItemEquiped(Player.Inventory.UserEquipment);

            // Envia todos pacotes aqui, alguns envia antes, por que agora estou usando o jeito o pangya original   

            Player.Send(Login());

            Player.Send(Handle_PACKET_RESPONSE.pacote070(Player.Inventory.Characters)); // characters

            Player.Send(Handle_PACKET_RESPONSE.pacote071(Player.Inventory.Caddies)); //caddies   

            Player.Send(Player.Inventory.WarehouseItems.Build()); //inventory(warehouse)   

            Player.Send(Handle_PACKET_RESPONSE.pacote0E1(Player.Inventory.Mascots)); //mascots

            Player.Send(Handle_PACKET_RESPONSE.pacote072(Player.Inventory.UserEquipment)); // equip selected                     

            GameServer.Instance.SendChannelList(Player);

            Player.Send(Handle_PACKET_RESPONSE.pacote102(Player.UserInfo, Player.Inventory.CouponGacha));        // Pacote novo do JP, passa os coupons do Gacha JP

            // Treasure Hunter Info
            Player.Send(Handle_PACKET_RESPONSE.pacote131());

            Player.UserInfo.Achievements.sendCounterItemToPlayer(Player);

            Player.UserInfo.Achievements.sendAchievementToPlayer(Player);

            //call messenger server
            Player.Send(Handle_PACKET_RESPONSE.pacote0F1());

            Player.Send(Handle_PACKET_RESPONSE.pacote135());

            Player.Send(Handle_PACKET_RESPONSE.pacote144());        // Pacote novo do JP

            Player.Send(Handle_PACKET_RESPONSE.pacote138(Player.Inventory.Cards));

            Player.Send(Handle_PACKET_RESPONSE.pacote136());

            Player.Send(Handle_PACKET_RESPONSE.pacote137(Player.Inventory.CardEquipment));
            //call messenger server
            Player.Send(Handle_PACKET_RESPONSE.pacote13F());
            Player.Send(Handle_PACKET_RESPONSE.pacote181(Player.Inventory.ItemBuffs));
            Player.Send(Handle_PACKET_RESPONSE.pacote096(Player.UserInfo.Cookie));
            Player.Send(Handle_PACKET_RESPONSE.pacote169(Player.Inventory.CurrentTrophy, 5/*season atual*/));
            Player.Send(Handle_PACKET_RESPONSE.pacote169(Player.Inventory.RemainingTrophy));
            Player.Send(Handle_PACKET_RESPONSE.pacote0B4(Player.Inventory.CurrentSpecialTrophies, 5/*season atual*/));
            Player.Send(Handle_PACKET_RESPONSE.pacote0B4(Player.Inventory.RemainingSpecialTrophies));
            Player.Send(Handle_PACKET_RESPONSE.pacote158(Player.UserInfo.UID, Player.UserInfo.Statistics, 0));
            //// Total de season, 5 atual season  
            Player.Send(Handle_PACKET_RESPONSE.pacote25D(Player.Inventory.CurrentGrandPrixTrophies, 5/*season atual*/));
            Player.Send(Handle_PACKET_RESPONSE.pacote25D(Player.Inventory.RemainingGrandPrixTrophies, 0));

            if (/*GameServer.Instance.getInfo().Rate.LoginRewardEvent == 1 && */sLoginRewardSystem.Instance.isLoad())
                sLoginRewardSystem.Instance.CheckRewardLoginAndSend(Player);
        }

        public Packet Login()
        {
            var p = new Packet(0x44);
            p.WriteByte(0);   // Option
            p.WriteString(GameServer.Instance.getInfo().ClientVersion);
            //write struct member info Player      
            p.WriteBytes(Player.UserInfo.GetLoginInfo());//new BuildVersion
            p.WriteUInt32(Player.UserInfo.UID);
            // write struct Player(statistic)
            p.WriteBytes(Player.UserInfo.GetUserStatisticInfo());//new BuildVersion 
            // write struct Trofel Info
            p.WriteBytes(Player.Inventory.GetTrophyInfo());
            //write struct User Equip
            p.WriteBytes(Player.Inventory.GetUserEquipInfo());//new BuildVersion 
            //write struct session(statistic)
            p.WriteBytes(Player.UserInfo.GetMapStatisticInfo());
            //Equiped Items
            p.WriteBytes(Player.Inventory.GetUserEquipedItem());
            // Write Time, 16 Bytes
            p.WriteTime();
            p.WriteUInt16(0); //depois eu procuro o que é
            p.WriteBytes(Player.UserInfo.Member.PapelShop.ToArray());
            p.WriteUInt32(Player.UserInfo.Member.CountPointEvent); // CountPointEvent.
            p.WriteUInt64(Player.UserInfo.BlockFlag.Flag.Value); // ServerFlag do server para bloquear sistemas 
            p.WriteInt32(Player.Inventory.ToTalClubSetCount + Player.Inventory.TotalPartsCount);
            p.WriteUInt32(GameServer.Instance.getInfo().Property.Value);
            return p;
        }
    }
}
