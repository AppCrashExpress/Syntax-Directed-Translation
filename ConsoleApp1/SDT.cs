using System;
using System.Collections.Generic;
using System.Text;

namespace MPTranslator
{
    public class TransNonTerm
    {
        readonly public string nonterm;
        public int attrVal;

        public TransNonTerm(string nonterm)
        {
            this.nonterm = nonterm;
            attrVal = 0;
        }
    }
    public class TransRule
    {
        readonly public TransNonTerm head;
        readonly public List<TransNonTerm> body;
        readonly Action<TransRule> semanticRule;

        public TransRule(TransNonTerm head, List<TransNonTerm> body, Action<TransRule> semanticRule)
        {
            this.head = head;
            this.body = body;
            this.semanticRule = semanticRule;
        }

        public void ApplyRule()
        {
            semanticRule(this);
        }

    }

    public class SSDT
    {
        public SSDT(string beginToken) 
        {
            this.beginToken = beginToken;
        }

        public void AddRule(string head, List<string> body, Action<TransRule> semanticRule)
        {
            List<TransNonTerm> convertedBody = new List<TransNonTerm> { };
            foreach (string nt in body)
            {
                convertedBody.Add(new TransNonTerm(nt));
            }
            rules.Add(new TransRule(new TransNonTerm(head), convertedBody, semanticRule));
        }

        public void Execute(string input)
        {
            AddZeroRule();

            // Add ParsingTable

            inputStack = ConvertInput(input);
            // int result = parsingTable.Parse();
        }

        private void AddZeroRule()
        {
            string newHead = beginToken + "'";
            TransRule zeroRule = new TransRule(new TransNonTerm(newHead),
                                               new List<TransNonTerm> { new TransNonTerm(beginToken) },
                                               (TransRule thisPlaceholder) => 
                                                    { thisPlaceholder.head.attrVal = thisPlaceholder.body[0].attrVal; } );
            beginToken = newHead;
        }

        Stack<TransNonTerm> ConvertInput(string input)
        {
            Stack<TransNonTerm> newInputStack = new Stack<TransNonTerm> { };
            string[] splitInput = input.Split(' ');

            foreach(string token in splitInput)
            {
                int val = 0;
                TransNonTerm termToAdd;
                if (Int32.TryParse(token, out val))
                {
                    termToAdd = new TransNonTerm("i");
                    termToAdd.attrVal = val;
                }
                else
                {
                    termToAdd = new TransNonTerm(token);
                }

                newInputStack.Push(termToAdd);
            }

            return newInputStack;
        }

        string beginToken;
        List<TransRule> rules;
        Stack<TransNonTerm> inputStack;

        public class TParsingTable
        {
            public TParsingTable(SSDT parent)
            {
                this.parent = parent;
            }

            public void Parse()
            {

            }

            private void Goto(int state, TransNonTerm token)
            {

            }

            private SSDT parent;
        }

        private struct TItem
        {
            // Represent item using number of rule
            // and position of dot in it. This helps
            // avoid cloning the rule.
            // (Could instead create reference to rule?)
            public TItem(int ruleNum, int dotPos)
            {
                this.ruleNum = ruleNum;
                this.dotPos = dotPos;
            }

            public int ruleNum;
            public int dotPos;
        }
    }

}
