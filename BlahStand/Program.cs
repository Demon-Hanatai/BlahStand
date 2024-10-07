using OpenQA.Selenium.Chrome;
using System;
using System.Threading;

namespace BlahStand
{
    internal class Program
    {
        public class User
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class text
        {
            public User user { get; set; } = new User();

            public void PrintName(string names, string Name2)
            {
                Console.WriteLine(names);
                Console.WriteLine(Name2);
            }
        }
     
        
        
        static void Main(string[] args)
        {
            Task Task = null!;
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; 
             
            };

            RoBin.BoBin<object> r = new RoBin.BoBin<object>();
            r.AddObject(typeof(Thread));
            r.AddObject(typeof(Console));
           

            while (true)
            {
                try
                {
                    Console.Write($"RB {Environment.CurrentDirectory}>");
                    var input = Console.ReadLine()!;
                    if (input == "!open")
                    {
                        string code = "";
                        Console.Write(">>>");
                        var @in = default(string);
                        while ((@in = Console.ReadLine()) != "!exit")
                        {
                            Console.Write(">>>");
                            code += @in+"\n";
                        }
                        r.Execute(code);
                    }
                    else
                    {
                        r.Execute(input);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e.ToString());
                    Console.ResetColor();
                    Console.WriteLine();
                }
            }
        }
    }
}
