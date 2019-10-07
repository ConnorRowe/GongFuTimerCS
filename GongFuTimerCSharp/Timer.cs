using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GongFuTimerCSharp
{
    public class Timer
    {
        public DateTime    startTime;
        public DateTime    endTime;
        public bool        isRunning = false;

        public Timer()
        {

        }

        public void Start()
        {
            startTime = DateTime.Now;
            isRunning = true;
        }

        public void Stop()
        {
            endTime = DateTime.Now;
            isRunning = false;
        }

        public double ElapsedMiliseconds()
        {
            DateTime currentTime;

            if(isRunning)
            {
                currentTime = DateTime.Now;
            }
            else
            {
                currentTime = endTime;
            }

            TimeSpan diff = currentTime - startTime;

            return diff.TotalMilliseconds;
        }

        public double ElapsedSeconds()
        {
            return ElapsedMiliseconds() / 1000.0;
        }

        public void Clear()
        {
            Start();
            Stop();
        }

        public void Restart()
        {
            Stop();
            Start();
        }

    }
}
