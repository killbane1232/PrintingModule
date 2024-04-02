namespace PrintingModule
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting");
            var bot = TelegramBot.getInstance();
            
            while (true)
            {
                Thread.Sleep(10000);
            }
        }
    }
}
