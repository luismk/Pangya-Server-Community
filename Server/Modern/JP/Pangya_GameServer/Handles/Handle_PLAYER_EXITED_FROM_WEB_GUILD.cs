using Pangya_GameServer.Feature;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;

using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_EXITED_FROM_WEB_GUILD : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                // Verifica se tem alteração nos pangs
                ulong old_pang = Player.UserInfo.Statistics.pang;

                // Update o Pang do server com o valor que está no banco de dados
                Player.UserInfo.updatePang();

                if (old_pang != Player.UserInfo.Statistics.pang)
                {
                    // Atualiza o pangs do Player no jogo
                    Packet p = new Packet((ushort)0xC8);

                    p.WriteUInt64(Player.UserInfo.Statistics.pang);
                    p.WriteUInt64(0);

                    Player.Send(p);
                }

                // Verifica se tem alguma atualização da Guild Web para atualizar o Player no server e cliente
                // Só verifica se o Player estiver em uma Guild
                if (Player.UserInfo.Guild.uid > 0)
                {
                    CmdGuildUpdateActivityInfo cmd_guai = new CmdGuildUpdateActivityInfo(Player.UserInfo.Guild.uid,
                        Player.UserInfo.UID, true);

                    NormalManagerDB.Instance.add(0, cmd_guai, null, null);

                    if (cmd_guai.getException().getCodeError() != 0)
                    {
                        throw cmd_guai.getException();
                    }

                    var v_info = cmd_guai.getInfo();

                    if (v_info.Any())
                    {
                        Packet p = new Packet();

                        // Verifica todas as alterações que tem na Guild e trata elas
                        foreach (var el in v_info)
                        {
                            switch (el.type)
                            {
                                case GuildUpdateActivityInfo.TYPE_UPDATE.TU_ACCEPTED_MEMBER:
                                    {
                                        p.init_plain(0x01);
                                        p.WriteUInt32(el.club_uid);
                                        p.WriteUInt32(el.player_uid);

                                        GameServer.Instance.sendCommandToOtherServerWithAuthServer(p, 3);

                                        var s = GameServer.Instance.FindPlayer(el.player_uid);

                                        if (s != null)
                                        {
                                            CmdMemberInfo cmd_mi = new CmdMemberInfo(s.UserInfo.UID);

                                            NormalManagerDB.Instance.add(0, cmd_mi, null, null);

                                            if (cmd_mi.getException().getCodeError() != 0)
                                            {
                                                throw cmd_mi.getException();
                                            }

                                            var mi = cmd_mi.getInfo();

                                            if (mi.GuildIndex > 0u)
                                            {
                                                s.UserInfo.Member.GuildMarkIndex = mi.GuildMarkIndex;
                                                s.UserInfo.Member.GuildIndex = mi.GuildIndex;
                                                s.UserInfo.Member.GuildWinPangs = mi.GuildWinPangs;
                                                s.UserInfo.Member.GuildWinPoints = mi.GuildWinPoints;
                                                s.UserInfo.Member.GuildName = mi.GuildName;
                                                s.UserInfo.Member.GuildMarkImage = mi.GuildMarkImage;

                                                CmdGuildInfo cmd_gi = new CmdGuildInfo(s.UserInfo.UID, 0);

                                                NormalManagerDB.Instance.add(0, cmd_gi, null, null);

                                                if (cmd_gi.getException().getCodeError() != 0)
                                                {
                                                    throw cmd_gi.getException();
                                                }

                                                s.UserInfo.Guild = cmd_gi.getInfo();

                                                if (s.GetChannel() != null)
                                                {
                                                    s.GetChannel().UpdatePlayerInfo(s);
                                                    s.GetChannel().SendUpdatePlayerInfo(s, 3);
                                                }
                                            }
                                        }
                                        break;
                                    }
                                case GuildUpdateActivityInfo.TYPE_UPDATE.TU_EXITED_MEMBER:
                                    {
                                        p.init_plain(0x02);
                                        p.WriteUInt32(el.club_uid);
                                        p.WriteUInt32(el.player_uid);

                                        GameServer.Instance.sendCommandToOtherServerWithAuthServer(p, 3);

                                        Player.UserInfo.Guild.clear();
                                        Player.UserInfo.Member.GuildMarkIndex = 0;
                                        Player.UserInfo.Member.GuildIndex = 0;
                                        Player.UserInfo.Member.GuildWinPangs = 0;
                                        Player.UserInfo.Member.GuildWinPoints = 0;
                                        Player.UserInfo.Member.GuildName = "";
                                        Player.UserInfo.Member.GuildMarkImage = "";

                                        if (Player.GetChannel() != null)
                                        {
                                            Player.GetChannel()?.UpdatePlayerInfo(Player);
                                            Player.GetChannel()?.SendUpdatePlayerInfo(Player, 3);
                                        }
                                        break;
                                    }
                                case GuildUpdateActivityInfo.TYPE_UPDATE.TU_KICKED_MEMBER:
                                    {
                                        p.init_plain(0x03);
                                        p.WriteUInt32(el.club_uid);
                                        p.WriteUInt32(el.player_uid);

                                        GameServer.Instance.sendCommandToOtherServerWithAuthServer(p, 3);

                                        var s = GameServer.Instance.FindPlayer(el.player_uid);

                                        if (s != null)
                                        {
                                            s.UserInfo.Guild.clear();
                                            s.UserInfo.Member.GuildMarkIndex = 0;
                                            s.UserInfo.Member.GuildIndex = 0;
                                            s.UserInfo.Member.GuildWinPangs = 0;
                                            s.UserInfo.Member.GuildWinPoints = 0;
                                            s.UserInfo.Member.GuildName = "";
                                            s.UserInfo.Member.GuildMarkImage = "";

                                            if (s.GetChannel() != null)
                                            {
                                                s.GetChannel().UpdatePlayerInfo(s);
                                                s.GetChannel().SendUpdatePlayerInfo(s, 3);
                                            }
                                        }
                                        break;
                                    }
                            }

                            // Atualiza o STATE do Guild update activity por que ela já foi tratada
                            NormalManagerDB.Instance.add(27,
                                 new CmdUpdateGuildUpdateActiviy(el.index),
                                 null, null);
                        }
                    }
                }
            }
            catch (exception e)
            { 
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestExitedFromWebGuild][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
    }
}