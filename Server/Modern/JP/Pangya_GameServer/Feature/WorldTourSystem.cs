using Pangya_GameServer.Engine;
using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using PangyaAPI.DataBase;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer.Feature
{
    public class WorldTourSystem
    {
        private world_tour_config m_wte = new world_tour_config(); // Config do evento
        private bool m_load; // Se o sistema já foi carregado

        public WorldTourSystem()
        {
            this.m_load = false;
        }

        // Carrega o evento
        public void load()
        {
            if (isLoad())
            {
                clear();
            }

            initialize();
        }

        // Retorna true se o sistema está carregado e pronto
        public bool isLoad()
        {
            return m_load && m_wte != null && m_wte.Id > 0;
        }

        // Inicializa: carrega a configuração do evento
        private void initialize()
        {
            var cmd_wt_event = new CmdWorldTourConfigEvent(); // Pode adicionar waiter se quiser

            NormalManagerDB.Instance.add(0, cmd_wt_event, null, null);

            if (cmd_wt_event.getException().getCodeError() != 0)
                throw cmd_wt_event.getException();

            m_wte = cmd_wt_event.GetConfig();

            if (m_wte == null || m_wte.Id == 0)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    "[WorldTourSystem::initialize][Warning] Not Loaded.",
                    type_msg.CL_ONLY_CONSOLE_DEBUG));
                return;
            }

            m_load = true;
        }

        // Limpa dados e reseta estado
        protected void clear()
        {
            //Monitor.Exit(m_cs);

            m_wte = new world_tour_config();
            m_load = false;

            //Monitor.Exit(m_cs);
        }

        // ---------------------------
        // Aqui começam os métodos do evento
        // ---------------------------

        // Marca o progresso de um jogador em um curso/mapa
        public async void MarkCourseCompleted(int uid, int course)
        {
            if (!isLoad())
                return;

            var cmd = new CmdUpdateWorldTourEvent(uid, course, true, DateTime.Now);
            NormalManagerDB.Instance.add(1, cmd, SQLDBResponse, this);
        }

        protected static void SQLDBResponse(int _msg_id,
            Pangya_DB _pangya_db,
            object _arg)
        {

            if (_arg == null)
            {
#if DEBUG
                // Static class
                _smp.message_pool.getInstance.push(new message("[WorldTourSystem::SQLDBResponse][Warning] _arg is nullptr na msg_id = " + Convert.ToString(_msg_id), type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif // _DEBUG
                return;
            }

            // Por Hora s� sai, depois fa�o outro Type de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.LogManager.Instance.push(new AppMessage("[WorldTourSystem::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            var gts = (WorldTourSystem)(_arg);

            switch (_msg_id)
            {
                case 1: // Update Wte Player
                    {

                        var cmd_ulr = (CmdUpdateWorldTourEvent)(_pangya_db);

                        // Log
                        _smp.LogManager.Instance.push(new AppMessage("[WorldTourSystem::SQLDBResponse][Debug] Normal[UID=" + Convert.ToString(cmd_ulr.getUID()) + ", FINISH_EVENT=" + (cmd_ulr.getIsEnd() ? "TRUE" : "FALSE") + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    }
                case 2: // Update Wte Config
                    {

                        var cmd_ulrp = (CmdWorldTourConfigEvent)(_pangya_db);

                        // Log
                        _smp.LogManager.Instance.push(new AppMessage("[WorldTourSystem::SQLDBResponse][Debug] Atualizou a Config[" + cmd_ulrp.GetConfig().Id + "].", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    }
                case 0:
                default:
                    break;
            }
        }
    }

    // Singleton
    public class sWorldTourSystem : Singleton<WorldTourSystem> { }

}
