using System.Collections.Generic;
using System.Linq;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdBoxInfo : Pangya_DB
    {
        public CmdBoxInfo(bool _waiter = false) : base(_waiter)
        {
        }

        public Dictionary<uint, ctx_box> getInfo()
        {
            return new Dictionary<uint, ctx_box>(m_box);
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(14, (uint)_result.cols);

            ctx_box ctx_b = new ctx_box();
            ctx_box_item ctx_bi = new ctx_box_item();

            ctx_b._typeid = (uint)IFNULL(_result.data[1]);

            // Box Item
            ctx_bi._typeid = (uint)IFNULL(_result.data[7]);
            ctx_bi.numero = IFNULL<int>(_result.data[8]);
            ctx_bi.probabilidade = (uint)IFNULL(_result.data[9]);
            ctx_bi.qntd = IFNULL<int>(_result.data[10]);
            ctx_bi.raridade = (BOX_TYPE_RARETY)((byte)IFNULL(_result.data[11]));
            ctx_bi.duplicar = (byte)IFNULL(_result.data[12]);
            ctx_bi.active = (byte)IFNULL(_result.data[13]);

            if (m_box.Any(c => c.Value._typeid == ctx_b._typeid))
            {
                // J� tem essa box add O Item s�
                // Add o Item a Box  
                m_box[ctx_b._typeid].item.Add(ctx_bi);
            }
            else
            { // Ainda n�o tem essa box no map, add ela ao map

                // Inicializa as informa��es da box
                ctx_b.id = IFNULL<int>(_result.data[0]);
                ctx_b.numero = IFNULL<int>(_result.data[2]);
                ctx_b.tipo_open = (BOX_TYPE_OPEN)((byte)IFNULL(_result.data[3]));
                ctx_b.tipo = (BOX_TYPE)((byte)IFNULL(_result.data[4]));
                ctx_b.opened_typeid = (uint)IFNULL(_result.data[5]);
                if (is_valid_c_string(_result.data[6]))
                {
                    STRCPY_TO_MEMORY_FIXED_SIZE(ref ctx_b.msg,
                        sizeof(char), _result.data[6]);
                }

                // Add o Item a Box
                ctx_b.item.Add(ctx_bi);

                // Add a Box ao map
                m_box.Add(ctx_b._typeid, ctx_b);
            }
        }

        protected override Response prepareConsulta()
        {

            var r = procedure(m_szConsulta, "");

            checkResponse(r, "nao conseguiu pegar info das box para o sistema de box");

            return r;
        }

        private Dictionary<uint, ctx_box> m_box = new Dictionary<uint, ctx_box>();

        private const string m_szConsulta = "pangya.ProcGetBoxInfo";
    }
}