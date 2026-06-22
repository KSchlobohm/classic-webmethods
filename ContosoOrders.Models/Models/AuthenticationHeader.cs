using System.Web.Services.Protocols;

namespace Contoso.OrderManagement.Models
{
    public class AuthenticationHeader : SoapHeader
    {
        public string ApiToken { get; set; }
        public string ClientId { get; set; }
    }
}
