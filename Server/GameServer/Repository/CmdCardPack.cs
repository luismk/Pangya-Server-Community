using System.Collections.Generic;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdCardPack : Pangya_DB
    {
        public CmdCardPack()
        {
        }

        public Dictionary<uint, CardPack> getCardPack()
        {
            return (m_card_pack);
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(10);

            CardPack cp = new CardPack();

            cp._typeid = IFNULL(_result.data[0]);

            if (m_card_pack.TryGetValue(cp._typeid, out var existingPack))
            {
                existingPack.card.Add(new Card
                {
                    _typeid = (uint)IFNULL(_result.data[7]),
                    prob = (uint)IFNULL(_result.data[8]),
                    tipo = (CARD_TYPE)IFNULL(_result.data[9])
                });
            }
            else
            {
                cp.num = IFNULL(_result.data[1]);
                cp.volume = (byte)IFNULL(_result.data[2]);

                for (int i = 0; i < cp.rate.value.Length; ++i)
                {
                    cp.rate.value[i] = (ushort)IFNULL(_result.data[3 + i]);
                }

                cp.card.Add(new Card
                {
                    _typeid = (uint)IFNULL(_result.data[7]),
                    prob = (uint)IFNULL(_result.data[8]),
                    tipo = (CARD_TYPE)IFNULL(_result.data[9])
                });

                m_card_pack[cp._typeid] = cp;
            }
        }

        protected override Response prepareConsulta()
        {

            var r = consulta(m_szConsulta);

            checkResponse(r, "nao conseguiui pegar o(s) Card(s) Pack");

            return r;
        }


        private Dictionary<uint, CardPack> m_card_pack = new Dictionary<uint, CardPack>();

        private const string m_szConsulta = "SELECT B.typeid as CardPack, B.quantidade as qntd, B.tipo as Vol, B.rate_N, B.rate_R, B.rate_SR, B.rate_SC, 				A.typeid, A.probabilidade as prob, A.tipo FROM pangya.pangya_new_cards A INNER JOIN pangya.pangya_new_card_pack B ON A.pack = B.tipo";
    }
}