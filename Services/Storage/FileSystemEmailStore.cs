using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailPullerApp.Services.Storage
{
    // Hlavni třída pro ukládání e-mailů do souborového systému, implementuje IEmailStore, využívá FilenameSanitizer pro sanitaci názvů souborů,
    // a vyuziva objekt output v appsettings.json aby se urcilo, kam se maji emaily ukládat, zda ukladat cely email a zda ukladat prilohy.
    internal class FileSystemEmailStore
    {
    }
}
