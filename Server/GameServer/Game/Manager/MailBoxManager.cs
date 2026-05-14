using System;
using System.Collections.Generic;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Models;

using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer.Game.Manager
{
    public class MailBoxManager
    {
        public static int sendMessage(uint _from_uid,
                uint _to_uid, string _msg)
        {

            int msg_id = _sendMessage((_from_uid),
                (_to_uid),
                ref _msg);

            if (msg_id <= 0)
            {
                throw new exception("[MailBoxManager::sendMessage][Error] nao conseguiu criar uma msg no banco de dados", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    3, 0));
            }
            putCommandNewMail((_to_uid), (msg_id));
            return (msg_id);
        }

        public static int sendMessageWithItem(uint _from_uid,
        uint _to_uid, string _msg,
            List<stItem> _v_item)
        {

            int msg_id = _sendMessage((_from_uid),
                (_to_uid),
                ref _msg);

            if (msg_id <= 0)
            {
                throw new exception("[MailBoxManager::sendMessageWithItem][Error] nao conseguiu criar uma msg no banco de dados", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                3, 0));
            }

            putItemInMail((_from_uid),
            (_to_uid),
                (msg_id), _v_item);
            putCommandNewMail((_to_uid), (msg_id));
            return (msg_id);
        }

        public static int sendMessageWithItem(uint _from_uid,
        uint _to_uid, string _msg,
        stItem _item)
        {
            if (_item._typeid == 0)
                return 0;

            int msg_id = _sendMessage((_from_uid),
                (_to_uid),
                ref _msg);

            if (msg_id <= 0)
            {
                throw new exception("[MailBoxManager::sendMessageWithItem][Error] nao conseguiu criar uma msg no banco de dados", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    3, 0));
            }

            putItemInMail((_from_uid), (_to_uid), msg_id, _item);
            putCommandNewMail((_to_uid), msg_id);
            return (msg_id);
        }

        public static int sendMessageWithItem(uint _from_uid,
            uint _to_uid, string _msg,
        EmailInfo.item[] _pItem,
        uint _count)
        {

            int msg_id = _sendMessage((_from_uid),
                (_to_uid),
                ref _msg);

            if (msg_id <= 0)
            {
                throw new exception("[MailBoxManager::sendMessageWithItem][Error] nao conseguiu criar um msg no banco de dados", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    3, 0));
            }

            putItemInMail((_from_uid),
                (_to_uid),
            (msg_id), _pItem,
                (_count));
            putCommandNewMail((_to_uid), (msg_id));
            return (msg_id);
        }

        public static int sendMessageWithItem(uint _from_uid,
        uint _to_uid, string _msg,
        EmailInfo.item _item)
        {

            int msg_id = _sendMessage((_from_uid),
                (_to_uid),
                ref _msg);

            if (msg_id <= 0)
            {
                throw new exception("[MailBoxManager::sendMessageWithItem][Error] nao conseguiu criar uma msg no banco de dados", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                3, 0));
            }

            putItemInMail((_from_uid),
            (_to_uid),
                (msg_id), _item);
            putCommandNewMail((_to_uid), (msg_id));
            return (msg_id);
        }

        protected static int _sendMessage(uint _from_uid,
            uint _to_uid,
            ref string _msg)
        {

            if (_to_uid == 0u)
            {
                throw new exception("[MailBoxManager::_sendMessage][Error] uid[value=" + Convert.ToString(_to_uid) + "] to send message is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
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

                // Loga a alteração no message pool
                _smp.message_pool.getInstance().push(new message("[MailBoxManager::_sendMessage][Log] replace string para[str=" + _msg + "] por que tinha valores que o MSSQL nao aceita", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }


            // Trate for Chat not printed ex:(Kanji) CJK  

            // cmd coloca msg no gift table
            CmdAddMsgMail cmd_amm = new CmdAddMsgMail(_from_uid, _to_uid, _msg);

            snmdb.NormalManagerDB.getInstance().add(0, cmd_amm, null, null);

            if (cmd_amm.getException().getCodeError() != 0)
            {
                throw cmd_amm.getException();
            }


            return (cmd_amm.getMailID());
        }

        protected static void putItemInMail(uint _from_uid,
            uint _to_uid,
            int _mail_id,
            List<stItem> _v_item)
        {

            if (_mail_id <= 0)
            {
                throw new exception("[MailBoxManager::putItemInMail][Error] _mail_id is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    4, 0));
            }

            if (_v_item.Count == 0)
            {
                throw new exception("[MailBoxManager::putItemInMail][Error] vector of itens is empty", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    5, 0));
            }

            foreach (var el in _v_item)
            {
                try
                {
                    putItemInMail((_from_uid),
                        (_to_uid),
                        (_mail_id), el);
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

        protected static void putItemInMail(uint _from_uid,
            uint _to_uid,
            int _mail_id, stItem _item)
        {

            if (_mail_id <= 0)
            {
                throw new exception("[MailBoxManager::putItemInMail][Error] _mail_id is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    4, 0));
            }

            if (_item._typeid == 0)
            {
                // Environment.StackTrace
                throw new exception("[MailBoxManager::putItemInMail][Error] _item is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    6, 0));
            }

            // Cmd add item
            CmdPutItemMailBox cmd_pimb = new CmdPutItemMailBox(_from_uid, // Waiter
                _to_uid, _mail_id, _item);

            snmdb.NormalManagerDB.getInstance().add(0,
                cmd_pimb, null, null);

            if (cmd_pimb.getException().getCodeError() != 0)
            {
                throw cmd_pimb.getException();
            }
        }

        protected static void putItemInMail(uint _from_uid,
            uint _to_uid,
            int _mail_id,
            EmailInfo.item[] _pItem,
            uint _count)
        {

            if (_mail_id <= 0)
            {
                throw new exception("[MailBoxManager::putItemInMail][Error] _mail_id is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    4, 0));
            }

            if (_pItem == null)
            {
                throw new exception("[MailBoxManager::putItemInMail][Error] _pItem is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    7, 0));
            }

            if ((int)_count <= 0)
            {
                throw new exception("[MailBoxManager::putItemInMail][Error] count[value=" + Convert.ToString(_count) + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    8, 0));
            }

            for (var i = 0; i < _count; ++i)
            {
                try
                {
                    putItemInMail((_from_uid),
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

        protected static void putItemInMail(uint _from_uid,
            uint _to_uid,
            int _mail_id,
            EmailInfo.item _item)
        {

            if (_mail_id <= 0)
            {
                throw new exception("[MailBoxManager::putItemInMail][Error] _mail_id is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    4, 0));
            }

            if (_item._typeid == 0)
            {
                throw new exception("[MailBoxManager::putItemInMail][Error] _item is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                    6, 0));
            }

            // Cmd add item
            CmdPutItemMailBox cmd_pimb = new CmdPutItemMailBox(_from_uid, // Waiter
                _to_uid, _mail_id, _item);

            snmdb.NormalManagerDB.getInstance().add(0,
                cmd_pimb, null, null);

            if (cmd_pimb.getException().getCodeError() != 0)
            {
                throw cmd_pimb.getException();
            }

            // Pronto j� colocou o item no mail do player
#if DEBUG
            _smp.message_pool.getInstance().push(new message("[MailBoxManager::putItemInMail][Log] PLAYER[UID=" + Convert.ToString(_from_uid) + "] colocou item[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + ", QNTD=" + Convert.ToString(_item.qntd) + "] no mail[ID=" + Convert.ToString(_mail_id) + "] do PLAYER[UID=" + Convert.ToString(_to_uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
#else
				_smp.message_pool.getInstance().push(new message("[MailBoxManager::putItemInMail][Log] PLAYER[UID=" + Convert.ToString(_from_uid) + "] colocou item[TYPEID=" + Convert.ToString(_item._typeid) + ", ID=" + Convert.ToString(_item.id) + ", QNTD=" + Convert.ToString(_item.qntd) + "] no mail[ID=" + Convert.ToString(_mail_id) + "] do PLAYER[UID=" + Convert.ToString(_to_uid) + "]", type_msg.CL_ONLY_FILE_LOG));
#endif
        }

        protected static void putCommandNewMail(uint _to_uid, int _mail_id)
        {

            if (_to_uid == 0u)
            {
                throw new exception("[MailBoxManager::putCommandNewMail][Error] uid[value=" + Convert.ToString(_to_uid) + "] to put Command. uid is invalid(zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
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

            snmdb.NormalManagerDB.getInstance().add(1,
                new CmdInsertCommand(ci),
                SQLDBResponse,
                null);

        }

        protected static void SQLDBResponse(int _msg_id,
                Pangya_DB _pangya_db,
                object _arg)
        {

            if (_arg == null)
            {
                return;
            }

            // Por Hora s� sai, depois fa�o outro tipo de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.message_pool.getInstance().push(new message("[MailBoxManager::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            // isso aqui depois pode mudar para o MailBoxManager, que vou tirar de ser uma classe static e usar ela como objeto(instancia)
            var _session = (Player)(_arg);

            switch (_msg_id)
            {
                case 1: // Insert Command
                    {
                        var cmd_ic = (CmdInsertCommand)(_pangya_db);

                        _smp.message_pool.getInstance().push(new message("[MailBoxManager::SQLDBResponse][Debug] Adicionou Command[" + cmd_ic.getInfo().ToString() + "] com sucesso!", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    }
                case 0:
                default:
                    break;
            }
        }
    }
}