using System;

namespace ConsoleApplication
{
    public static class CommandLineHelpers
    {
        const string forbiddenChars = "\\/:?\"<>|*";
        public static bool isForbiddenCharacter(char c)
        {
            return forbiddenChars.Contains(c);
            //return ( std::string::npos != forbiddenChars.find( c ) );
        }

        string makeSafeFilename(string input, char replacement)
        {

            string result = input.Replace(isForbiddenCharacter, replacement);
            return result;
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }
}
