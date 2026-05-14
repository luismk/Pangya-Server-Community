using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Threading;
using static PangyaAPI.Utilities.Tools;

public class PangyaThread : IDisposable
{
    private Thread m_thread;
    private ThreadRoutine m_routine;
    private object m_parameter;

    // Evento para pausar/retomar a execução
    private readonly ManualResetEventSlim m_pauseEvent;
    // Flag de controle de execução
    private volatile bool m_running;
    private readonly uint m_tipo;

    public PangyaThread(uint tipo)
    {
        m_tipo = tipo;
        m_pauseEvent = new ManualResetEventSlim(true); // Começa liberado (true)
        m_running = false;
    }

    public PangyaThread(
        uint tipo,
        ThreadRoutine routine,
        object parameter,
        ThreadPriority priority = ThreadPriority.Normal
    ) : this(tipo)
    {
        init_thread(routine, parameter, priority);
    }

    /// <summary>
    /// Inicia a execução da Thread
    /// </summary>
    public void init_thread(
        ThreadRoutine routine,
        object parameter,
        ThreadPriority priority = ThreadPriority.Normal
    )
    {
        if (isLive())
            throw new exception($"[Thread {m_tipo}] Já está iniciada.");

        m_routine = routine ?? throw new exception("Routine is null");
        m_parameter = parameter;
        m_running = true;

        m_thread = new Thread(thread_entry)
        {
            IsBackground = true, // Permite que o processo feche mesmo com a thread rodando
            Priority = priority,
            Name = $"PangyaThread_Type_{m_tipo}"
        };

        m_thread.Start();
    }

    private void thread_entry()
    {
        try
        {
            // Loop principal: Mantém a thread viva enquanto m_running for true
            // Útil para rotinas que processam filas (Queue)
            while (m_running)
            {
                // Verifica se a thread foi pausada
                m_pauseEvent.Wait();

                if (!m_running) break;

                // Executa a lógica do Pangya
                m_routine?.Invoke(m_parameter);

                // Se a sua rotina NÃO for um loop infinito interno, 
                // você deve colocar um break aqui ou m_running = false 
                // para que a thread termine após uma execução.
                // No Pangya, geralmente threads de banco/log rodam uma vez e terminam,
                // ou rodam em loop processando pacotes.
                if (IsSingleExecution(m_tipo))
                    break;
            }
        }
        catch (ThreadInterruptedException)
        {
            // Disparado pelo Interrupt() quando o servidor está fechando
            // Log amigável apenas para debug
        }
        catch (Exception e)
        {
            _smp.message_pool.getInstance().push(
                new message($"[PangyaThread][Error Type {m_tipo}] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE)
            );
        }
        finally
        {
            m_running = false;
            // Opcional: Notificar o gerenciador que a thread morreu
        }
    }

    // Método auxiliar para decidir se a thread deve re-executar
    private bool IsSingleExecution(uint tipo)
    {
        // Adicione aqui os tipos de thread que rodam apenas 1 vez
        return tipo != 100; // Exemplo: se tipo 100 for um Worker de Fila, ele não para.
    }

    public void pause_thread()
    {
        if (m_thread != null)
            m_pauseEvent.Reset();
    }

    public void resume_thread()
    {
        if (m_thread != null)
            m_pauseEvent.Set();
    }

    public void exit_thread()
    {
        if (m_thread == null)
            return;

        m_running = false;
        m_pauseEvent.Set(); // Libera o Wait() para a thread poder morrer

        try
        {
            // Tenta finalizar graciosamente
            if (!m_thread.Join(1000))
            {
                m_thread.Interrupt(); // Força a saída de sleeps/waits
            }
        }
        catch { }
        finally
        {
            m_thread = null;
        }
    }

    public void waitThreadFinish(int milliseconds)
    {
        if (m_thread != null && !m_thread.Join(milliseconds))
            throw new exception($"[Thread {m_tipo}] Tempo de espera excedido para finalização.");
    }

    public bool isLive()
    {
        return m_thread != null && m_thread.IsAlive;
    }

    public uint getTipo() => m_tipo;

    public void Dispose()
    {
        exit_thread();
        m_pauseEvent.Dispose();
        GC.SuppressFinalize(this);
    }

    ~PangyaThread()
    {
        Dispose();
    }
}