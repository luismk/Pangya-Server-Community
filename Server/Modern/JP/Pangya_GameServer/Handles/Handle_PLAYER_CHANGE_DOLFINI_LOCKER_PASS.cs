using Pangya_GameServer.Repository;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_CHANGE_DOLFINI_LOCKER_PASS : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new();
            var m_ci = Player.GetChannel();
            try
            {
                // 1. Leitura das senhas antiga e nova
                string old_pass = Packet.ReadString();
                string new_pass = Packet.ReadString();

                // 2. Validação e Sanitização (Old Pass)
                if (string.IsNullOrEmpty(old_pass))
                {
                    throw new exception("[Handle_PLAYER_CHANGE_DOLFINI_LOCKER_PASS][Error] Normal[UID=" + Player.UserInfo.UID + "] tentou contra o server[MESSAGE=" + old_pass + "], vazio. Hacker ou Bug",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1));
                }

                if (!Tools.Sanitize(old_pass))
                {
                    throw new exception("[Handle_PLAYER_CHANGE_DOLFINI_LOCKER_PASS][Error] Normal[UID=" + Player.UserInfo.UID + "] tentou contra o server[MESSAGE=" + old_pass + "], tentativa de inject. Hacker ou Bug",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1));
                }

                // 3. Validação e Sanitização (New Pass)
                if (string.IsNullOrEmpty(new_pass))
                {
                    throw new exception("[Handle_PLAYER_CHANGE_DOLFINI_LOCKER_PASS][Error] Normal[UID=" + Player.UserInfo.UID + "] tentou contra o server[MESSAGE=" + new_pass + "], vazio. Hacker ou Bug",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1));
                }

                if (!Tools.Sanitize(new_pass))
                {
                    throw new exception("[Handle_PLAYER_CHANGE_DOLFINI_LOCKER_PASS][Error] Normal[UID=" + Player.UserInfo.UID + "] tentou contra o server[MESSAGE=" + new_pass + "], tentativa de inject. Hacker ou Bug",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1));
                }

                // 4. Verificação de comprimento máximo
                if (old_pass.Length > 4 || new_pass.Length > 4)
                {
                    throw new exception("[Handle_PLAYER_CHANGE_DOLFINI_LOCKER_PASS][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou trocar a Password, mas length é superior ao permitido.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 301, 5100202));
                }

                p.init_plain(0x174);

                // 5. Lógica de Troca
                if (string.CompareOrdinal(old_pass, Player.Inventory.DolfineLocker.pass) != 0)
                {
                    // Senha antiga incorreta
                    _smp.LogManager.Instance.push(new AppMessage("[Dolfini Locker::Change Pass][Success] Normal [UID=" + Player.UserInfo.UID + "] tentou trocar a Password mas a antiga[" + old_pass + "] está incorreta", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    p.WriteUInt32(1); // Código de erro: Senha antiga não confere
                }
                else
                {
                    // Senha correta: Atualiza na memória e no DB
                    Player.Inventory.DolfineLocker.pass = new_pass;
                    p.WriteUInt32(0); // Sucesso

                    _smp.LogManager.Instance.push(new AppMessage("[Dolfini Locker::Change Pass][Success] Normal [UID=" + Player.UserInfo.UID + "] trocou a Password com sucesso", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    NormalManagerDB.Instance.add(1, new CmdUpdateDolfiniLockerPass(Player.UserInfo.UID, new_pass));
                }

                Player.Send(p);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_CHANGE_DOLFINI_LOCKER_PASS][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x174);

                uint errorCode = (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL)
                    ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError())
                    : 5100200;

                p.WriteUInt32(errorCode);
                Player.Send(p);
            }
        }
    }
}