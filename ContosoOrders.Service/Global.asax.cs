using System;
using System.Web;

namespace Contoso.OrderManagement
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            OrderRepository.Instance.Seed();
        }
    }
}
