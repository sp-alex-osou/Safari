using System;

namespace Safari
{
    static class Program
    {
        static void Main(string[] args)
        {
            using (Safari game = new Safari())
            {
                game.Run();
            }
        }
    }
}

