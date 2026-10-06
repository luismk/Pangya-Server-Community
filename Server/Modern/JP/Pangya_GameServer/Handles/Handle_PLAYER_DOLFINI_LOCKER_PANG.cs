using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_DOLFINI_LOCKER_PANG : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                // Inicia o pacote 0x172 (Dolfini Locker Pang Info)
                p.init_plain(0x172); 
                p.WriteUInt64(Player.Inventory.DolfineLocker.pang); 
                Player.Send(p);
            }
            catch (exception e)
            {
                // Log de erro no console e arquivo
                _smp.LogManager.Instance.push(new message("[Handle_PLAYER_DOLFINI_LOCKER_PANG][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x172);

                // Em caso de erro, envia 0 pangs para evitar que o cliente fique aguardando ou dê crash
                p.WriteUInt64(0);

                Player.Send(p);
            }
        }
    }
}