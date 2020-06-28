using System;
using System.Collections.Generic;

namespace MPTranslator
{
    class Program
    {
        static void Main(string[] args)
        {
            SSDT translator = new SSDT(
                new string[] { "(", ")", "i", "-", "+", "*", "/" },
                new string[] { "E", "T", "F" },
                "E"
            );

            // Jesus fuck
            translator.AddRule(
                "E",
                new List<string> { "T" },
                (TransRule placeholder) =>
                    { placeholder.head.attrVal = placeholder.body[0].attrVal; }
            );
            translator.AddRule(
                "E",
                new List<string> { "E", "+", "T" },
                (TransRule placeholder) =>
                    { placeholder.head.attrVal = placeholder.body[0].attrVal + placeholder.body[2].attrVal; }
            );
            translator.AddRule(
                "E",
                new List<string> { "E", "-", "T" },
                (TransRule placeholder) =>
                    { placeholder.head.attrVal = placeholder.body[0].attrVal - placeholder.body[2].attrVal; }
            );
            translator.AddRule(
                "T",
                new List<string> { "F" },
                (TransRule placeholder) =>
                    { placeholder.head.attrVal = placeholder.body[0].attrVal; }
            );
            translator.AddRule(
                "T",
                new List<string> { "T", "*", "F" },
                (TransRule placeholder) =>
                    { placeholder.head.attrVal = placeholder.body[0].attrVal * placeholder.body[2].attrVal; }
            );
            translator.AddRule(
                "T",
                new List<string> { "T", "/", "F" },
                (TransRule placeholder) =>
                    { placeholder.head.attrVal = placeholder.body[0].attrVal / placeholder.body[2].attrVal; }
            );
            translator.AddRule(
                "F",
                new List<string> { "i" },
                (TransRule placeholder) =>
                    { placeholder.head.attrVal = placeholder.body[0].attrVal; }
            );
            translator.AddRule(
                "F",
                new List<string> { "(", "E", ")" },
                (TransRule placeholder) =>
                    { placeholder.head.attrVal = placeholder.body[1].attrVal; }
            );
            translator.AddRule(
                "F",
                new List<string> { "-", "F" },
                (TransRule placeholder) =>
                    { placeholder.head.attrVal = (- placeholder.body[1].attrVal); }
            );

            //translator.AddRule(
            //    "E",
            //    new List<string> { "E", "+", "T" },
            //    (TransRule placeholder) =>
            //        { placeholder.head.attrVal = placeholder.body[0].attrVal + placeholder.body[2].attrVal; }
            //);
            //translator.AddRule(
            //    "E",
            //    new List<string> { "T" },
            //    (TransRule placeholder) =>
            //        { placeholder.head.attrVal = placeholder.body[0].attrVal; }
            //);
            //translator.AddRule(
            //    "T",
            //    new List<string> { "T", "*", "F" },
            //    (TransRule placeholder) =>
            //        { placeholder.head.attrVal = placeholder.body[0].attrVal * placeholder.body[2].attrVal; }
            //);
            //translator.AddRule(
            //    "T",
            //    new List<string> { "F" },
            //    (TransRule placeholder) =>
            //        { placeholder.head.attrVal = placeholder.body[0].attrVal; }
            //);
            //translator.AddRule(
            //    "F",
            //    new List<string> { "(", "E", ")" },
            //    (TransRule placeholder) =>
            //        { placeholder.head.attrVal = placeholder.body[1].attrVal; }
            //);
            //translator.AddRule(
            //    "F",
            //    new List<string> { "i" },
            //    (TransRule placeholder) =>
            //        { placeholder.head.attrVal = placeholder.body[0].attrVal; }
            //);

                // Plus test
            System.Console.WriteLine(" 8 ? {0}",  8 == translator.Execute("5 + 3"));
            System.Console.WriteLine(" 5 ? {0}",  5 == translator.Execute("5 + 0"));
                // Minus test
            System.Console.WriteLine(" 1 ? {0}",  1 == translator.Execute("3 - 2"));
            System.Console.WriteLine(" 0 ? {0}",  0 == translator.Execute("1 - 1"));
                // Multiplication test
            System.Console.WriteLine("10 ? {0}", 10 == translator.Execute("5 * 2"));
            System.Console.WriteLine(" 5 ? {0}",  5 == translator.Execute("5 * 1"));
            System.Console.WriteLine(" 0 ? {0}",  0 == translator.Execute("5 * 0"));
                // Division test
            System.Console.WriteLine(" 2 ? {0}",  2 == translator.Execute("4 / 2"));
            System.Console.WriteLine(" 4 ? {0}",  4 == translator.Execute("4 / 1"));
                // Unary minus tests
            System.Console.WriteLine("-1 ? {0}", -1 == translator.Execute("- 1"));
            System.Console.WriteLine("-2 ? {0}", -2 == translator.Execute("- 1 * 2"));
                // Complex behaviour test
            System.Console.WriteLine(" 7 ? {0}",  7 == translator.Execute("5 + 1 * 2"));
            System.Console.WriteLine(" 8 ? {0}",  8 == translator.Execute("( 5 - 1 ) * 2"));
            System.Console.WriteLine("12 ? {0}", 12 == translator.Execute("( 5 - 3 ) * 2 * 3"));
            System.Console.WriteLine(" 2 ? {0}",  2 == translator.Execute("( 5 + ( - 2 ) ) / 3 * 2"));
                // Bigger integer test
            System.Console.WriteLine("32 ? {0}", 32 == translator.Execute("30 + 2"));
            System.Console.WriteLine("28 ? {0}", 28 == translator.Execute("30 - 2"));
            System.Console.WriteLine("60 ? {0}", 60 == translator.Execute("30 * 2"));
            System.Console.WriteLine(" 2 ? {0}",  2 == translator.Execute("30 / 15"));

        }
    }
}
