/// create and converted by LUIS MK
namespace Pangya_GameServer.Models.Game
{
    public class RoomGrandPrixInfo
    {
        public uint dados_typeid;
        public uint rank_typeid;
        public uint tempo;
        public uint active;

        public byte[] ToArray()
        {
            using (var p = new Packet())
            {
                p.Write(dados_typeid);
                p.Write(rank_typeid);
                p.Write(tempo);
                p.Write(active);
                return p.GetBytes;
            }
        }

        public override string ToString()
        {
            return $"RoomGrandPrixInfo {{ dados_typeid = {dados_typeid}, rank_typeid = {rank_typeid}, tempo = {tempo}, State = {active} }}";
        }

    }
}
