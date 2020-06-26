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
        public SSDT(string[] terms, string[] nonterms, string beginToken) 
        {
            this.beginToken = beginToken;
            this.terms = Array.ConvertAll<string, TransNonTerm>(terms, (str) => new TransNonTerm(str));
            this.nonterms = Array.ConvertAll<string, TransNonTerm>(nonterms, (str) => new TransNonTerm(str));

            rules = new List<TransRule> { };
            parsingTable = new TParsingTable(this);
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

            parsingTable.Create();

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
            rules.Insert(0, zeroRule);
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
        TransNonTerm[] terms;
        TransNonTerm[] nonterms;
        List<TransRule> rules;
        Stack<TransNonTerm> inputStack;
        TParsingTable parsingTable;

        public class TParsingTable
        {
            public TParsingTable(SSDT parent)
            {
                this.parent = parent;
            }
            
            public void Create()
            {
                List<TransNonTerm> allTokens = new List<TransNonTerm> { };
                allTokens.AddRange(parent.terms);
                allTokens.AddRange(parent.nonterms);

                HashSet<TItemState> stateSet = new HashSet<TItemState> { };
                Queue<TItemState> transitionlessItems = new Queue<TItemState> { };

                TItemState newState = new TItemState(new TItem(0, 0));
                stateSet.Add(newState);
                transitionlessItems.Enqueue(newState);

                // do a while loop
                while(transitionlessItems.Count != 0)
                {
                    List<TItem> closure = ComputeClosure(transitionlessItems.Dequeue());
                }
            }

            public void Parse()
            {

            }

            private List<TItem> ComputeClosure(List<TItem> itemSet)
            {
                List<TItem> closure = new List<TItem>(itemSet);
                List<TransRule> rules = parent.rules;
                int prevCount = 0;

                // Keep adding new items until size doesn't change
                while( (closure.Count - prevCount) > 0 )
                {
                    prevCount = closure.Count;
                    foreach(TItem item in closure)
                    {
                        TransNonTerm nextToken = rules[item.ruleNum].body[item.dotPos];
                        for(int i = 0; i < rules.Count; ++i)
                        {
                            if (nextToken != rules[i].head) continue;
                            TItem newItem = new TItem(i, 0);
                            if (closure.Contains(newItem)) continue;
                            closure.Add(newItem);
                        }
                    }
                }

                return null;
            }

            private List<TItem> Goto(List<TItem> itemSet, TransNonTerm nextTerm)
            {
                List<TItem> resultingSet = new List<TItem> { };

                foreach(TItem item in itemSet)
                {
                    TransRule rule = parent.rules[item.ruleNum];
                    if (rule.body[item.dotPos] == nextTerm)
                    {
                        resultingSet.Add(new TItem(item.ruleNum, item.dotPos + 1));
                    }
                }

                return resultingSet;
            }

            private Action<TParsingTable> CreateShiftCallback()
            {
                return null;
            }

            private Action<TParsingTable> CreateReduceCallback()
            {
                return null;
            }

            private SSDT parent;
            private Stack<int> stateStack;
            private Stack<TransNonTerm> readTokens;

            public struct TItem
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

            public class TItemState
            {
                public TItemState(TItem oneItem)
                {
                    stateItems = new List<TItem> {oneItem};
                    transitions = new Dictionary<TransNonTerm, TItemState> { };
                }

                public TItemState(List<TItem> itemList)
                {
                    stateItems = itemList;
                    transitions = new Dictionary<TransNonTerm, TItemState> { };
                }

                public List<TItem> stateItems;
                public Dictionary<TransNonTerm, TItemState> transitions;
            }
        }
    }

}
