using System;
using System.Collections.Generic;
namespace PangyaAPI.Utilities
{
    public class PangyaSyncTimerManager
    {
        private readonly List<PangyaSyncTimer> timers = new List<PangyaSyncTimer>();
        private readonly object lockObj = new object();

        public PangyaSyncTimer CreateTimer(uint timeMs, Action onFinish, bool autoRepeat = false)
        {
            return CreateTimer(timeMs, onFinish, null as List<long>, PangyaSyncTimer.TIMER_TYPE.NORMAL, autoRepeat);
        }

        public PangyaSyncTimer CreateTimer(uint timeMs, Action onFinish, List<long> intervals, PangyaSyncTimer.TIMER_TYPE type, bool autoRepeat = false)
        {
            PangyaSyncTimer t = null;
            try
            {
                t = new PangyaSyncTimer(timeMs, intervals, type, onFinish, autoRepeat);
                lock (lockObj) { timers.Add(t); }
            }
            catch (Exception)
            {
                t?.Dispose();
                t = null;
            }
            return t;
        }


        public void DeleteTimer(PangyaSyncTimer t)
        {
            if (t == null) return;
            lock (lockObj)
            {
                var _time = timers.IndexOf(t);

                if (_time > -1)
                {
                    timers.RemoveAt(_time);
                    t.Stop();
                    t.Dispose();
                    t = null;
                }
            }
        }

        public void PauseAll()
        {
            lock (lockObj) { timers.ForEach(t => t.Pause()); }
        }

        public void ResumeAll()
        {
            lock (lockObj) { timers.ForEach(t => t.Resume()); }
        }

        public void StopAll()
        {
            lock (lockObj)
            {
                timers.ForEach(t => t.Stop());
                timers.Clear();
            }
        }

        public bool IsEmpty()
        {
            lock (lockObj) { return timers.Count == 0; }
        }
    }
}