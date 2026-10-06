using PangyaAPI.DataBase;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdateWebShopPoint : Pangya_DB
    {
        private uint m_uid;
        private long m_points_to_add; 
        /// <summary>
        /// Atualiza os pontos de um jogador que já possui registro na tabela.
        /// </summary>
        /// <param Name="_uid">ID do Jogador</param>
        /// <param Name="_points_to_add">Quantidade de pontos a SOMAR ao atual</param>
        /// <param Name="_limit_buy">Novo limite de compra</param>
        public CmdUpdateWebShopPoint(uint _uid, long _points_to_add)
        {
            this.m_uid = _uid;
            this.m_points_to_add = _points_to_add; 
        }

        public CmdUpdateWebShopPoint()
        {
            this.m_uid = 0u;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            // Update não retorna linhas de dados
            return;
        }

        protected override Response prepareConsulta()
        {
            if (m_uid == 0u)
            {
                throw new exception("[CmdUpdateWebShopPoint::prepareConsulta][Error] UID inválido (0).",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 4, 0));
            }

            // Query SQL direta para UPDATE
            // Usamos += (points = points + X) para garantir que o ganho de pontos seja acumulativo
            string query = string.Format(
                "UPDATE [pangya].[pangya_point_event] " +
                "SET [points] = [points] + {1} " +
                "WHERE [UID] = {0}",
                m_uid, m_points_to_add
            );

            // Executa via método _update
            var r = _update(query);

            // Valida se o comando foi aceito pelo SQL
            checkResponse(r, "Não foi possível atualizar os pontos do Normal[UID: " + m_uid + "]");

            return r;
        }
    }

    public class CmdWebShopPoint : Pangya_DB
    {
        // Estrutura para segurar o resultado que vem do banco
        private long m_points = 0;
        private uint m_uid = 0;
        private long m_add_points = 0;
        private int m_limit_buy = 0;

        public CmdWebShopPoint(uint _uid, long _add_points = 0, int _limit_buy = 5)
        {
            this.m_uid = _uid;
            this.m_add_points = _add_points;
            this.m_limit_buy = _limit_buy;
        }

        public long getPoints()
        {
            return m_points;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            // Aqui pegamos o valor que o SELECT da Procedure retornou
            m_points = _result.GetInt64(0);
        }

        protected override Response prepareConsulta()
        {
            if (m_uid == 0)
            {
                throw new exception("[CmdWebShopPoint::prepareConsulta][Error] m_uid is invalid",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 4, 0));
            }
             
            var r = procedure(m_szConsulta,
                m_uid.ToString() + ", " +
                m_add_points.ToString() + ", " +
                m_limit_buy.ToString());

            checkResponse(r, "nao conseguiu processar PointEvent para o Normal[UID: " + m_uid + "]");

            return r;
        }

        // Nome da procedure que criamos no passo anterior
        private const string m_szConsulta = "pangya.USP_WEB_EVENT_SHOP";
    }
}