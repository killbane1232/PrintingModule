namespace PrintingModule
{
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("Starting");
            TelegramBot.GetInstance();
            
            while (true)
            {
                Thread.Sleep(10000);
            }
        }
    }
}
