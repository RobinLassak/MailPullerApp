using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailPullerApp.Models
{
    public class GraphSettings
    {
        // Kazda vlastnost v teto tride reprezentuje jednu vlastnost objektu Graph v appsettings.json
        public string TenantId { get; set; } = default!;
        public string ClientId { get; set; } = default!;
        public string ClientSecret { get; set; } = default!;
        public string AuthorityHost { get; set; } = default!;
        public bool UseDelta { get; set; }
        public string Select { get; set; } = default!;
    }
}
