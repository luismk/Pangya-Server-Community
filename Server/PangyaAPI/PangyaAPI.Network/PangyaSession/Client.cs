using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Network.PangyaServer;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace PangyaAPI.Network.PangyaSession
{
    public abstract class Client : pangya_packet_handle
    {
        // Estados do cliente
        

        private SessionManager m_session_manager;

        public ServerState m_state;
        public int m_continue_monitor = 1;
        public int m_continue_send_msg = 1;

        private Task m_thread_monitor; 
        private CancellationTokenSource m_cts = new CancellationTokenSource();

        public Client(SessionManager _session_manager)
        {
            this.m_session_manager = _session_manager;
            this.m_state = ServerState.Uninitialized;

            try
            {
                m_continue_monitor = 1;
               m_continue_send_msg = 1;

                // Monitor Thread - Usando Task para simular o comportamento de thread de monitoramento
                m_thread_monitor = Task.Factory.StartNew(() => monitor(), m_cts.Token,
                    TaskCreationOptions.LongRunning, TaskScheduler.Default);

                m_state = ServerState.Initialized;
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[client::client][Error] client inicializado com error: " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
                m_state = ServerState.Failure;
            }
        }
         
        public uint monitor()
        {
            try
            {
                _smp.message_pool.getInstance().push(new message("monitor iniciado com sucesso!", type_msg.CL_FILE_LOG_AND_CONSOLE));

                while (m_continue_monitor == 1)
                {
                    try
                    {
                        // No C#, a verificação de "isLive" em Tasks é feita via IsCompleted
                        // Verificaria threads customizadas aqui como no original
                        checkClienteOnline(); 
                        Thread.Sleep(1000); // 1 second para próxima verificação
                    }
                    catch (Exception e)
                    {
                        // No original: if (STDA_SOURCE_ERROR_DECODE(e.getCodeError()) != STDA_ERROR_TYPE::THREAD)
                        _smp.message_pool.getInstance().push(new message("Erro no loop monitor: " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message(e.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return 0;
        }
         

        protected override void dispach_packet_same_thread(Session _session, packet _packet)
        {
            try
            {
                var func = packet_func_base.funcs.getPacketCall(_packet.getTipo());

                if (func != null)
                {
                    ParamDispatch _pd = new ParamDispatch { _session = _session, _packet = _packet };
                    if (func.ExecCmd(_pd) != 0)
                    {
                        _smp.message_pool.getInstance().push(new message("[client::dispach_packet_same_thread][Error] ID: " + _pd._packet.getTipo() + "(0x" + _pd._packet.getTipo().ToString("X") + ")", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[client::dispach_packet_same_thread][Error] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void dispach_packet_sv_same_thread(Session _session, packet _packet)
        {
            try
            {
                var func = packet_func_base.funcs_sv.getPacketCall(_packet.getTipo());

                if (func != null)
                {
                    ParamDispatch _pd = new ParamDispatch { _session = _session, _packet = _packet };
                    if (func.ExecCmd(_pd) != 0)
                    {
                        _smp.message_pool.getInstance().push(new message("Erro ao tratar o pacote. ID: " + _pd._packet.getTipo() + "(0x" + _pd._packet.getTipo().ToString("X") + "). threadpool::dispach_packet_sv_same_thread()", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message(e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void waitAllThreadFinish(uint dwMilleseconds)
        {
            m_continue_monitor = 0;
            m_continue_send_msg = 0;


            // Cancela as tasks via CancellationToken
            m_cts.Cancel();

            if (m_thread_monitor != null)
            {
                // Simula o waitThreadFinish do original
                m_thread_monitor.Wait((int)dwMilleseconds);
            } 
        }

        public virtual void start()
        {
            if (m_state != ServerState.Failure)
            {
                try
                {
                    commandScan();

                    waitAllThreadFinish(uint.MaxValue); // INFINITE
                }
                catch (Exception e)
                {
                    _smp.message_pool.getInstance().push(new message(e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
            else
            {
                _smp.message_pool.getInstance().push(new message("Cliente inicializado com falha.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                waitAllThreadFinish(uint.MaxValue);
            }
        }

        // Métodos auxiliares que devem ser implementados ou referenciados
        public abstract void checkClienteOnline();
        public abstract void commandScan(); 
    }
}
