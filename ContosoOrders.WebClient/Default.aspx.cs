using System;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Contoso.OrderClient;

namespace Contoso.OrderManagement.WebClient
{
    public class DefaultPage : Page
    {
        protected HtmlForm form1;
        protected Label lblResult;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack)
            {
                return;
            }

            var proxy = new OrderManagementService
            {
                Url = ConfigurationManager.AppSettings["OrderServiceUrl"],
                AuthenticationHeaderValue = new AuthenticationHeader
                {
                    ApiToken = "DEMO-TOKEN-12345",
                    ClientId = "ContosoOrders.WebClient"
                }
            };

            var order = proxy.GetOrderById(1);
            var summaries = proxy.SearchOrders(null);

            var builder = new StringBuilder();
            builder.Append("<strong>Order 1:</strong> ")
                .Append(Server.HtmlEncode(order.Customer.CompanyName))
                .Append(" - ")
                .Append(Server.HtmlEncode(order.Status.ToString()))
                .Append("<br/>");
            builder.Append("<strong>Total Orders:</strong> ")
                .Append(summaries.Length)
                .Append("<br/>");
            builder.Append("<strong>Customers:</strong> ")
                .Append(Server.HtmlEncode(string.Join(", ", summaries.Select(s => s.CustomerName).Distinct().ToArray())));

            lblResult.Text = builder.ToString();
        }
    }
}
