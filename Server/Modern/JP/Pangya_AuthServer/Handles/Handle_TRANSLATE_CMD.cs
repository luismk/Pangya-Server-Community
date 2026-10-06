using Pangya_AuthServer.Handles.Commands;
using Pangya_AuthServer.Models;
using Pangya_AuthServer.Repository;
using Pangya_AuthServer.Server;
using PangyaAPI.Utilities.Log; 
namespace Pangya_AuthServer.Handles
{
    public class Handle_TRANSLATE_CMD
    {
        #region COMMANDS
        private readonly Dictionary<COMMAND_ID, ICmdHandler> _commandHandlers = new()
{
    { COMMAND_ID.BROADCAST_NOTICE,       new HandleNotice() },
    { COMMAND_ID.BROADCAST_TICKER,       new HandleTicker() },
    { COMMAND_ID.BROADCAST_CUBE_WIN,     new HandleCubeWin() }, // Segue a mesma lógica do Notice
    { COMMAND_ID.NEW_ITEM_NOTICE,        new HandleNewItem() },
    { COMMAND_ID.NEW_RATE,               new HandleNewRate() },
    { COMMAND_ID.ADM_KICK_FROM_WEBSITE,  new HandleAdmKick() },
    { COMMAND_ID.SHUTDOWN,               new HandleShutdown() },
    { COMMAND_ID.RELOAD_SYSTEM,          new HandleReloadSystem() }
};
        #endregion

        public void Handle()
        { 
            // Check Commands
            CmdCommandInfo cmd_ci = new();
            snmdb.NormalManagerDB.Instance.add(0, cmd_ci);

            if (cmd_ci.getException().getCodeError() != 0)
            {
                throw cmd_ci.getException();
            }

            TranslateCmd(cmd_ci.getInfo());
        }

        private async void TranslateCmd(List<CommandInfo> _v_ci)
        {
            try
            {
                foreach (var el in _v_ci)
                {
                    if (DateTime.Now < el.reserveDate)
                        continue;

                    if (_commandHandlers.TryGetValue((COMMAND_ID)el.id, out var handler))
                    {
                        await handler.Execute(el);
                          
                        _smp.LogManager.Instance.push(new AppMessage(
                            $"[Handle_{(COMMAND_ID)el.id}][Sucess] TARGET[UID: {el.target}, FROM: {el.arg[0]}]",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));

                        el.valid = 0;
                        snmdb.NormalManagerDB.Instance.add(1, new CmdUpdateCommand(el), AuthServer.Instance.DBResponse, AuthServer.Instance);

                    }
                    else
                    { 
                        _smp.LogManager.Instance.push(new AppMessage(
                            $"[TranslateCmd][Log] Comando não mapeado: {(COMMAND_ID)el.id}",
                            type_msg.CL_ONLY_FILE_LOG));
                    }
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[TranslateCmd][ErrorSystem] {e.Message}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
    }
}
