using System;
using Pangya_RankingServer.Models;
using Pangya_RankingServer.UTIL;
using PangyaAPI.Network.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_RankingServer.Repository
{
	public class CmdRankRegistryCharacterInfo : Pangya_DB
	{
			public CmdRankRegistryCharacterInfo(bool _waiter = false) : base(_waiter)
			{
			}

			public virtual void Dispose()
			{

				if(! m_entry.empty())
				{
					m_entry.Clear();
				}
			}

			public RankCharacterEntry getInfo()
			{
				return m_entry;
			}

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(85, _result.cols);

            // Cria RankCharacter e CharacterInfo
            RankCharacter rc = new RankCharacter();
            CharacterInfo ce = new CharacterInfo();

            rc.setUID(IFNULL<uint>(_result.data[0]));

            if (is_valid_c_string(_result.data[1]))
                rc.setId(_result.GetString(1));

            if (is_valid_c_string(_result.data[2]))
                rc.setNickname(_result.GetString(2));

            rc.setLevel((ushort)IFNULL<uint>(_result.data[3]));

            // Inicializa CharacterInfo
            ce.id = IFNULL<int>(_result.data[4]);
            ce._typeid = IFNULL<uint>(_result.data[5]);

            for (int i = 0; i < 24; i++)
                ce.parts_id[i] = IFNULL<uint>(_result.data[6 + i]);

            for (int i = 0; i < 24; i++)
                ce.parts_typeid[i] = IFNULL<uint>(_result.data[30 + i]);

            ce.default_hair = (byte)IFNULL<uint>(_result.data[54]);
            ce.default_shirts = (byte)IFNULL<uint>(_result.data[55]);
            ce.gift_flag = (byte)IFNULL<uint>(_result.data[56]);

            for (int i = 0; i < 5; i++)
                ce.pcl[i] = (byte)IFNULL<uint>(_result.data[57 + i]);

            ce.purchase = (byte)IFNULL<uint>(_result.data[62]);

            for (int i = 0; i < 5; i++)
                ce.auxparts[i] = IFNULL<uint>(_result.data[63 + i]);

            for (int i = 0; i < 4; i++)
                ce.cut_in[i] = IFNULL<uint>(_result.data[68 + i]);

            ce.mastery = IFNULL<uint>(_result.data[72]);

            for (int i = 0; i < 4; i++)
                ce.Card_Character[i] = IFNULL<uint>(_result.data[73 + i]);

            for (int i = 0; i < 4; i++)
                ce.Card_Caddie[i] = IFNULL<uint>(_result.data[77 + i]);

            for (int i = 0; i < 4; i++)
                ce.Card_NPC[i] = IFNULL<uint>(_result.data[81 + i]);

            rc.setCharacterInfo(ce);

            uint uid = rc.getUID();

            if (m_entry.TryGetValue(uid, out var existingRC))
            {
                // Já existe um personagem
                _smp.message_pool.getInstance().push(new message(
                    $"[CmdRankRegistryCharacterInfo::lineResult][WARNING] Player[UID={uid}] " +
                    $"CHARACTER_ANT[TYPEID={existingRC.getCharacterInfo()._typeid}, ID={existingRC.getCharacterInfo().id}] " +
                    $"CHARACTER_REPLACE[TYPEID={ce._typeid}, ID={ce.id}] já tem mais de um character equipado no rank. Trocando o character antigo pelo novo.",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                m_entry[uid] = rc;
            }
            else
            {
                // Não existe, adiciona novo
                m_entry[uid] = rc; 
            }
        }


        protected override Response prepareConsulta()
			{

				if(! m_entry.empty())
				{
					m_entry.Clear();
				}

				var r = procedure(
					m_szConsulta, "");

				checkResponse(r, "Nao conseguiu pegar os registro de characters do Rank");

				return r;
			}
		 
			private RankCharacterEntry m_entry = new RankCharacterEntry();

			private string m_szConsulta = "pangya.ProcGetRankRegistryCharacterInfo";
	}
}
