using System.IO;
using System.Reflection;
using log4net;
using log4net.Config;
using MailPullerApp.Configuration;

namespace MailPullerApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to my PullerApp");
            Console.WriteLine("Press any key to continue");
            try
            {
                var config = ConfigLoader.Load();
                ConfigLog.InitializeLog4Net(config);

                var log = ConfigLog.GetLogger<Program>();
                log.Info("Log4net je připraveno.");
                

                Console.WriteLine("Configuration loaded successfully.");
                Console.WriteLine($"Email: {config.Logging.Log4NetConfigFile}");
            }
            catch (Exception e)
            {

                Console.WriteLine($"An error occurred while loading the configuration: {e.Message}");
            }
            Console.ReadKey();
        }
    }
}
