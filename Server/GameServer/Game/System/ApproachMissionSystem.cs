using System;
using System.Collections.Generic;
using System.Linq;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Models;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer.Game.System
{
    public class ApproachMissionSystem
    {
        private List<mission_approach_dados> m_mad; // Approach Mission dados

        private bool m_load;

        public ApproachMissionSystem()
        {
            this.m_mad = new List<mission_approach_dados>(); 
        }

        public void initialize()
        {
            CmdApproachMissions cmd_am = new CmdApproachMissions(true); // Waiter

            snmdb.NormalManagerDB.getInstance().add(0,
                cmd_am, null, null);

            if (cmd_am.getException().getCodeError() != 0)
            {
                throw cmd_am.getException();
            }
             

            m_mad = cmd_am.getInfo();

            //#ifdef _DEBUG
            if (m_mad.Count == 0)
                _smp.message_pool.getInstance().push(new message("[ApproachMissionSystem::initialize][Warning] Not Loaded!", type_msg.CL_FILE_LOG_AND_CONSOLE));

            // Carregado com sucesso
            m_load = true;  
        }

        public void clear()
        {
            // Limpa a lista de itens do Approach Mission System 
            if (!m_mad.empty())
            {
                m_mad.Clear();
            }

            m_load = false;
        }
        public void load()
        {

            if (isLoad())
            {
                clear();
            }

            initialize();
        }

        public bool isLoad()
        {

            bool isLoad = false;

            isLoad = (m_load && !m_mad.empty());
            return isLoad;
        }

        public mission_approach_ex drawMission(uint _num_players)
        {

            mission_approach_ex ma = new mission_approach_ex();

            if (!isLoad())
            {
                return ma;
            }

            var RandomNumbers = new Random();

            if (((RandomNumbers.Next() % 1000) / 10) <= 50)
            { // 50% de chance de sair mission

                var index = RandomNumbers.Next() % m_mad.Count();

                _smp.message_pool.getInstance().push(new message("[ApproachMissionSystem::drawMission][Log] Mission[Number=" + Convert.ToString(m_mad[index].numero) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (m_mad[index].flag.players < _num_players)
                {

                    ma.numero = (byte)m_mad[index].numero;
                    ma.box_qntd = (byte)m_mad[index].box;
                    ma.tipo = m_mad[index].tipo;

                    switch (m_mad[index].numero)
                    {
                        case 1:
                        case 11:
                        case 23:
                        case 25:
                            ma.condition[0] = (int)(RandomNumbers.Next() % m_mad[index].flag.condition1);
                            break;
                        case 6:
                        case 14:
                            {
                                ma.condition[0] = (int)(RandomNumbers.Next() % m_mad[index].flag.condition1 + 1);

                                // Box, aumenta se for menos dist ncia
                                if (ma.condition[0] <= 1)
                                {
                                    ma.box_qntd = 3;
                                }
                                else if (ma.condition[0] <= 3)
                                {
                                    ma.box_qntd = 2;
                                }

                                break;
                            }
                        case 7:
                            ma.condition[0] = (int)(RandomNumbers.Next() % _num_players + 1);
                            break;
                        case 10:
                            {
                                var characters = sIff.getInstance().getCharacter();

                                var choice = RandomNumbers.Next() % characters.Count();

                                var it = characters[choice]; // pega o elemento na posição escolhida

                                ma.condition[0] = (int)it.ID/* / *Typeid * /*/;
                                ma.condition[1] = (int)choice;
                                break;
                            }
                        case 15:
                            ma.condition[0] = (int)(RandomNumbers.Next() % _num_players + 1);
                            ma.condition[1] = (int)(m_mad[index].flag.condition1 == 0 ? 0 : RandomNumbers.Next() % m_mad[index].flag.condition1);
                            break;
                        case 17:
                            ma.condition[1] = (int)((_num_players < 15) ? 150 : 250);
                            break;
                        case 18:
                            ma.condition[0] = (int)(RandomNumbers.Next() % m_mad[index].flag.condition1 + 1);
                            ma.condition[1] = (int)(RandomNumbers.Next() % 9);
                            break;
                        case 29: // Player Chip-in
                            ma.is_player_uid = true;
                            ma.condition[1] = (int)(RandomNumbers.Next() % _num_players); // index player choice
                            break;
                    }
                }
            }
            return ma;
        }
        public static void SQLDBResponse(uint _msg_id,
            Pangya_DB _pangya_db,
            object _arg)
        {

            if (_arg == null)
            {
                _smp.message_pool.getInstance().push(new message("[ApproachMissionSystem::SQLDBResponse][Warning] _arg is nullptr com msg_id = " + Convert.ToString(_msg_id), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            // Por Hora s  sai, depois fa o outro tipo de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.message_pool.getInstance().push(new message("[ApproachMissionSystem::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            switch (_msg_id)
            {
                case 0:
                default:
                    break;
            }
        }

    }
    public class sApproachMissionSystem : Singleton<ApproachMissionSystem> { }
}
