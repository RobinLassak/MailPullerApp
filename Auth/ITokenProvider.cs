using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailPullerApp.Auth
{
    //Rozhraní pro poskytování tokenů pro aplikaci
    public interface ITokenProvider
    {
        Task<string> GetAppTokenAsync(string[] scopes, CancellationToken cancellationToken = default);
    }
}
