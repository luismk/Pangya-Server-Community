using Pangya_MessengerServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities;
using static PangyaAPI.Utilities.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PangyaAPI.SQL.Manager;
using Pangya_MessengerServer.Repository;

namespace Pangya_MessengerServer.Manager
{
    public class FriendManager
    {
        public FriendManager()
        {
            this.m_pi = new player_info(0);
            this.m_friend = new Dictionary<uint, FriendInfoEx>();
            this.m_state = false; 
        }

        public FriendManager(player_info _pi)
        {
            this.m_pi = _pi;
            this.m_friend = new Dictionary<uint, FriendInfoEx>();
            this.m_state = false;     
        }
                                                                              

        public virtual void init(player_info _pi)
        {

            if (isInitialized())
            {
                clear();
            }

            // Atualiza
            m_pi = _pi;

            if (m_pi.uid == 0)
            {
                throw new exception("[FriendManager::init][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.FRIEND_MANAGER,
                    1, 0));
            }

            CmdFriendInfo cmd_fi = new CmdFriendInfo(m_pi.uid, // Waiter
                CmdFriendInfo.TYPE.ALL,
                0u);

            snmdb.NormalManagerDB.getInstance().add(0,
                cmd_fi, null, null);

            if (cmd_fi.getException().getCodeError() != 0)
            {
                throw cmd_fi.getException();
            }

            m_friend = new Dictionary<uint, FriendInfoEx>(cmd_fi.getInfo());

            m_state = true;
        }

        public virtual void clear()
        {
            if (m_friend.Count > 0)
            {
                m_friend.Clear();
            }

            m_pi = new player_info();

            m_state = false;
        }

        public bool isInitialized()
        {
            return m_state;
        }

        // Counters   
        public uint countAllFriend()
        {

            uint count = 0u;
                       
            count = (uint)m_friend.Count;
                                              
            return (uint)count;
        }

        public uint countGuildMember()
        {

            uint count = 0u;                                                                
            count = (uint)m_friend.Count(el =>
            {
                return el.Value.flag.guild_member == 1;
            });                                                                              
            return (uint)count;
        }

        public uint countFriend()
        {

            uint count = 0u;                                                                  

            count = (uint)m_friend.Count(el =>
            {
                return el.Value.flag._friend == 1;
            });
             
            return (uint)count;
        }

        // Request Add Friend
        public void requestAddFriend(FriendInfoEx _fi)
        {

            // UPDATE ON SERVER
            addFriend(_fi);

            // UPDATE ON DB
            snmdb.NormalManagerDB.getInstance().add(1,
                new CmdAddFriend(m_pi.uid, _fi),
                FriendManager.SQLDBResponse,
                this);
        }

        // Request Delete Friend
        public void requestDeleteFriend(FriendInfoEx _fi)
        {                 
            requestDeleteFriend(_fi.uid);
        }

        public void requestDeleteFriend(uint _uid)
        {                  
            // UPDATE ON SERVER
            deleteFriend(_uid);

            // UPDATE ON DB
            snmdb.NormalManagerDB.getInstance().add(2,
                new CmdDeleteFriend(m_pi.uid, _uid),
                FriendManager.SQLDBResponse,
                this);
        }

        // Request Update Friend Info
        public void requestUpdateFriendInfo(FriendInfoEx _fi)
        {

            if (_fi.uid == 0)
            {
                throw new exception("[FriendManager::requestUpdateFriendInfo][Error] _fi.uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.FRIEND_MANAGER,
                    1, 0));
            }

