using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_UPDATE_USER_PLACE : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {

                // 2. Leitura do 'Place' (Lugar)
                sbyte newPlace = Packet.ReadSByte();

                // 3. Atualização em Memória
                Player.UserInfo.Place = newPlace;

                // 4. Persistência no Banco de Dados 
                Player.UserInfo.updateLocationDB();

                // Log de rastreamento (Opcional, útil para debugar transições de mapa/lugar)
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Handle_PLAYER_UPDATE_USER_PLACE][Warning] Normal[UID: {Player.UserInfo.UID}, STATE: {(newPlace == 2 ? "OPEN FORM" : "IN LOBBY")}] LOC.",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Handle_PLAYER_UPDATE_USER_PLACE][ErrorSystem] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        await Task.CompletedTask;
        }
    }
}