using System;
using System.IO;

namespace RoBin
{
    class Program
    {
        static void Main(string[] args)
        {
            //            RoBinFormater formater = new RoBinFormater();
            //            string formattedCode = formater.Format(@"


            //@function DeleteFile{$filePath},
            //{

            //    $deleteResult = @WINAPI {
            //        dll: ""Kernel32.dll"",
            //        ""DeleteFileW"",
            //         $filePath ,

            //        [System.Boolean]  // Returns true if the file is deleted successfully
            //    };

            //    @if {$deleteResult == True},
            //    {
            //        @printf ""File deleted successfully: "" + $filePath;
            //    };

            //    @if {$deleteResult == False},
            //    {
            //        @printf {Failed to delete file: $filePath};
            //    };
            //};

            //$files = [C:\Users\Demon\source\repos\RRRR\RRRR\Program.cs, C:\Users\Demon\source\repos\RRRR\RRRR\Program.cs];
            //@foreach {$file in $files},
            //{
            //    @DeleteFile($file);
            //};
            //");

           
            if (args.Length == 2 && args[0] == "-r")
            {
                string fileName = args[1];

                if (File.Exists(fileName))
                {
                    string fullCode = File.ReadAllText(fileName);
                    RoBinFormater formater = new RoBinFormater();
                    string formattedCode = formater.Format(fullCode);

                    RoBin.BoBin<Program> rbin = new();
                    try
                    {
                        rbin.Execute(formattedCode);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(e.ToString());
                        Console.ResetColor();
                        Console.WriteLine();
                        Console.ReadLine();
                    }
                }
                else
                {
                    Console.WriteLine($"File {fileName} does not exist.");
                }
            }
            else
            {
                Console.WriteLine("Usage: RoBin -c <fileName>");
            }
        }
    }
}
