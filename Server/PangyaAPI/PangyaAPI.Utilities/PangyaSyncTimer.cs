using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using PangyaAPI.Utilities.Log;
namespace PangyaAPI.Utilities
{
    /// <summary>
    /// Create By LuisMK
    /// </summary>
    public class PangyaSyncTimer
    {
        public enum TIMER_STATE
        {
            NONE,
            INIT,
            STANDBY,
            RUNNING,
            PAUSE,
            PAUSING,
            PAUSED,
            STOP,
            STOPPING,
            STOPPED,
            FINISH
        }

        public enum TIMER_TYPE
        {
            NORMAL,
            PERIODIC,
            PERIODIC_INFINITE
        }

        private List<long> tableInterval;
        private int currentIntervalIndex;
        private Timer timer;
        private Stopwatch stopwatch;
        private TimeSpan acumulado;
        private bool pausado;
        private bool autoRepeat;
        private bool isDisposed;
        public System.DateTime m_timer;
        private uint timeFix;
        private TIMER_TYPE tipo;
        private TIMER_STATE state;
        /// <summary>
        /// obter tipo time
        /// </summary>
        /// <returns></returns>
        public TIMER_STATE getState() => state;
        private Action onTimeFinish;
        public PangyaSyncTimer(
   uint timeMs,
   TIMER_TYPE tipo,
   Action onTimeFinish,
   bool autoRepeat = false
)
   : this(timeMs, null, tipo, onTimeFinish, autoRepeat) { }

        // E esse aqui é o principal, que os dois construtores usam:
        public PangyaSyncTimer(
            uint time,
            List<long> tableInterval,
            TIMER_TYPE tipo,
            Action onTimeFinish,
            bool autoRepeat = false
        )
        {
            this.timeFix = time;
            this.tableInterval = tableInterval ?? new List<long>();
            this.tipo = tipo;
            this.autoRepeat = autoRepeat;
            this.onTimeFinish = onTimeFinish;

            stopwatch = new Stopwatch();
            ResetInternals();

            state = TIMER_STATE.INIT;

            Start();
        }

        private void ResetInternals()
        {
            pausado = false;
            currentIntervalIndex = 0;
            stopwatch.Reset();
        }

        public void Start()
        {
            ResetInternals();

            stopwatch.Start();
            pausado = false;
            state = TIMER_STATE.STANDBY;
            timer?.Dispose();
            m_timer = DateTime.Now;
            timer = new Timer(_ => TickTime(), null, 0, tipo != TIMER_TYPE.NORMAL ? 50 : 1000); // checagem a cada 50ms
        }

        public void Pause()
        {
            if (!pausado && stopwatch.IsRunning)
            {
                stopwatch.Stop(); 
                pausado = true;
                state = TIMER_STATE.PAUSED; 
                timer?.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        public void Resume()
        {
            if (pausado)
            { 
                stopwatch.Start();
                pausado = false;
                state = TIMER_STATE.RUNNING; 
                // Reinicia o Tick do timer
                timer?.Change(0, tipo != TIMER_TYPE.NORMAL ? 50 : 1000);
            }
        }

        public void Stop(TIMER_STATE forcedState = TIMER_STATE.STOP)
        {
            stopwatch.Stop();

            pausado = false;
            state = forcedState;
            Dispose();
        }

        public void Reset(List<long> newTableInterval = null)
        {
            Stop();
            if (newTableInterval != null)
            {
                tableInterval = newTableInterval;
                currentIntervalIndex = 0;
            }
            state = TIMER_STATE.INIT;
        }

        public long getElapsed()
        {
            var elapsed = acumulado;
            if (!pausado && stopwatch.IsRunning)
                elapsed += stopwatch.Elapsed;

            long result = (long)elapsed.TotalMilliseconds; // aplica fator aqui 

            return result;
        }

        public long getTimeElapsed()
        {
            return (long)Math.Round((DateTime.Now - this.m_timer).TotalMilliseconds / 1000.0f);/*Mili para segundos*/
        }


        public long getRemainingMilliseconds()
        {
            long elapsed = getElapsed();
            long timeTarget = GetCurrentTargetTime();
            return timeTarget > elapsed ? timeTarget - elapsed : 0;
        }

        public long getRemaining()
        {
            long elapsed = getElapsed();
            long timeTarget = GetCurrentTargetTime();
            return getRemainingMilliseconds() == 0 ? elapsed > timeTarget ? elapsed - timeTarget : 0 : 0;
        }

        private long GetCurrentTargetTime()
        {
            if (tipo == TIMER_TYPE.NORMAL)
                return timeFix;

            if (tipo == TIMER_TYPE.PERIODIC || tipo == TIMER_TYPE.PERIODIC_INFINITE)
            {
                if (currentIntervalIndex < tableInterval.Count)
                    return tableInterval[currentIntervalIndex];
            }

            return 0;
        }


        public string getTimeLog()
        {

            if (timer == null)
                return ""; 

                if (state == TIMER_STATE.STOP || state == TIMER_STATE.FINISH && isDisposed)
                    return ""; 
                long remaining = getRemainingMilliseconds();
                long totalSeconds = remaining / 1000;

                long minutes = remaining / 60000;
                long seconds = (remaining / 1000) % 60;
                long milliseconds = remaining % 1000;


                string minPart = minutes != 0 ? minutes.ToString("D2") : "";
                string timeLog = $"{(minPart != "" ? minPart + ":" : "")}{seconds:D2}:{milliseconds:D3}";
            return timeLog;
            }
        private void TickTime()
        {
            if (timer == null)
                return;

            try
            {
                if (state == TIMER_STATE.STOP || state == TIMER_STATE.FINISH && isDisposed)
                    return;

                state = TIMER_STATE.RUNNING;
                long remaining = getRemainingMilliseconds();
                long totalSeconds = remaining / 1000;

                long minutes = remaining / 60000;
                long seconds = (remaining / 1000) % 60;
                long milliseconds = remaining % 1000;


                string minPart = minutes != 0 ? minutes.ToString("D2") : "";
                string timeLog = $"{(minPart != "" ? minPart + ":" : "")}{seconds:D2}:{milliseconds:D3}";

               // _smp.message_pool.getInstance().push(new message($"[PangyaSyncTimer::TickTime][Log] Time: {timeLog}", type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (remaining == 0)
                {
                    Stop(TIMER_STATE.FINISH);
                    onTimeFinish?.Invoke();

                    if (tipo == TIMER_TYPE.PERIODIC || tipo == TIMER_TYPE.PERIODIC_INFINITE)
                    {
                        currentIntervalIndex++;

                        if (currentIntervalIndex < tableInterval.Count)
                        {
                            Start(); // próximo
                        }
                        else if (autoRepeat)
                        {
                            currentIntervalIndex = 0;
                            Start();
                        }
                    }
                    else if (autoRepeat)
                    {
                        Start();
                    }
                }

            }
            catch (exception ex)
            {
                _smp.message_pool.getInstance().push(new message(
                                        $"[PangyaSyncTimer][Error] -> " + ex.getFullMessageError(),
                                        type_msg.CL_ONLY_CONSOLE_DEBUG));
            }
        }



        public void Dispose(bool reset = true)
        {
            timer?.Dispose();
            timer = null;

            if (reset)
                ResetInternals();

            isDisposed = true;
        }
    }
}