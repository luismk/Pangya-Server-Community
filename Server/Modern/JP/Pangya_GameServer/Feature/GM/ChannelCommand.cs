using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Feature.GM
{
    public class ChannelCommand : IGMCommand
    {
        public async Task Execute(Player session, Packet packet)
        {
            // 1. Leitura do ID do Canal (UInt16 conforme o pacote original)
            ushort channelId = packet.ReadUInt16();
             
            // 3. Atualização do estado da Sessão e PlayerUserStatistics 
            session.m_gi.channel = (byte)channelId;
            session.UserInfo.Member.State.Channel = (byte)channelId;

            // 4. Sincronização lógica: Se Channel ON, Whisper segue o mesmo estado 
            session.m_gi.whisper = session.m_gi.channel;

            // Log de auditoria
            Console.WriteLine($"[GM-Action] {session.UserInfo.NickName} alterou monitoramento para o Canal: {channelId}");

        await Task.CompletedTask;
        }
    }
}