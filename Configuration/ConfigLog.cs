using log4net;
using log4net.Config;
using MailPullerApp.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MailPullerApp.Configuration
{
    internal class ConfigLog
    {
        public static bool IsCongigured { get; private set; } = false;

        //Metoda ktera inicializuje log4net z cesty uvedene v appsettings.json
        public static void InitializeLog4Net(AppConfig config)
        {
            if(IsCongigured)
            {
                return; // Log4net je nakonfirugovan
            }
            var path = config.Logging.Log4NetConfigFile ?? "log4net.config"; // Pokud neni cesta nastavena, pouzije se defaultni hodnota
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(AppContext.BaseDirectory, path); // Pokud neni cesta absolutni, prida se cesta k aplikaci
            }
            // V korenovem adresari vytvorime slozku pro logy, pokud neexistuje
            Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "logs"));

            var repo = LogManager.GetRepository(Assembly.GetEntryAssembly()!);
            XmlConfigurator.Configure(repo, new FileInfo(path));

            IsCongigured = true; // Nastavime, ze log4net je nakonfigurovan
        }

        // Pomocnik k ziskani loggeru pro konkretni typ nebo jmeno
        public static ILog GetLogger<T>() => LogManager.GetLogger(typeof(T));
        public static ILog GetLogger(string name) => LogManager.GetLogger(name);
    }
}
