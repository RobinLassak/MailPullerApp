using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailPullerApp.Services.Graph
{
    // Rozhraní, které definuje, jak z Microsoft Graphu získat seznam e-mailů a stáhnout MIME obsah konkrétní zprávy, aby zbytek appky neřešil HTTP detaily.
    internal interface IGraphMailClient
    {
    }
}
