using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
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
    public class Handle_PLAYER_COOKIE : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                // Sempre atualiza o Cookie do server com o valor que está no banco de dados

                // Update cookie do server com o que está no banco de dados
                Player.UserInfo.updateCookie();

                // Update ON GAME
                p.init_plain(0x96);

                p.WriteUInt64(Player.UserInfo.Cookie);

                Player.Send(p);

                // Vou colocar aqui para atualizar os Grand Zodiac Pontos por que quando eu fazer o evento o Grand Zodiac ele vai consumir os pontos na página web, 
                // aí vou atualizar aqui com o do banco de dados
                CmdGrandZodiacPontos cmd_gzp = new CmdGrandZodiacPontos(Player.UserInfo.UID,
                    CmdGrandZodiacPontos.eCMD_GRAND_ZODIAC_TYPE.CGZT_GET);

                NormalManagerDB.Instance.add(0,
                     cmd_gzp, null, null);

                if (cmd_gzp.getException().getCodeError() != 0)
                {
                    throw cmd_gzp.getException();
                }

                Player.UserInfo.GrandZodiacPoints = cmd_gzp.getPontos();

            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestCookie][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
    }
}