using Pangya_MessengerServer.Models;
using PangyaAPI.DataBase;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities;
using static PangyaAPI.Utilities.Tools;
using Pangya_MessengerServer.Repository;
using PangyaAPI.Network.Models;

namespace Pangya_MessengerServer.Manager
{
    public class FriendManager
    {
        private bool State; // Estado
        protected Dictionary<uint, FriendInfoEx> Friends = new Dictionary<uint, FriendInfoEx>();
        protected PlayerInfoBase UserInfo = new PlayerInfoBase(); // Owner[Dono] do FriendManager

        public FriendManager()
        {
            this.UserInfo = new PlayerInfoBase();
            this.Friends = new Dictionary<uint, FriendInfoEx>();
            this.State = false;
        }

        public FriendManager(PlayerInfoBase _pi)
        {
            this.UserInfo = _pi;
            this.Friends = new Dictionary<uint, FriendInfoEx>();
            this.State = false;
        }


        public void init(PlayerInfoBase _pi)
        {

            if (isInitialized())
            {
                clear();
            }

            // Atualiza
            UserInfo = _pi;

            if (UserInfo.UID == 0)
            {
                throw new exception("[FriendManager::init][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.FRIEND_MANAGER,
                    1, 0));
            }

            CmdFriendInfo cmd_fi = new CmdFriendInfo(UserInfo.UID, // Waiter
                CmdFriendInfo.TYPE.ALL,
                0u);

            snmdb.NormalManagerDB.Instance.add(0,
                  cmd_fi, null, null);

            if (cmd_fi.getException().getCodeError() != 0)
            {
                throw cmd_fi.getException();
            }

            Friends = new Dictionary<uint, FriendInfoEx>(cmd_fi.getInfo());

            State = true;
        }

        public virtual void clear()
        {
            if (Friends.Count > 0)
            {
                Friends.Clear();
            }

            UserInfo = new PlayerInfoBase();

            State = false;
        }

        public bool isInitialized()
        {
            return State;
        }

        // Counters   
        public uint countAllFriend()
        {

            uint count = 0u;

            count = (uint)Friends.Count;

            return (uint)count;
        }

        public uint countGuildMember()
        {

            uint count = 0u;
            count = (uint)Friends.Count(el =>
            {
                return el.Value.flag.guild_member == 1;
            });
            return (uint)count;
        }

        public uint countFriend()
        {

            uint count = 0u;

            count = (uint)Friends.Count(el =>
            {
                return el.Value.flag._friend == 1;
            });

            return (uint)count;
        }

        // Request Add Friend
        public async void requestAddFriend(FriendInfoEx _fi)
        {

            // UPDATE ON SERVER
            addFriend(_fi);

            // UPDATE ON DB
            snmdb.NormalManagerDB.Instance.add(1,
                 new CmdAddFriend(UserInfo.UID, _fi),
                 FriendManager.SQLDBResponse,
                 this);
        }

        // Request Delete Friend
        public async void requestDeleteFriend(FriendInfoEx _fi)
        {
            await requestDeleteFriend(_fi.uid);
        }

        public async Task requestDeleteFriend(uint _uid)
        {
            // UPDATE ON SERVER
            deleteFriend(_uid);

            // UPDATE ON DB
            snmdb.NormalManagerDB.Instance.add(2,
                 new CmdDeleteFriend(UserInfo.UID, _uid),
                 FriendManager.SQLDBResponse,
                 this);
        }

        // Request Update Friend Info
        public async void requestUpdateFriendInfo(FriendInfoEx _fi)
        {

            if (_fi.uid == 0)
            {
                throw new exception("[FriendManager::requestUpdateFriendInfo][Error] _fi.UID is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.FRIEND_MANAGER,
                    1, 0));
            }

