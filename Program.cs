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
                var config = Configuration.ConfigLoader.Load();
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
