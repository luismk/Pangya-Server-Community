using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using PangyaAPI.DataBase;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
namespace Pangya_GameServer.Manager
{
    public class MailManager
    {
        public static int SendMail(uint _from_uid, uint _to_uid, string _msg)
        { 
            int msg_id = _SendMail(_from_uid, _to_uid, _msg);


            if (msg_id <= 0)
            {
                throw new exception("[MailBoxManager::sendMessage][Error] nao conseguiu criar uma msg no banco de dados", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    3, 0));
            }
            PutCommandNewMail(_to_uid,msg_id);
            return msg_id;
        }

        public static int SendMailWithItem(uint _from_uid, uint _to_uid, string _msg, List<stItem> _v_item)
        {

            int msg_id = _SendMail(_from_uid, _to_uid, _msg);

            if (msg_id <= 0)
            {
                throw new exception("[MailBoxManager::sendMessageWithItem][Error] nao conseguiu criar uma msg no banco de dados", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                3, 0));
            }

            PutItemInMail(_from_uid, _to_uid, msg_id, _v_item);

            PutCommandNewMail(_to_uid, msg_id);
            return msg_id;
        }

        public static int SendMessageWithItem(uint _from_uid, uint _to_uid, string _msg,  stItem _item)
        {
            if (_item._typeid == 0)
                return 0;

            int msg_id = _SendMail(_from_uid, _to_uid, _msg);

            if (msg_id <= 0)
            {
                throw new exception("[MailBoxManager::sendMessageWithItem][Error] nao conseguiu criar uma msg no banco de dados", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    3, 0));
            }

            PutItemInMail(_from_uid, _to_uid, msg_id, _item);

            PutCommandNewMail(_to_uid, msg_id);
            return msg_id;
        }

        public static int SendMailWithItem(uint _from_uid, uint _to_uid, string _msg, EmailInfo.ItemGift[] _pItem, uint _count)
        {

            int msg_id = _SendMail(_from_uid, _to_uid, _msg);

            if (msg_id <= 0)
            {
                throw new exception("[MailBoxManager::sendMessageWithItem][Error] nao conseguiu criar um msg no banco de dados", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    3, 0));
            }
             
            PutItemInMail(_from_uid, _to_uid, msg_id, _pItem, _count);

            PutCommandNewMail(_to_uid, msg_id);
            return msg_id;
        } 

        private static int _SendMail(uint _from_uid, uint _to_uid, string _msg)
        {

            if (_to_uid == 0u)
            {
                throw new exception("[MailBoxManager::_sendMessage][Error] UID[value=" + Convert.ToString(_to_uid) + "] to send AppMessage is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    1, 0));
            }

            if (_msg.Length == 0)
            {
                throw new exception("[MailBoxManager::_sendMessage][Error] _msg is empty", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    2, 0));
            }

            // Verifica se a string contém aspas simples
            var index = _msg.IndexOf('\'');

            if (index != -1)
            {
                // Substitui todas as ocorrências de aspas simples por duas aspas simples
                _msg = _msg.Replace("'", "''");

                // Loga a alteração no AppMessage pool
                _smp.LogManager.Instance.push(new AppMessage("[MailBoxManager::_sendMessage][Log] replace string para[str=" + _msg + "] por que tinha valores que o MSSQL nao aceita", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
             
            // cmd coloca msg no gift table
            CmdAddMsgMail cmd_amm = new CmdAddMsgMail(_from_uid, _to_uid, _msg);

            NormalManagerDB.Instance.add(0, cmd_amm, null, null);

            if (cmd_amm.getException().getCodeError() != 0)
            {
                throw cmd_amm.getException();
            }


            return cmd_amm.getMailID();
        }

        private static void PutItemInMail(uint _from_uid, uint _to_uid, int _mail_id, List<stItem> _v_item)
        {

            if (_mail_id <= 0)
            {
                throw new exception("[MailBoxManager::PutItemInMail][Error] _mail_id is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    4, 0));
            }

            if (_v_item.Count == 0)
            {
                throw new exception("[MailBoxManager::PutItemInMail][Error] vector of itens is empty", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    5, 0));
            }

            foreach (var el in _v_item)
            {
                try
                {
                    PutItemInMail(_from_uid, _to_uid, _mail_id, el);
                }
                catch (exception e)
                {
                    // Se n�o for erro de item invalid, relan�a a exception
                    if (!ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(),
                        STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                        6))
                    {
                        throw;
                    }
                }
            }
        }

        private static void PutItemInMail(uint _from_uid, uint _to_uid, int _mail_id, stItem _item)
        {

            if (_mail_id <= 0)
            {
                throw new exception("[MailBoxManager::PutItemInMail][Error] _mail_id is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    4, 0));
            }

            if (_item._typeid == 0)
            {
                // Environment.StackTrace
                throw new exception("[MailBoxManager::PutItemInMail][Error] _item is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    6, 0));
            }

            // Cmd add item
            CmdPutItemMailBox cmd_pimb = new CmdPutItemMailBox(_from_uid, // Waiter
                _to_uid, _mail_id, _item);

            NormalManagerDB.Instance.add(0,
                  cmd_pimb, null, null);

            if (cmd_pimb.getException().getCodeError() != 0)
            {
                throw cmd_pimb.getException();
            }
        }

        private static void PutItemInMail(uint _from_uid,
            uint _to_uid,
            int _mail_id,
            EmailInfo.ItemGift[] _pItem,
            uint _count)
        {

            if (_mail_id <= 0)
            {
                throw new exception("[MailBoxManager::PutItemInMail][Error] _mail_id is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    4, 0));
            }

            if (_pItem == null)
            {
                throw new exception("[MailBoxManager::PutItemInMail][Error] _pItem is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    7, 0));
            }

            if ((int)_count <= 0)
            {
                throw new exception("[MailBoxManager::PutItemInMail][Error] count[value=" + Convert.ToString(_count) + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    8, 0));
            }

            for (var i = 0; i < _count; ++i)
            {
                try
                {
                    PutItemInMail(_from_uid,
                        (_to_uid),
                        (_mail_id),
                        _pItem[i]);
                }
                catch (exception e)
                {
                    // Se n�o for erro de item invalid, relan�a a exception
                    if (!ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(),
                        STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                        6))
                    {
                        throw;
                    }
                }
            }
        }

        private static void PutItemInMail(uint _from_uid,
            uint _to_uid,
            int _mail_id,
            EmailInfo.ItemGift _item)
        {

            if (_mail_id <= 0)
            {
                throw new exception("[MailBoxManager::PutItemInMail][Error] _mail_id is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    4, 0));
            }

            if (_item._typeid == 0)
            {
                throw new exception("[MailBoxManager::PutItemInMail][Error] _item is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    6, 0));
            }

            // Cmd add item
            CmdPutItemMailBox cmd_pimb = new CmdPutItemMailBox(_from_uid, // Waiter
                _to_uid, _mail_id, _item);

            NormalManagerDB.Instance.add(0,
                  cmd_pimb, null, null);

            if (cmd_pimb.getException().getCodeError() != 0)
            {
                throw cmd_pimb.getException();
            }

            // Pronto j� colocou o item no mail do player
#if DEBUG
            _smp.message_pool.getInstance.push(new message("[MailBoxManager::PutItemInMail][Log] PLAYER[UID=" + Convert.ToString(_from_uid) + "] colocou item[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + ", QNTD=" + Convert.ToString(_item.qntd) + "] no mail[ID=" + Convert.ToString(_mail_id) + "] do PLAYER[UID=" + Convert.ToString(_to_uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
#else
				_smp.LogManager.Instance.push(new AppMessage("[MailBoxManager::PutItemInMail][Log] Normal[UID=" + Convert.ToString(_from_uid) + "] colocou item[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + ", QNTD=" + Convert.ToString(_item.qntd) + "] no mail[ID=" + Convert.ToString(_mail_id) + "] do Normal[UID=" + Convert.ToString(_to_uid) + "]", type_msg.CL_ONLY_FILE_LOG));
#endif
        }

        private static void PutCommandNewMail(uint _to_uid, int _mail_id)
        {

            if (_to_uid == 0u)
            {
                throw new exception("[MailBoxManager::putCommandNewMail][Error] UID[value=" + Convert.ToString(_to_uid) + "] to put Command. UID is invalid(zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    1, 0));
            }

            if (_mail_id <= 0)
            {
                throw new exception("[MailBoxManager::putCommandNewMail][Error] _mail_id is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    4, 0));
            }

            CommandInfo ci = new CommandInfo
            {
                id = 4 // New Mail Arrived on Mailbox of player
            };

            ci.arg[0] = (int)_to_uid;

            ci.arg[1] = _mail_id;

            ci.valid = 1;

            ci.target = 1; // Todos os game server

            NormalManagerDB.Instance.add(1,
                  new CmdInsertCommand(ci),
                  SQLDBResponse,
                  null);

        }

        private static void SQLDBResponse(int _msg_id,
                Pangya_DB _pangya_db,
                object _arg)
        {

            if (_arg == null)
            {
                return;
            }

            // Por Hora s� sai, depois fa�o outro Type de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.LogManager.Instance.push(new AppMessage("[MailBoxManager::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            switch (_msg_id)
            {
                case 1: // Insert Command
                    {
                        var cmd_ic = (CmdInsertCommand)(_pangya_db);

                        break;
                    }
                case 0:
                default:
                    break;
            }
        }
    }
}