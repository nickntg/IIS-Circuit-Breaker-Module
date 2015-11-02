using System;

namespace IISCircuitBreaker.Module
{
    public interface ICircuitBreaker
    {
        CircuitBreakerConfig Config { get; }
        DateTime OpenUntil { get; }
        void AddError();
        void ClearErrors();
        bool IsCircuitOpen();
    }
}