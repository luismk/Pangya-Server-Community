using System;
using System.Runtime.InteropServices;
namespace PangyaAPI.IFF.JP.Models.Data
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class PointShop : ICloneable
    {
        // Propriedades da classe
        public uint Active { get; set; }
        public uint ID { get; set; }
        public uint Points { get; set; }   // Quantidade de pontos para trocar por itens
        public uint Quantity { get; set; } // Quantidade de itens para trocar pelos pontos
        public uint Flag { get; set; }

        // Construtor padrão
        public PointShop()
        {
            // Inicializa todas as propriedades com 0 por padrão
            Clear();
        }

        // Método para limpar todos os dados
        public void Clear()
        {
            Active = 0;
            ID = 0;
            Points = 0;
            Quantity = 0;
            Flag = 0;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}