# IIS Circuit Breaker Module

This is a sample module that is based on the <a href="https://en.wikipedia.org/wiki/Circuit_breaker_design_pattern">circuit breaker pattern</a>. A great C# implementation of this pattern is <a href="https://github.com/michael-wolfenden/Polly">Polly</a> that provides a wealth of options and very fine-grained control.

To see how the IIS module works with default configuration values, start the sample web app and reload it for 5 times (getting an error in all five). Then reload it again and the circuit breaker should have kicked in.

You can change the configuration values of the circuit breaker in the sample web application's web.config.
