using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IISCircuitBreaker.Module.Tests.Unit
{
    [TestClass]
    public class CircuitBreakerTests
    {
        [TestMethod]
        public void TestCorrectSetup()
        {
            var breaker = new CircuitBreaker(new CircuitBreakerConfig {ConsecutiveErrorsToBreak = 100, BreakDelayInSeconds = 5});

            Assert.IsNotNull(breaker.Config);
            Assert.AreEqual(100, breaker.Config.ConsecutiveErrorsToBreak);
            Assert.AreEqual(5, breaker.Config.BreakDelayInSeconds);

            Assert.AreEqual(0, breaker.CurrentNumberOfErrors);
            Assert.IsTrue(DateTime.Now.CompareTo(breaker.OpenUntil) >= 0);

            Assert.IsFalse(breaker.IsCircuitOpen());
        }

        [TestMethod]
        public void TestThresholdReach()
        {
            var breaker = new CircuitBreaker(new CircuitBreakerConfig { ConsecutiveErrorsToBreak = 2, BreakDelayInSeconds = 5 });

            Assert.IsFalse(breaker.IsCircuitOpen());
            breaker.AddError();
            Assert.IsFalse(breaker.IsCircuitOpen());
            breaker.AddError();
            Assert.IsTrue(breaker.IsCircuitOpen());
        }

        [TestMethod]
        public void TestThresholdNotReachedWhenErrorsAreCleared()
        {
            var breaker = new CircuitBreaker(new CircuitBreakerConfig { ConsecutiveErrorsToBreak = 2, BreakDelayInSeconds = 5 });

            Assert.IsFalse(breaker.IsCircuitOpen());
            breaker.AddError();
            Assert.IsFalse(breaker.IsCircuitOpen());

            breaker.ClearErrors();
            Assert.IsFalse(breaker.IsCircuitOpen());

            breaker.AddError();
            Assert.IsFalse(breaker.IsCircuitOpen());
        }

        [TestMethod]
        public void TestWaitForOneSecondUntilCircuitCloses()
        {
            var breaker = new CircuitBreaker(new CircuitBreakerConfig { ConsecutiveErrorsToBreak = 2, BreakDelayInSeconds = 1 });

            Assert.IsFalse(breaker.IsCircuitOpen());
            breaker.AddError();
            Assert.IsFalse(breaker.IsCircuitOpen());
            breaker.AddError();
            Assert.IsTrue(breaker.IsCircuitOpen());

            Thread.Sleep(250);
            Assert.IsTrue(breaker.IsCircuitOpen());

            Thread.Sleep(250);
            Assert.IsTrue(breaker.IsCircuitOpen());

            Thread.Sleep(500);
            Assert.IsFalse(breaker.IsCircuitOpen());
        }
    }
}