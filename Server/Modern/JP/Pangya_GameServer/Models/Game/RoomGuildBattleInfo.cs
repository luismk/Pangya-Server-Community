/// create and converted by LUIS MK

using System.Text;
namespace Pangya_GameServer.Models.Game
{
    public class RoomGuildBattleInfo
    {
        public int guild_1_uid;
        public int guild_2_uid;
        public string guild_1_mark;             // mark string o pangya JP não usa aqui fica 0
        public string guild_2_mark;             // mark string o pangya JP não usa aqui fica 0
        public ushort guild_1_index_mark;
        public ushort guild_2_index_mark;
        public string guild_1_nome;
        public string guild_2_nome;
        public RoomGuildBattleInfo()
        {
            clear();
        }

        public void clear(int type = 0)
        {
            if (type == 0)
            {
                guild_1_uid = 0;
                guild_1_index_mark = 0;
                guild_1_mark = "";
                guild_1_nome = "";

                guild_2_uid = 0;
                guild_2_index_mark = 0;
                guild_2_mark = "";
                guild_2_nome = "";
            }
            if (type == 1)
            {
                guild_1_uid = 0;
                guild_1_index_mark = 0;
                guild_1_mark = "";
                guild_1_nome = "";
            }
            if (type == 2)
            {
                guild_2_uid = 0;
                guild_2_index_mark = 0;
                guild_2_mark = "";
                guild_2_nome = "";
            }
        }

        public byte[] ToArray()
        {
            using (var p = new Packet())
            {
                p.Write(guild_1_uid);
                p.Write(guild_2_uid);
                p.WriteString(guild_1_mark, 12);
                p.WriteString(guild_2_mark, 12);
                p.Write(guild_1_index_mark);
                p.Write(guild_2_index_mark);
                p.WriteString(guild_1_nome, 20);
                p.WriteString(guild_1_nome, 20);
                return p.GetBytes;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("RoomGuildBattleInfo {");
            sb.AppendLine($"  guild_1_uid = {guild_1_uid}");
            sb.AppendLine($"  guild_2_uid = {guild_2_uid}");
            sb.AppendLine($"  guild_1_mark = \"{guild_1_mark}\"");
            sb.AppendLine($"  guild_2_mark = \"{guild_2_mark}\"");
            sb.AppendLine($"  guild_1_index_mark = {guild_1_index_mark}");
            sb.AppendLine($"  guild_2_index_mark = {guild_2_index_mark}");
            sb.AppendLine($"  guild_1_nome = \"{guild_1_nome}\"");
            sb.AppendLine($"  guild_2_nome = \"{guild_2_nome}\"");
            sb.Append("}");
            return sb.ToString();
        }

    }
}
