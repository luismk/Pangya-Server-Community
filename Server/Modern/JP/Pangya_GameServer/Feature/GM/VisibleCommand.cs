using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Session;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Pangya_GameServer.Feature.GM
{
    public class VisibleCommand : IGMCommand
    {
        public async Task Execute(Player session, Packet pkt)
        {
            var channel = session.GetChannel();

            try
            {
                // 1. Leitura do estado (0 = Invisível, 1 = Visível)
                ushort visibleValue = pkt.ReadUInt16();
                byte state = (byte)(visibleValue & 1);

                // 2. Busca da sala atual (Prioriza a referência direta na sessão)
                var room = session.GetRoom();

                // 3. Validação de Segurança
                if (session.UserInfo.Member.RoomID != -1 && room == null)
                {
                    throw new exception($"[GM::Visible] Player[UID={session.UserInfo.UID}] está marcado na sala {session.UserInfo.Member.RoomID}, mas a sala não existe.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 10, 0x5700100));
                }

                // 4. Atualização de Estado
                // Atualiza tanto a ServerFlag de GM quanto a ServerFlag de estado que vai no pacote de Info
                session.m_gi.visible = state;
                session.UserInfo.Member.State.Visible = state;

                // 5. Sincronização de Rede
                // Atualiza os dados locais e envia o broadcast para os outros jogadores
                if (channel != null)
                {
                    channel.Lobby.UpdatePlayerInfo(session);
                    channel.Lobby.SendUpdatePlayerInfo(session, 3); // Tipo 3: State Update
                }

                if (room != null)
                {
                    room.UpdatePlayerInfo(session);
                    room.SendPlayerInfo(session, 3);
                }

                Debug.WriteLine(session.UserInfo.Member.State.ToString());

                // Log de Auditoria de GM
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[GM::Command] {session.UserInfo.NickName} alterou visibilidade para: {(state == 1 ? "VISÍVEL" : "INVISÍVEL")}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[VisibleCommand][Error] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            await Task.CompletedTask;
        }
    }
}