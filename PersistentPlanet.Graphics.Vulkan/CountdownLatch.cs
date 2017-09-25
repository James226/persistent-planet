using System;
using System.Threading;

namespace PersistentPlanet.Graphics.Vulkan
{
    public class CountdownLatch
    {
        private readonly ManualResetEvent _waitEvent = new ManualResetEvent(true);
        private int _count;

        public void Increment()
        {
            var count = Interlocked.Increment(ref _count);
            if (count == 1)
            {
                _waitEvent.Reset();
            }
        }

        public void Decrement()
        {
            var count = Interlocked.Decrement(ref _count);
            if (count == 0)
            {
                _waitEvent.Set();
            }
            else if (count < 0)
            {
                throw new InvalidOperationException("Count must be greater than or equal to 0");
            }
        }

        public void WaitUntilZero()
        {
            _waitEvent.WaitOne();
        }
    }
}