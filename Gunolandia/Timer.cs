using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Gunolandia
{
    class Timer
    {
        private ulong us_elapsed = 0;
        private int delay;

        public Timer(int delay)
        {
            this.delay = delay;
        }

        public bool IncrementTime(ulong us_increment)
        {
            us_elapsed += us_increment;
            if (us_elapsed / 1e3f >= delay)
            {
                us_elapsed = 0;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            us_elapsed = 0;
        }
    }
}
