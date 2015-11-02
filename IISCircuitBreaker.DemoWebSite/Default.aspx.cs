using System;
using System.Web.UI;

namespace IISCircuitBreaker.DemoWebSite
{
    public partial class _Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            throw new InvalidOperationException("Fail");
        }
    }
}