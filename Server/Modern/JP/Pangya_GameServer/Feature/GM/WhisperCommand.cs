using System;
using System.Threading.Tasks;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using Pangya_GameServer.Server;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Feature.GM
{
    public class WhisperCommand : IGMCommand
    {
        public async Task Execute(Player session, Packet packet)
        {
            // 1. Leitura do estado do Whisper (ON/OFF ou ID do canal de escuta)
            ushort whisperState = packet.ReadUInt16();
              
            session.m_gi.whisper = (byte)whisperState;
            session.UserInfo.Member.State.Whisper = (byte)whisperState; 

            session.m_gi.channel = session.m_gi.whisper;

            // Log de auditoria para o console
            Console.WriteLine($"[GM-Action] {session.UserInfo.NickName} alterou estado de WHISPER para: {whisperState}");

            await Task.CompletedTask;
        }
    }
}