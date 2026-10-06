using Pangya_GameServer.Channels;
using Pangya_GameServer.Engine;
using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models.Generic;
using PangyaAPI.IFF.Regions.JP.Models.IFF;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Session;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Pangya_GameServer.Models.DefineConstants;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_KICK_PLAYER_OF_ROOM : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                uint uid = Packet.ReadUInt32();

                var r = Player.GetRoom();

                if (r == null)
                {
                    throw new exception("[Handle_PLAYER_KICK_PLAYER_OF_ROOM][Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] tentou chutar um Normal [UID=" + (uid) + "] da sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "], mas sala nao existe. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        10, 0));
                }

                if (r.GetMaster() != Player.UserInfo.UID)
                {
                    throw new exception("[Handle_PLAYER_KICK_PLAYER_OF_ROOM][Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] tentou chutar um Normal [UID=" + (uid) + "] da sala[NUMERO=" + r.GetRoomId() + "], mas o Player nao é Master da sala para poder chutar(kick) o Player. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        11, 0));
                }

                // Se não for GM, não pode kikar o Player da sala com jogo em andamento
                if (!Player.UserInfo.UserCapabilities.IsGameMaster && r.CurrentGame != null)
                {
                    throw new exception("[Handle_PLAYER_KICK_PLAYER_OF_ROOM][Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] tentou chutar um Normal [UID=" + (uid) + "] da sala[NUMERO=" + r.GetRoomId() + "], mas o Player é GM para poder chutar o Player da sala com o jogo em andamento.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        13, 0));
                }

                var PlayerKick = r.FindSessionByOid(uid);

                if (PlayerKick == null)
                {
                    throw new exception("[Handle_PLAYER_KICK_PLAYER_OF_ROOM][Error] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + "] tentou chutar um Normal [UID=" + (uid) + "] da sala[NUMERO=" + r.GetRoomId() + "], mas o Player nao existe na sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        12, 0));
                }

                if (PlayerKick.UserInfo.UID == Player.UserInfo.UID)
                {
                    // Enviamos um aviso para o chat do próprio GM em vez de dar erro fatal
                    Player.SendChatNotice("no executed, other Player");

                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[Handle_PLAYER_KICK_FROM_ROOM][Warning] GM {Player.UserInfo.NickName} tentou se auto-desconectar (Bloqueado).",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));

                    return; // Interrompe a execução aqui
                }

                // Player precisa do pacote para sair da sala
                // Não precisa verifica se é Grand Prix o multiPlayer,
                // o pacote do multiPlayer serve para kikar o Player da sala. O pacote do GP no GP buga
                // Nota: Assumindo que LeaveRoomMultiPlayer esteja acessível via contexto ou classe estática correspondente
                PlayerKick.GetChannel().LeaveRoomMultiPlayer(PlayerKick, 3);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Channel:Handle_PLAYER_KICK_PLAYER_OF_ROOM][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        await Task.CompletedTask;
        }
    }
}