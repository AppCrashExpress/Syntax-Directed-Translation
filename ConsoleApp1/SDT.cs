using System;
using System.Collections.Generic;
using System.Text;

namespace MPTranslator
{
    public struct TransNonTerm
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
        public TransNonTerm head;
        public List<TransNonTerm> body;
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
                stateSet = FindAllStates();


            }
            private HashSet<TItemState> FindAllStates()
            {
                HashSet<TransNonTerm> allTokens = new HashSet<TransNonTerm> { };
                foreach (TransNonTerm nonterm in parent.nonterms)
                {
                    allTokens.Add(nonterm);
                }
                foreach (TransNonTerm term in parent.terms)
                {
                    allTokens.Add(term);
                }

                HashSet<TItemState> foundStates = new HashSet<TItemState> { };
                Queue<TItemState> transitionlessItems = new Queue<TItemState> { };

                    // Didn't want to create a variable for one time use
                    // So, created a variable with reuseable name
                    // These three lines add initial state kernel [S' -> .S]
                TItemState kernelState = new TItemState(new TItem(0, 0));
                foundStates.Add(kernelState);
                transitionlessItems.Enqueue(kernelState);

                while(transitionlessItems.Count != 0)
                {
                    kernelState = transitionlessItems.Dequeue();
                    TItemState kernelClosure = ComputeClosure(kernelState);

                    foreach(TransNonTerm token in allTokens)
                    {
                        TItemState newState = Goto(kernelClosure, token);
                        if (newState.stateItems.Count == 0)
                            { continue; }
                        kernelState.transitions[token] = newState;

                        bool stateInSet = false;
                        foreach(TItemState state in foundStates)
                        {
                            if (state.Equals(newState)) 
                            { 
                                stateInSet = true; 
                                break;
                            }
                        }

                        if (!stateInSet)
                        {
                            foundStates.Add(newState);
                            transitionlessItems.Enqueue(newState);
                        }
                    }
                }

                return foundStates;
            }

            public void Parse()
            {

            }

            private TItemState ComputeClosure(TItemState itemState)
            {
                List<TransRule> rules = parent.rules;
                HashSet<TItem> closure = new HashSet<TItem>(itemState.stateItems);
                int prevCount = 0;

                // Keep adding new items until size doesn't change
                while( (closure.Count - prevCount) > 0 )
                {
                    prevCount = closure.Count;
                    foreach(TItem item in new HashSet<TItem>(closure)) // Clone because c# is dumb
                    {
                        if (rules[item.ruleNum].body.Count == item.dotPos) continue;

                        TransNonTerm nextToken = rules[item.ruleNum].body[item.dotPos];
                        for(int i = 0; i < rules.Count; ++i)
                        {
                            if (nextToken.nonterm != rules[i].head.nonterm) continue;
                            TItem newItem = new TItem(i, 0);
                            if (closure.Contains(newItem)) continue;
                            closure.Add(newItem);
                        }
                    }
                }

                return new TItemState(closure);
            }

            private TItemState Goto(TItemState itemState, TransNonTerm nextTerm)
            {
                HashSet<TItem> resultingSet = new HashSet<TItem> { };

                foreach(TItem item in itemState.stateItems)
                {
                    TransRule rule = parent.rules[item.ruleNum];
                    if (rule.body.Count == item.dotPos) continue;
                    if (rule.body[item.dotPos].nonterm == nextTerm.nonterm)
                    {
                        resultingSet.Add(new TItem(item.ruleNum, item.dotPos + 1));
                    }
                }

                return new TItemState(resultingSet);
            }

            private Action<TParsingTable> CreateShiftCallback()
            {
                return null;
            }

            private Action<TParsingTable> CreateReduceCallback()
            {
                return null;
            }

            private SSDT                parent;
            private HashSet<TItemState> stateSet;
            private Stack<int>          stateStack;
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
                    stateItems = new HashSet<TItem> {oneItem};
                    transitions = new Dictionary<TransNonTerm, TItemState> { };
                }

                public TItemState(HashSet<TItem> itemList)
                {
                    stateItems = itemList;
                    transitions = new Dictionary<TransNonTerm, TItemState> { };
                }

                public bool Equals(TItemState other)
                {
                    if (this.stateItems.SetEquals(other.stateItems))
                        return true;
                    else
                        return false;
                }

                public HashSet<TItem> stateItems;
                public Dictionary<TransNonTerm, TItemState> transitions;
            }
        }
    }

}
