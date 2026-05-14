using Pangya_LoginServer.PangyaEnums;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Utilities.Models;
using System;
using System.Runtime.InteropServices;

namespace Pangya_LoginServer.Models
{                 
    // PlayerInfo
    public class player_info
    {
        public player_info(uint _ul = 0u)
        {
            clear();
        }
        public void clear()
        {
            block_flag = new BlockFlag();
             id = "";
            nickname = "";
            pass = "";
        }

        public void set_info(player_info info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info), "O parâmetro 'info' não pode ser nulo.");

            uid = info.uid;
            m_cap = info.m_cap;
            block_flag = info.block_flag != null ? info.block_flag : new BlockFlag();
            pass = info.pass; 
            level = info.level; 
            id = info.id;
            nickname = info.nickname;
        }
        public uint uid;
        public uint m_cap;
        public BlockFlag block_flag = new BlockFlag();
      
        public ushort level; 
        public string id = "";
        public string nickname = "";
        public string pass = "";
        public DateTime login_time = DateTime.Now;
        public string acess_code = "302540";///chave de acesso no web cookies, esta fixo ate entao
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class LoginData
    {
        public string id;
         public string password;////aqui é short/ushort(seria o tamanho da string) + string

        public byte opt_count;
        public uint[] v_opt_unkn = new uint[4];
        public string mac_address;

        public LoginData(packet reader)
        {
            reader.ReadPStr(out id);         // ushort + string
            reader.ReadPStr(out password);   // ushort + string

            reader.ReadByte(out opt_count);

            for (int i = 0; i < (opt_count * 8) / 4; i++)
                reader.ReadUInt32(out v_opt_unkn[i]);

            reader.ReadPStr(out mac_address); // ushort + string
        }
        public override string ToString()
        {
            string data = $": [USER = {id}], [PWD = {password}], [MAC = {mac_address}]";
            return data;
        }
    }
}