            // UPDATE ON DB
            snmdb.NormalManagerDB.Instance.add(3,
                 new CmdUpdateFriend(UserInfo.UID, _fi),
                 FriendManager.SQLDBResponse,
                 this);
        }

        // add Friend

        // add Friend
        public void addFriend(FriendInfoEx _fi)
        {

            if (_fi.uid == 0)
            {
                throw new exception("[FriendManager::addFriend][Error] player[UID=" + Convert.ToString(UserInfo.UID) + "] tentou adicionar um amigo[UID=" + Convert.ToString(_fi.uid) + "], mas o UID is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.FRIEND_MANAGER,
                    1, 0));
            }

            if (_fi.flag.guild_member == 1 && UserInfo.GuildIndex == 0)
            {
                throw new exception("[FriendManager::addFriend][Error] player[UID=" + Convert.ToString(UserInfo.UID) + "] tentou adicionar um Guild Member[UID=" + Convert.ToString(_fi.uid) + "], mas ele nao esta em nenhum Guild. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.FRIEND_MANAGER,
                    2, 0));
            }

            var it = Friends.FirstOrDefault(c=> c.Key == _fi.uid);

            if (!Friends.Any(c => c.Key == _fi.uid)) // add new friend ou Guild Member
            {
                Friends.Add(_fi.uid, _fi);
            }
            else if (it.Value != null && it.Value.flag.ucFlag != 3 && it.Value.flag.ucFlag != _fi.flag.ucFlag) // Add Guild Member ou Friend
            {
                it.Value.flag.ucFlag |= _fi.flag.ucFlag;
            }
            else // j� tem o amigo na Guild e em amigos
            {
                _smp.LogManager.Instance.push(new AppMessage("[FriendManager::addFriend][Error][Warning] player[UID=" + Convert.ToString(UserInfo.UID) + "] ja tem esse Amigo[UID=" + Convert.ToString(_fi.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
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
                throw new exception("[FriendManager::deleteFriend][Error] player[UID=" + Convert.ToString(UserInfo.UID) + "] tentou adicionar um amigo[UID=" + Convert.ToString(_uid) + "], mas o UID is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.FRIEND_MANAGER,
                    1, 0));
            }

            if (Friends.Any(c => c.Key == _uid))
            {
                Friends.Remove(_uid);
            }
            else
            {
                _smp.LogManager.Instance.push(new AppMessage("[FriendManager::deleteFriend][Error][Warning] player[UID=" + Convert.ToString(UserInfo.UID) + "] tentou deletat amigo[UID=" + Convert.ToString(_uid) + "] do map, mas ele nao existe no map.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        // Finders                                 
        public FriendInfoEx findFriendInAllFriend(uint _uid)
        {
            var it = Friends.FirstOrDefault(el =>
            {
                return el.Value.uid == _uid;
            });

            return it.Value;
        }

        public FriendInfoEx findGuildMember(uint _uid)
        {
            var it = Friends.FirstOrDefault(el =>
            {
                return el.Value.flag.guild_member == 1 && el.Value.uid == _uid;
            });

            return it.Value;
        }

        public FriendInfoEx findFriend(uint _uid)
        {
            var it = Friends.FirstOrDefault(el =>
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
            Friends.ToList().ForEach(el =>
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

            Friends.ToList().ForEach(el =>
            {
                if (el.Value.flag.guild_member == 1)
                {
                    v_friend.Add(el.Value);
                }
            });

            return new List<FriendInfoEx>(v_friend);
        }

         public List<FriendInfoEx> getAllFriendAndGuildMember(bool filterBlocked = false)
{
    List<FriendInfoEx> result = new List<FriendInfoEx>();

    foreach (var pair in Friends)
    {
        var friend = pair.Value;
        
        // If we aren't filtering blocks, OR if the person isn't blocked, add them
        if (!filterBlocked || friend.state.block != 1)
        {
            result.Add(friend);
        }
    }

    // TODO: Add logic for m_guildMembers here using the same pattern

    return result;
}
        public bool IsBlocked(uint uid)
        {
            var fi = findFriendInAllFriend(uid);
            if (fi != null)
                return fi.state.block == 1;

            return false;
        }

        protected static void SQLDBResponse(int _msg_id,
            Pangya_DB _pangya_db,
            object _arg)
        {

            if (_arg == null)
            {
                _smp.LogManager.Instance.push(new AppMessage("[FriendManager::SQLDBResponse][WARNING] _arg is nullptr, na msg_id = " + Convert.ToString(_msg_id), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            // Por Hora s� sai, depois fa�o outro Type de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.LogManager.Instance.push(new AppMessage("[FriendManager::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            var _server = (FriendManager)(_arg);

            switch (_msg_id)
            {
                case 1: // Add Friend
                    {
                        var cmd_af = (CmdAddFriend)(_pangya_db);

                        _smp.LogManager.Instance.push(new AppMessage("[FriendManager::SQLDBResponse][Log] player[UID=" + Convert.ToString(cmd_af.getUID()) + "] adicionou Amigo[UID=" + Convert.ToString(cmd_af.getInfo().uid) + ", APELIDO=" + (cmd_af.getInfo().apelido) + ", NICK=" + (cmd_af.getInfo().nickname) + ", STATE=" + Convert.ToString((ushort)cmd_af.getInfo().state.ucState) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 2: // Delete Friend
                    {
                        var cmd_df = (CmdDeleteFriend)(_pangya_db);

                        _smp.LogManager.Instance.push(new AppMessage("[FriendManager::SQLDBResponse][Log] player[UID=" + Convert.ToString(cmd_df.getUID()) + "] deletou Amigo[UID=" + Convert.ToString(cmd_df.getFriendUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 3: // Update Friend Info
                    {
                        var cmd_ufi = (CmdUpdateFriend)(_pangya_db);

                        _smp.LogManager.Instance.push(new AppMessage("[FriendManager::SQLDBResponse][Log] player[UID=" + Convert.ToString(cmd_ufi.getUID()) + "] atualizou Info do Amigo[UID=" + Convert.ToString(cmd_ufi.getInfo().uid) + ", APELIDO=" + (cmd_ufi.getInfo().apelido) + ", UNK1=" + Convert.ToString(cmd_ufi.getInfo().lUnknown) + ", UNK2=" + Convert.ToString(cmd_ufi.getInfo().lUnknown2) + ", UNK3=" + Convert.ToString(cmd_ufi.getInfo().lUnknown3) + ", UNK4=" + Convert.ToString(cmd_ufi.getInfo().lUnknown4) + ", UNK5=" + Convert.ToString(cmd_ufi.getInfo().lUnknown5) + ", UNK6=" + Convert.ToString(cmd_ufi.getInfo().lUnknown6) + ", UNK_FLAG=" + Convert.ToString((short)cmd_ufi.getInfo().cUnknown_flag) + ", STATE=" + Convert.ToString((byte)cmd_ufi.getInfo().state.ucState) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 0:
                default:
                    break;
            }
        }

      
    }
}
