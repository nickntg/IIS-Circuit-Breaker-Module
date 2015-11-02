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
        private static readonly int ConsecutiveErrorsToBreak;
        private static readonly int BreakDelayInSeconds;
        private static readonly int StatusCodeWhileBroken;

        private static int CurrentErrors { get; set; }

        private static bool IsBreakerActive { get; set; }

        private static DateTime BreakerActiveUntil { get; set; }
 
        static CircuitBreakerHttpModule()
        {
            FilesWatched = ConfigurationManager.AppSettings["IISCircuitBreaker.FilesWatched"].Split(new[] {','}).ToList();
            StatusCodes = ConfigurationManager.AppSettings["IISCircuitBreaker.StatusCodes"].Split(new[] { ',' }).ToList();
            ConsecutiveErrorsToBreak = Convert.ToInt32(ConfigurationManager.AppSettings["IISCircuitBreaker.ConsecutiveErrorsToBreak"]);
            BreakDelayInSeconds = Convert.ToInt32(ConfigurationManager.AppSettings["IISCircuitBreaker.BreakDelayInSeconds"]);
            StatusCodeWhileBroken = Convert.ToInt32(ConfigurationManager.AppSettings["IISCircuitBreaker.StatusCodeWhileBroken"]);
            CurrentErrors = 0;
            IsBreakerActive = false;
            BreakerActiveUntil = DateTime.Now;
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
            // If the circuit is closed, we've nothing to do.
            if (!IsBreakerActive)
            {
                return;
            }

            // Circuit is open. Until when?
            if (DateTime.Now.CompareTo(BreakerActiveUntil) > 0)
            {
                // Time to close the circuit again.
                lock (typeof (CircuitBreakerHttpModule))
                {
                    CurrentErrors = 0;
                    IsBreakerActive = false;
                    return;
                }
            }

            // Circuit is open. Set error and return.
            var application = (HttpApplication)source;
            var context = application.Context;
            context.Response.StatusCode = StatusCodeWhileBroken;
            context.Response.Write(string.Format(
                    "<h1>Circuit is open</h1>This means that there were {0} consecutive errors. The site now returns {1} and will continue to do so until {2}, then you can try again.",
                    ConsecutiveErrorsToBreak, StatusCodeWhileBroken, BreakerActiveUntil.ToString("yyyy/MM/dd HH:mm:ss")));
        }

        private void ApplicationEndRequest(Object source, EventArgs e)
        {
            /* If the circuit is open, we don't need to do something.
             * We expect that the circuit will open at beginRequest, however
             this might mean that some longer-lived request might be served
             correctly despite the fact that the circuit is open. */
            if (IsBreakerActive)
            {
                return;
            }

            var application = (HttpApplication)source;

            /* Is it a file we care about? */
            if (!FilesWatched.Contains(application.Request.CurrentExecutionFilePathExtension))
            {
                /* If not, process and don't count anything */
                return;
            }

            var context = application.Context;

            /* Is there an error we care about? */
            if (context.Error == null || !StatusCodes.Contains(context.Response.StatusCode.ToString(CultureInfo.InvariantCulture)))
            {
                /* No errors that we care about, reset our count and process */
                lock (typeof(CircuitBreakerHttpModule))
                {
                    CurrentErrors = 0;
                }
                return;
            }

            lock (typeof (CircuitBreakerHttpModule))
            {
                /* Add error and check whether we need to break the circuit */
                CurrentErrors++;
                if (CurrentErrors >= ConsecutiveErrorsToBreak)
                {
                    BreakerActiveUntil = DateTime.Now.AddSeconds(BreakDelayInSeconds);
                    IsBreakerActive = true;
                }
            }
        }
    }
}