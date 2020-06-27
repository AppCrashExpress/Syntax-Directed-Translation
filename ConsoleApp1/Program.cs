using System;
using System.Collections.Generic;

namespace MPTranslator
{
    class Program
    {
        static void Main(string[] args)
        {
            SSDT translator = new SSDT(
                new string[] { "i", "(", ")", "-", "+", "*", "/" },
                new string[] { "E", "T", "F" },
                "E"
            );

                // Jesus fuck
            //translator.AddRule(
            //    "E",
            //    new List<string> { "T" },
            //    (TransRule placeholder) =>
            //        { placeholder.head.attrVal = placeholder.body[0].attrVal; }
            //);
            //translator.AddRule(
            //    "E",
            //    new List<string> { "E", "+", "T" },
            //    (TransRule placeholder) =>
            //        { placeholder.head.attrVal = placeholder.body[0].attrVal + placeholder.body[2].attrVal; }
            //);
            //translator.AddRule(
            //    "E",
            //    new List<string> { "E", "-", "T" },
            //    (TransRule placeholder) =>
            //        { placeholder.head.attrVal = placeholder.body[0].attrVal - placeholder.body[2].attrVal; }
            //);
            //translator.AddRule(
            //    "T",
            //    new List<string> { "F" },
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
            //    new List<string> { "T", "/", "F" },
            //    (TransRule placeholder) =>
            //        { placeholder.head.attrVal = placeholder.body[0].attrVal / placeholder.body[2].attrVal; }
            //);
            //translator.AddRule(
            //    "F",
            //    new List<string> { "i" },
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
            //    new List<string> { "-", "F" },
            //    (TransRule placeholder) =>
            //        { placeholder.head.attrVal = placeholder.body[1].attrVal; }
            //);

            translator.AddRule(
                "E",
                new List<string> { "E", "+", "T" },
                (TransRule placeholder) =>
                    { placeholder.head.attrVal = placeholder.body[0].attrVal + placeholder.body[2].attrVal; }
            );
            translator.AddRule(
                "E",
                new List<string> { "T" },
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
                new List<string> { "F" },
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
                new List<string> { "i" },
                (TransRule placeholder) =>
                    { placeholder.head.attrVal = placeholder.body[0].attrVal; }
            );

            translator.Execute("5 + 3");
        }
    }
}
