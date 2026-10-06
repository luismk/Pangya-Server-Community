using System;
using System.Linq;
using Pangya_RankingServer.Flags;
using Pangya_RankingServer.Models;
using PangyaAPI.DataBase;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_RankingServer.Repository
{
    public class CmdRankRegistryInfo : Pangya_DB
    {
        public CmdRankRegistryInfo()
        {
            this.m_entry = new RankEntry();
        }

        public RankEntry getInfo()
        {
            return m_entry;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(6, _result.cols);

            // Cria RankRegistry
            RankRegistry rr = new RankRegistry();
            rr.setUID(IFNULL<uint>(_result.data[0]));
            rr.setCurrentPosition(IFNULL<uint>(_result.data[1]));
            rr.setLastPosition(IFNULL<uint>(_result.data[2]));
            rr.setValue(IFNULL<int>(_result.data[3]));

            // Cria chave de menu
            var menu_rank = (RankMenuFlags)IFNULL<byte>(_result.data[4]);
            var menu_item = IFNULL<byte>(_result.data[5]);
            // Cria chave de menu
            var km = new KeyMenu(menu_rank, menu_item);

            var kp = new KeyPosition(rr.getUID(), rr.getCurrentPosition());

            if (m_entry.TryGetValue(km, out var rankEntryValue))
            {
                if (rankEntryValue.TryGetValue(kp, out var existingRR))
                {
                    // Já existe esse player/posição
                    if (existingRR.getUID() != rr.getUID() || existingRR.getCurrentPosition() != rr.getCurrentPosition())
                    {
                        // Substitui e loga
                        m_entry[km].Add(kp, rr);

                        _smp.LogManager.Instance.push(new AppMessage(
                            $"[CmdRankRegistryInfo::lineResult][Log] Player[UID={rr.getUID()}] Atualizou o registro no rank registry map. " +
                            $"REGISTRY_ANT[UID={existingRR.getUID()}, CURRENT_POSITION={existingRR.getCurrentPosition()}], " +
                            $"REGISTRY_NEW[UID={rr.getUID()}, CURRENT_POSITION={rr.getCurrentPosition()}].",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
                else
                {
                    m_entry[km].Add(kp, rr);
                }
            }
            else
            {
                // Não existe, cria um novo
                m_entry.Add(km, new RankEntryValue(kp, rr));
            }

        }

        protected override Response prepareConsulta()
        {

            if (m_entry.Any())
                m_entry.Clear();

            var r = procedure(m_szConsulta, "");

            checkResponse(r, "Nao conseguiu pegar os Entry do Rank");

            return r;
        }
        private RankEntry m_entry = new RankEntry();

        private string m_szConsulta = "pangya.ProcGetRankRegistryInfo";
    }
}
