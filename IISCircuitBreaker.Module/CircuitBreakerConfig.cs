namespace IISCircuitBreaker.Module
{
    public class CircuitBreakerConfig
    {
        public int ConsecutiveErrorsToBreak { get; set; }
        public int BreakDelayInSeconds { get; set; }
    }
}
