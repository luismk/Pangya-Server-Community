using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pangya_GameServer.Models
{
    using System;

    public class ctx_personal_shop
    {
        // Campos
        public uint index;
        public string name = string.Empty; // Correspondente a [Name] VARCHAR(50)
        public uint id;                     // Correspondente a [ID] INT
        public uint price;                  // Correspondente a [Price] INT
        public DateTime reg_date;           // Correspondente a [reg_date] DATETIME2(7)

        // Construtor padrão
        public ctx_personal_shop()
        {
            index = 0;
            id = 0;
            price = 0;
            name = string.Empty;
            reg_date = DateTime.MinValue;
        }

        // Limpa todos os campos
        public void clear()
        {
            index = 0;
            name = string.Empty;
            id = 0;
            price = 0;
            reg_date = DateTime.MinValue;
        }

        // Representação em string
        public string toString()
        {
            return $"Index={index}, Name={name}, ID={id}, Price={price}, RegDate={reg_date}";
        }
    }
}
