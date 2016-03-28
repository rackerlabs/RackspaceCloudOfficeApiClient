using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Rackspace.CloudOffice
{
    internal class Throttler
    {
        public int ThreshholdCount { get; set; }
        public TimeSpan WindowSize { get; set; }

        DateTime _windowEnd = DateTime.MinValue;
        int _count;

        public async Task Throttle()
        {
            var delay = GetDelay();
            while (delay > TimeSpan.Zero)
            {
                Trace.WriteLine("Throttling");
                await Task.Delay(delay).ConfigureAwait(false);
                delay = GetDelay();
            }
        }

        TimeSpan GetDelay()
        {
            lock (this)
            {
                if (DateTime.UtcNow > _windowEnd)
                {
                    _windowEnd = DateTime.UtcNow + WindowSize;
                    _count = 0;
                }

                _count++;
                return _count <= ThreshholdCount
                    ? TimeSpan.Zero
                    : _windowEnd - DateTime.UtcNow;
            }
        }
    }
}