            // UPDATE ON DB
            snmdb.NormalManagerDB.getInstance().add(3,
                new CmdUpdateFriend(m_pi.uid, _fi),
                FriendManager.SQLDBResponse,
                this);
        }

        // add Friend

        // add Friend
        public void addFriend(FriendInfoEx _fi)
        {

            if (_fi.uid == 0)
            {
                throw new exception("[FriendManager::addFriend][Error] player[UID=" + Convert.ToString(m_pi.uid) + "] tentou adicionar um amigo[UID=" + Convert.ToString(_fi.uid) + "], mas o uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.FRIEND_MANAGER,
                    1, 0));
            }

            if (_fi.flag.guild_member  == 1 && m_pi.guild_uid == 0)
            {
                throw new exception("[FriendManager::addFriend][Error] player[UID=" + Convert.ToString(m_pi.uid) + "] tentou adicionar um Guild Member[UID=" + Convert.ToString(_fi.uid) + "], mas ele nao esta em nenhum Guild. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.FRIEND_MANAGER,
                    2, 0));
            }
                                                      
            var it = m_friend.find(_fi.uid);
             
            if (!m_friend.Any(c => c.Key == _fi.uid)) // add new friend ou Guild Member
            {
                m_friend.Add(_fi.uid, _fi);
            }
            else if (it.Value.flag.ucFlag != 3 && it.Value.flag.ucFlag != _fi.flag.ucFlag) // Add Guild Member ou Friend
            {
                 it.Value.flag.ucFlag |= _fi.flag.ucFlag;
            }
            else // j� tem o amigo na guild e em amigos
            {
                _smp.message_pool.getInstance().push(new message("[FriendManager::addFriend][Error][Warning] player[UID=" + Convert.ToString(m_pi.uid) + "] ja tem esse Amigo[UID=" + Convert.ToString(_fi.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }                                           
        }

        // delete Friend

        // delete Friend
        public void deleteFriend(FriendInfoEx _fi)
        {

            deleteFriend(_fi.uid);
        }

        public void deleteFriend(uint _uid)
        {

            if (_uid == 0)
            {
                throw new exception("[FriendManager::deleteFriend][Error] player[UID=" + Convert.ToString(m_pi.uid) + "] tentou adicionar um amigo[UID=" + Convert.ToString(_uid) + "], mas o uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.FRIEND_MANAGER,
                    1, 0));
            }
  
             if (m_friend.Any(c => c.Key == _uid))
            {
                m_friend.Remove(_uid);
            }
            else
            {
                _smp.message_pool.getInstance().push(new message("[FriendManager::deleteFriend][Error][Warning] player[UID=" + Convert.ToString(m_pi.uid) + "] tentou deletat amigo[UID=" + Convert.ToString(_uid) + "] do map, mas ele nao existe no map.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }  
        }

        // Finders                                 
        public FriendInfoEx findFriendInAllFriend(uint _uid)
        {                                                                                 
            var it = m_friend.FirstOrDefault(el =>
            {
                return el.Value.uid == _uid;
            });
             
            return it.Value;
        }

        public FriendInfoEx findGuildMember(uint _uid)
        {                                                                                  
            var it = m_friend.FirstOrDefault(el =>
            {
                return el.Value.flag.guild_member == 1 && el.Value.uid == _uid;
            });
                                 
            return it.Value;
        }

        public FriendInfoEx findFriend(uint _uid)
        {              
            var it = m_friend.FirstOrDefault(el =>
            {
                return el.Value.flag._friend == 1 && el.Value.uid == _uid;
            });               

            return it.Value;
        }

        // Gets
        public List<FriendInfoEx> getAllFriend(bool _block = false)
        {

            List<FriendInfoEx> v_friend = new List<FriendInfoEx>();
                               
            // Os Amigos que n�o estiverem bloqueados
            m_friend.ToList().ForEach(el =>
            {
                if (el.Value.flag._friend == 1 && (!_block || !(el.Value.state.block == 1)))
                {
                    v_friend.Add(el.Value);
                }
            });
                                                              
            return new List<FriendInfoEx>(v_friend);
        }

        public List<FriendInfoEx> getAllGuildMember()
        {

            List<FriendInfoEx> v_friend = new List<FriendInfoEx>();
                                                               
            m_friend.ToList().ForEach(el =>
            {
                if (el.Value.flag.guild_member == 1)
                {
                    v_friend.Add(el.Value);
                }
            });
                                                                                                                              
            return new List<FriendInfoEx>(v_friend);
        }

        public List<FriendInfoEx> getAllFriendAndGuildMember(bool _block = false)
        {

            List<FriendInfoEx> v_friend = new List<FriendInfoEx>();
                                              
            // Os Amigos que n�o estiverem bloqueados
            m_friend.ToList().ForEach(el =>
            {
                if (!_block || !(el.Value.state.block == 1))
                {
                    v_friend.Add(el.Value);
                }
            });  
            return new List<FriendInfoEx>(v_friend);
        }

        protected Dictionary<uint, FriendInfoEx> m_friend = new Dictionary<uint, FriendInfoEx>();
        protected player_info m_pi = new player_info(); // Owner[Dono] do FriendManager

        protected static void SQLDBResponse(int _msg_id,
            Pangya_DB _pangya_db,
            object _arg)
        {

            if (_arg == null)
            {
                _smp.message_pool.getInstance().push(new message("[FriendManager::SQLDBResponse][WARNING] _arg is nullptr, na msg_id = " + Convert.ToString(_msg_id), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            // Por Hora s� sai, depois fa�o outro tipo de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.message_pool.getInstance().push(new message("[FriendManager::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

             var _server = (FriendManager)(_arg);

            switch (_msg_id)
            {
                case 1: // Add Friend
                    {
                         var cmd_af =(CmdAddFriend)(_pangya_db);

                        _smp.message_pool.getInstance().push(new message("[FriendManager::SQLDBResponse][Log] player[UID=" + Convert.ToString(cmd_af.getUID()) + "] adicionou Amigo[UID=" + Convert.ToString(cmd_af.getInfo().uid) + ", APELIDO=" + (cmd_af.getInfo().apelido) + ", NICK=" + (cmd_af.getInfo().nickname) + ", STATE=" + Convert.ToString((ushort)cmd_af.getInfo().state.ucState) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 2: // Delete Friend
                    {
                         var cmd_df =(CmdDeleteFriend)(_pangya_db);

                        _smp.message_pool.getInstance().push(new message("[FriendManager::SQLDBResponse][Log] player[UID=" + Convert.ToString(cmd_df.getUID()) + "] deletou Amigo[UID=" + Convert.ToString(cmd_df.getFriendUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 3: // Update Friend Info
                    {
                         var cmd_ufi =(CmdUpdateFriend)(_pangya_db);

                        _smp.message_pool.getInstance().push(new message("[FriendManager::SQLDBResponse][Log] player[UID=" + Convert.ToString(cmd_ufi.getUID()) + "] atualizou Info do Amigo[UID=" + Convert.ToString(cmd_ufi.getInfo().uid) + ", APELIDO=" + (cmd_ufi.getInfo().apelido) + ", UNK1=" + Convert.ToString(cmd_ufi.getInfo().lUnknown) + ", UNK2=" + Convert.ToString(cmd_ufi.getInfo().lUnknown2) + ", UNK3=" + Convert.ToString(cmd_ufi.getInfo().lUnknown3) + ", UNK4=" + Convert.ToString(cmd_ufi.getInfo().lUnknown4) + ", UNK5=" + Convert.ToString(cmd_ufi.getInfo().lUnknown5) + ", UNK6=" + Convert.ToString(cmd_ufi.getInfo().lUnknown6) + ", UNK_FLAG=" + Convert.ToString((short)cmd_ufi.getInfo().cUnknown_flag) + ", STATE=" + Convert.ToString((byte)cmd_ufi.getInfo().state.ucState) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 0:
                default:
                    break;
            }
        }        
        private bool m_state; // Estado
    }
}
