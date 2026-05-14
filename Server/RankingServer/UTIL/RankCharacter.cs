using PangyaAPI.Network.Models;
using PangyaAPI.Utilities.Models;
using System;
using System.Collections.Generic;

namespace Pangya_RankingServer.UTIL
{
    public class RankCharacter : IDisposable
    {
        protected uint m_uid;
        protected string m_id = string.Empty;
        protected string m_nickname = string.Empty;
        protected ushort m_level;

        protected byte m_term_s5_type; // Opções descontinuadas no Fresh UP!, porém ainda mantidas nos packets
        protected byte m_class_type;   // Opções descontinuadas no Fresh UP!, porém ainda mantidas nos packets

        // Character Info
        protected CharacterInfo m_ci = new CharacterInfo();

        public RankCharacter()
        {
            m_uid = 0;
            m_id = string.Empty;
            m_nickname = string.Empty;
            m_level = 0;
            m_ci = new CharacterInfo();
            m_term_s5_type = 0;
            m_class_type = 0;
        }

        public RankCharacter(uint _uid,
            string _id, string _nickname,
            ushort _level,
            CharacterInfo _ci,
            byte _term_s5_type = 0,
            byte _class_type = 0)
        {
            m_uid = _uid;
            m_id = _id ?? string.Empty;
            m_nickname = _nickname ?? string.Empty;
            m_level = _level;
            m_ci = _ci ?? new CharacterInfo();
            m_term_s5_type = _term_s5_type;
            m_class_type = _class_type;
        }

        public virtual void Dispose()
        {
            clear();
        }

        public void clear()
        {
            m_uid = 0;
            m_id = string.Empty;
            m_nickname = string.Empty;
            m_level = 0;
            m_term_s5_type = 0;
            m_class_type = 0;
            m_ci.clear();
        }

        public void playerInfoToPacket(PangyaBinaryWriter _packet)
        {
            _packet.WriteByte(m_level);
            _packet.WriteByte(m_term_s5_type);
            _packet.WriteByte(m_class_type);
            _packet.WriteString(m_id);
            _packet.WriteString(m_nickname);
        }

        public void playerFullInfoPacket(PangyaBinaryWriter _packet)
        {
            _packet.WriteUInt32(m_uid);
            _packet.WriteString(m_id, 22);
            _packet.WriteString(m_nickname, 22);
            _packet.WriteUInt16(m_level);
        }

        public void playerCharacterInfoToPacket(PangyaBinaryWriter _packet)
        {
            _packet.WriteBytes(m_ci.ToArray());
        }

        // Get
        public uint getUID() => m_uid;
        public string getId() => m_id;
        public string getNickname() => m_nickname;
        public ushort getLevel() => m_level;
        public byte getTermS5Type() => m_term_s5_type;
        public byte getClassType() => m_class_type;
        public CharacterInfo getCharacterInfo() => m_ci;

        // Set
        public void setUID(uint _uid) => m_uid = _uid;
        public void setId(string _id) => m_id = _id ?? string.Empty;
        public void setNickname(string _nickname) => m_nickname = _nickname ?? string.Empty;
        public void setLevel(ushort _level) => m_level = _level;
        public void setTermS5Type(byte _term_s5_type) => m_term_s5_type = _term_s5_type;
        public void setClassType(byte _class_type) => m_class_type = _class_type;
        public void setCharacterInfo(CharacterInfo _ci) => m_ci = _ci ?? new CharacterInfo();

    }

    // Rank Character Entry
    public class RankCharacterEntry : Dictionary<uint /*UID*/, RankCharacter>
    { }
}
