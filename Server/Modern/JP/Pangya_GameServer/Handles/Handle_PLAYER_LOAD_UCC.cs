using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Security;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_LOAD_UCC : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                // Filtra as UCCs válidas do Warehouse do usuário
                var allUcc = Player.Inventory.WarehouseItems.Values
                    .Where(item => item.IsUCC())
                    .Where(item =>
                    {
                        var iff = sIff.Instance.findPart(item._typeid);
                        return iff != null && (iff.type_item == PART_TYPE.UCC_DRAW_ONLY || iff.type_item == PART_TYPE.UCC_COPY_ONLY);
                    })
                    .ToList();

                if (allUcc.Count > 0)
                {
                    // antes eu usava struct, feio demais
                    var rawWriter = new Packet();
                    foreach (var item in allUcc)
                    {
                        rawWriter.WriteUInt32(item._typeid);
                        rawWriter.WriteInt32(item.id);
                        rawWriter.WriteString(item.ucc.idx ?? "", 9);//antes eu coloquei 8
                    }

                    byte[] rawData = rawWriter.GetBytes;//pega os bytes escritos ->>> allUcc.Count * 16
                    byte[] tmp = new byte[rawData.Length + 10];//ele precisa ter 10 bytes vazios..
                    uint compress_out = (uint)rawData.Length;//ele pega o tamamnho real, e compressiona so ate nessa parte do tamanho
                    //modifiquei para ficar compativel com a minha versao antiga
                    //acrisio falou que era assim(eu acho)
                    var new_size = Lzo.Compress(rawData, compress_out, tmp, compress_out + 10);
                    var p = new Packet(0x1B1);
                    p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                    p.WriteUInt32(compress_out);
                    p.WriteBytes(rawData); 
                    Player.Send(p); 
                }
                else
                {
                    SendEmptyUccLoad(Player);
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage($"[Handle_PLAYER_LOAD_UCC][Error] {e.Message}", type_msg.CL_ONLY_FILE_LOG));
                SendEmptyUccLoad(Player);
            }

        await Task.CompletedTask;
        }

        private void SendEmptyUccLoad(Player session)
        {
            var p = new Packet(0x1B1);
            // Valores padrão de erro/vazio do Season 4
            p.WriteUInt64(0x190132DC55);
            p.WriteUInt64(0x2211000000);
            p.WriteZero(13);
            p.WriteUInt32(0x1100);
            Player.Send(p);
        }
    }
}