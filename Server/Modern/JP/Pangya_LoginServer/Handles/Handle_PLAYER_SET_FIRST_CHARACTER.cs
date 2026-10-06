using Pangya_LoginServer.DataBase;

using Pangya_LoginServer.Session;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Models;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;

namespace Pangya_LoginServer.Handles
{
    public class Handle_PLAYER_SET_FIRST_CHARACTER : HandleBase<Player, Packet_EXAMPLE>
    {
        /// <summary>
        /// Handler para seleção do primeiro personagem e roupas iniciais (0x08)
        /// </summary>
        public override async Task Handle()
        {
            if (!sIff.Instance.isLoad())
                sIff.Instance.Init();
            try
            {
                // 1. Leitura dos dados do pacote
                uint _typeid = Packet.ReadUInt32();
                byte default_hair = Packet.ReadByte();
                byte default_shirts = Packet.ReadByte();

                // 2. Validações de IFF (Integridade dos dados)
                // Verifica se o Character existe no IFF do Pangya Fun
                if (sIff.Instance.findCharacter(_typeid) == null)
                    throw new exception("[typeid character: " + (_typeid) + " is worng.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.LOGIN_SERVER, 21, 0));

                if (default_hair > 9)
                    throw new exception("[default_hair: " + (default_hair) + " is wrong. character: " + (_typeid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.LOGIN_SERVER, 22, 0));

                if (default_shirts != 0)
                    throw new exception("[default_shirts: " + (default_shirts) + " is wrong. character: " + (_typeid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.LOGIN_SERVER, 23, 0));

                // 3. Preparação do Objeto Character
                CharacterInfo ci = new(_typeid, default_hair, default_shirts);

                // Inicializa as partes padrão (Combo/Parts)
                ci.initComboDef();

                uint uid = Player.UserInfo.UID;

                // 4. Persistência no Banco de Dados (Async)
                CommandDB.AddFirstSet(uid);

                // Adiciona o personagem e recupera o objeto com o ID real do DB
                ci = CommandDB.AddCharacter(uid, ci, 0, 1);

                // Define este novo personagem como o equipado atualmente
                CommandDB.UpdateCharacterEquiped(uid, ci.id);

                // 5. Resposta ao Cliente
                // Envia pacote de confirmação (Geralmente 0x11 no login)
                Player.Send(Handle_PACKET_RESPONSE.pacote011());

                // Finaliza o fluxo e entra no jogo
               await Handle_PLAYER_LOGIN.SUCCESS_LOGIN(Player);
            }
            catch (exception e)
            {
                // Em caso de erro, avisa o cliente e loga no console
                Player.Send(Handle_PACKET_RESPONSE.pacote011()); // Envia 011 para destravar o cliente
                Player.Send(Handle_PACKET_RESPONSE.pacote00E(Player, "", 12, 500051));

                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Handle_PLAYER_SELECT_CHARACTER][Error] UID {Player.UserInfo.UID}: {e.Message}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE)
                );
            }
            await Task.CompletedTask;
        }
    }
}