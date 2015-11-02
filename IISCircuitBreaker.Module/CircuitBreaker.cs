using System;

namespace IISCircuitBreaker.Module
{
    public class CircuitBreaker : ICircuitBreaker
    {
        public CircuitBreakerConfig Config { get; private set; }

        public DateTime OpenUntil { get; private set; }

        public int CurrentNumberOfErrors { get; private set; }

        private bool _isOpen;

        public CircuitBreaker(CircuitBreakerConfig config)
        {
            Config = config;
            Reset();
        }

        public void AddError()
        {
            lock (typeof(CircuitBreaker))
            {
                CurrentNumberOfErrors++;

                if (CurrentNumberOfErrors >= Config.ConsecutiveErrorsToBreak)
                {
                    OpenUntil = DateTime.Now.AddSeconds(Config.BreakDelayInSeconds);
                    _isOpen = true;
                }
            }   
        }

        public void ClearErrors()
        {
            lock (typeof (CircuitBreaker))
            {
                CurrentNumberOfErrors = 0;
            }
        }

        public bool IsCircuitOpen()
        {
            if (!_isOpen)
            {
                return false;
            }

            if (DateTime.Now.CompareTo(OpenUntil) > 0)
            {
                Reset();
                return false;
            }

            return true;
        }

        private void Reset()
        {
            lock (typeof(CircuitBreaker))
            {
                CurrentNumberOfErrors = 0;
                _isOpen = false;
                OpenUntil = DateTime.Now;
            }
        }
    }
}