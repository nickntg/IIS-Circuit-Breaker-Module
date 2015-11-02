using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Configuration;

namespace IISCircuitBreaker.Module
{
    public class CircuitBreakerHttpModule : IHttpModule
    {
        private static readonly List<string> FilesWatched;
        private static readonly List<string> StatusCodes;
        private static readonly int StatusCodeWhileBroken;

        private static CircuitBreaker Breaker { get; set; }
 
        static CircuitBreakerHttpModule()
        {
            FilesWatched = ConfigurationManager.AppSettings["IISCircuitBreaker.FilesWatched"].Split(new[] {','}).ToList();
            StatusCodes = ConfigurationManager.AppSettings["IISCircuitBreaker.StatusCodes"].Split(new[] { ',' }).ToList();
            StatusCodeWhileBroken = Convert.ToInt32(ConfigurationManager.AppSettings["IISCircuitBreaker.StatusCodeWhileBroken"]);

            Breaker = new CircuitBreaker(new CircuitBreakerConfig
                {
                    BreakDelayInSeconds = Convert.ToInt32(ConfigurationManager.AppSettings["IISCircuitBreaker.BreakDelayInSeconds"]),
                    ConsecutiveErrorsToBreak = Convert.ToInt32(ConfigurationManager.AppSettings["IISCircuitBreaker.ConsecutiveErrorsToBreak"])
                });
        }

        public void Dispose()
        {
        }

        public void Init(HttpApplication context)
        {
            context.BeginRequest += ApplicationBeginRequest;
            context.EndRequest += ApplicationEndRequest;
        }

        private void ApplicationBeginRequest(Object source, EventArgs e)
        {
            var application = (HttpApplication)source;

            // If we don't care about the file, do nothing.
            if (IsFileWeWatch(application.Request.CurrentExecutionFilePathExtension))
            {
                return;
            }

            // If the circuit is closed, we've nothing to do.
            if (!Breaker.IsCircuitOpen())
            {
                return;
            }

            // Circuit is open. Set error and return.
            var context = application.Context;
            context.Response.StatusCode = StatusCodeWhileBroken;
            context.Response.Write(string.Format(
                    "<h1>Circuit is open</h1>This means that there were {0} consecutive errors. The site now returns {1} and will continue to do so until {2}, then you can try again.",
                    Breaker.Config.ConsecutiveErrorsToBreak, StatusCodeWhileBroken, Breaker.OpenUntil.ToString("yyyy/MM/dd HH:mm:ss")));
        }

        private void ApplicationEndRequest(Object source, EventArgs e)
        {
            var application = (HttpApplication)source;

            // If we don't care about the file, do nothing.
            if (IsFileWeWatch(application.Request.CurrentExecutionFilePathExtension))
            {
                return;
            }

            /* If the circuit is open, we don't need to do something.
             * We expect that the circuit will open at beginRequest, however
             this might mean that some longer-lived request might be served
             correctly despite the fact that the circuit is open. */
            if (Breaker.IsCircuitOpen())
            {
                return;
            }

            var context = application.Context;

            // Is there an error we care about?
            if (context.Error == null || !StatusCodes.Contains(context.Response.StatusCode.ToString(CultureInfo.InvariantCulture)))
            {
                // No errors that we care about, reset our count and process.
                Breaker.ClearErrors();
                return;
            }

            Breaker.AddError();
        }

        private bool IsFileWeWatch(string extension)
        {
            return !FilesWatched.Contains(extension);
        }
    }
}