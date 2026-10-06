using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Repository;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_CHECK_NICK : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            NICK_CHECK nc = NICK_CHECK.SUCCESS;
            string nick = string.Empty;

            byte opt = 0;
            byte error = 2;

            PlayerMemberInfo mi = null;

            try
            {
                opt = Packet.ReadByte();

                if (opt != 0)
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                       $"[Lobby::requestCheckNick][WARNING] Player[UID={Player.UserInfo.UID}] Pediu para Check Nickname: {nick}, [OPT={opt}] diferente de 0.",
                       type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                nick = Packet.ReadPStr();

                _smp.LogManager.Instance.push(new AppMessage($"[Lobby::requestCheckNick][Log] Player[UID={Player.UserInfo.UID}, IGN_CHECK={nick}]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (nc == NICK_CHECK.SUCCESS && Regex.IsMatch(nick, @".*[ ].*"))
                {
                    nc = NICK_CHECK.EMPETY_ERROR;

                    _smp.LogManager.Instance.push(new AppMessage(
                       $"[Lobby::requestCheckNick][Log] Player[UID={Player.UserInfo.UID}] Pediu para verificar o nick contem espaco em branco: {nick}",
                       type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                if ((nc == NICK_CHECK.SUCCESS && nick.Length < 4) ||
                    Regex.IsMatch(nick, @".*[\^$&,\\?`´~\|""@#¨'%*!\\].*"))
                {
                    nc = NICK_CHECK.INCORRECT_NICK;

                    _smp.LogManager.Instance.push(new AppMessage(
                       $"[Lobby::requestCheckNick][Log] Player[UID={Player.UserInfo.UID}] Pediu para verificar o nick é menor que 4 letras ou tem caracteres que nao pode: {nick}",
                       type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                if (nc == NICK_CHECK.SUCCESS)
                {
                    var cmd_vn = new CmdVerifyNick(nick); // Waiter
                    NormalManagerDB.Instance.add(0, cmd_vn, null, null);

                    if (cmd_vn.getException().getCodeError() != 0)
                        throw cmd_vn.getException();

                    if (cmd_vn.getLastCheck())
                    {
                        nc = NICK_CHECK.NICK_IN_USE;

                        error = (nc == NICK_CHECK.NICK_IN_USE && cmd_vn.getUID() != 0 ? (byte)0 : (byte)2);

                        var cmd_mi = new CmdMemberInfo(cmd_vn.getUID()); // Waiter
                        NormalManagerDB.Instance.add(0, cmd_mi, null, null);

                        if (cmd_mi.getException().getCodeError() != 0)
                            throw cmd_mi.getException();

                        mi = cmd_mi.getInfo();

                        _smp.LogManager.Instance.push(new AppMessage(
                           $"[Lobby::requestCheckNick][Log] Player[UID={Player.UserInfo.UID}] Pediu para verificar o nick ja esta em uso: {nick}",
                           type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }
            catch (exception e) // sua exception customizada
            {
                _smp.LogManager.Instance.push(new AppMessage(
                   $"[Lobby::requestCheckNick][ErrorSystem] {e.getFullMessageError()}",
                   type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.PANGYA_DB)
                    nc = NICK_CHECK.ERROR_DB;
                else
                    nc = NICK_CHECK.UNKNOWN_ERROR;
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                   $"[Lobby::requestCheckNick][ErrorSystem] {e.Message}",
                   type_msg.CL_FILE_LOG_AND_CONSOLE));

                nc = NICK_CHECK.UNKNOWN_ERROR;
            }

            try
            {
                Packet p = new Packet(0xA1);

                p.WriteByte(error);

                if (error == 0 && nc == NICK_CHECK.NICK_IN_USE)
                {
                    p.WriteUInt32(mi.UID);
                    p.WriteBytes(mi.ToArray());
                }

                Player.Send(p);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                   $"[Lobby::requestCheckNick][ErrorSystem] {e.getFullMessageError()}",
                   type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
    }
}