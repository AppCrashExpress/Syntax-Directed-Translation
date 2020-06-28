using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace MPTranslator
{
    public struct TransNonTerm
    {
        public TransNonTerm(string nonterm, int attrVal = 0)
        {
            this.nonterm = nonterm;
            this.attrVal = attrVal;
        }

        readonly public string nonterm;
        public int attrVal;
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
            firstSets  = ComputeFirstSets();
            followSets = ComputeFollowSets();

            parsingTable.BuildTable();

            inputQueue = ConvertInput(input);
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

        private Queue<TransNonTerm> ConvertInput(string input)
        {
            Queue<TransNonTerm> newInputQueue = new Queue<TransNonTerm> { };
            string[] splitInput = input.Split(' ');

            foreach(string token in splitInput)
            {
                int val = 0;
                TransNonTerm termToAdd;
                if (Int32.TryParse(token, out val))
                {
                    termToAdd = new TransNonTerm("i", val);
                }
                else
                {
                    termToAdd = new TransNonTerm(token);
                }

                newInputQueue.Enqueue(termToAdd);
            }
            newInputQueue.Enqueue(new TransNonTerm("$"));

            return newInputQueue;
        }

        // retards can't make typedef lmao
        private Dictionary<TransNonTerm, HashSet<TransNonTerm>> ComputeFirstSets()
        {
            Dictionary<TransNonTerm, HashSet<TransNonTerm>> firstSets = InitilizeSets();

            bool notDone;
            do {
                notDone = false;

                // "bool1 |= bool2" is the same is "bool1 = bool1 | bool2"
                // If either of them is true, then bool1 is true
                // If we get at least one true, then the set changed
                //     and calculation continues
                foreach (TransRule rule in rules)
                {
                    if (rule.body.Count == 1 && rule.body[0].nonterm == "")
                    {
                        notDone |= firstSets[rule.head].Add(new TransNonTerm(""));
                    }
                    else
                    {
                        notDone |= ComputeBodyFirstSets(firstSets, rule);
                    }
                }
            } while (notDone);

            foreach (TransNonTerm term in terms)
            {
                firstSets.Add(term, new HashSet<TransNonTerm> { term });
            }

            return firstSets;
        }

        // If rule contains body other than one empty symbol, create firsts set for it
        private bool ComputeBodyFirstSets(Dictionary<TransNonTerm, HashSet<TransNonTerm>> firstSets, TransRule rule)
        {
            bool notDone = false;
            bool containsEpsilon = true;

            foreach(TransNonTerm token in rule.body)
            {
                containsEpsilon = false;

                if (terms.Contains(token))
                {
                    notDone |= firstSets[rule.head].Add(token);
                    break;
                }

                foreach(TransNonTerm element in firstSets[token])
                {
                    if (element.Equals(new TransNonTerm("")))
                        { containsEpsilon = true; }

                    notDone |= firstSets[rule.head].Add(element);
                }

                if (!containsEpsilon)
                    { break; }
            }

            if (containsEpsilon)
            {
                notDone |= firstSets[rule.head].Add(new TransNonTerm(""));
            }

            return notDone;
        }

        private Dictionary<TransNonTerm, HashSet<TransNonTerm>> ComputeFollowSets()
        {
            Dictionary<TransNonTerm, HashSet<TransNonTerm>> followSets = InitilizeSets();
            followSets[rules[0].head].Add(new TransNonTerm("$"));

            bool notDone;
            do {
                notDone = false;

                foreach(TransRule rule in rules)
                {
                    for(int i = 0; i < rule.body.Count; ++i)
                    {
                        TransNonTerm token = rule.body[i];
                        if (!nonterms.Contains(token))
                            { continue; }

                        HashSet<TransNonTerm> nextTokenFirsts = GetNextTokenFirsts(rule, i + 1);
                        foreach(TransNonTerm first in nextTokenFirsts)
                        {
                            if ( first.Equals(new TransNonTerm("")) )
                            {
                                foreach(TransNonTerm headFirst in followSets[rule.head])
                                {
                                    notDone |= followSets[token].Add(headFirst);
                                }
                            }
                            else
                            {
                                notDone |= followSets[token].Add(first);
                            }
                        }
                    }
                }

            } while (notDone);

            return followSets;
        }

        private HashSet<TransNonTerm> GetNextTokenFirsts(TransRule rule, int nextTokenIndex)
        {
            HashSet<TransNonTerm> result = new HashSet<TransNonTerm> { };
            bool containsEpsilon = true;

            for (int i = nextTokenIndex; i < rule.body.Count; ++i)
            {
                TransNonTerm token = rule.body[i];
                containsEpsilon = false;

                if (terms.Contains(token))
                {
                    result.Add(token);
                    break;
                }

                foreach(TransNonTerm first in firstSets[token])
                {
                    if (first.Equals(new TransNonTerm("")))
                        { containsEpsilon = true; }

                    result.Add(first);
                }

                containsEpsilon |= firstSets[token].Count == 0;

                if (!containsEpsilon)
                    { break; }
            }

            if (containsEpsilon)
                { result.Add(new TransNonTerm("")); }

            return result;
        }

        private Dictionary<TransNonTerm, HashSet<TransNonTerm>> InitilizeSets()
        {
            Dictionary<TransNonTerm, HashSet<TransNonTerm>> fSets = new Dictionary<TransNonTerm, HashSet<TransNonTerm>> { };
            fSets.Add(new TransNonTerm(beginToken), new HashSet<TransNonTerm> { });
            foreach (TransNonTerm nonterm in nonterms)
            {
                fSets.Add(nonterm, new HashSet<TransNonTerm> { });
            }

            return fSets;
        }

        private string          beginToken;
        private TransNonTerm[]  terms;
        private TransNonTerm[]  nonterms;
        private List<TransRule> rules;
        private Dictionary<TransNonTerm, HashSet<TransNonTerm>> firstSets;
        private Dictionary<TransNonTerm, HashSet<TransNonTerm>> followSets;

        private Stack<int> stateStack;
        private Stack<TransNonTerm> tokenStack;
        private Queue<TransNonTerm> inputQueue;
        private TParsingTable parsingTable;

        public class TParsingTable
        {
            public TParsingTable(SSDT parent)
            {
                this.parent = parent;
                ruleList = parent.rules;
                stateStack = parent.stateStack;
                tokenStack = parent.tokenStack;
                inputQueue = parent.inputQueue;
            }
            
            public void BuildTable()
            {
                stateList = FindAllStates();


            }

            public void Parse()
            {

            }

            private List<TItemState> FindAllStates()
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

                List<TItemState> foundStates = new List<TItemState> { };
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

            private void GenerateTable()
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

            private Action CreateShiftCallback(int newState)
            {
                return () => {
                    stateStack.Push(newState);
                    tokenStack.Push(inputQueue.Dequeue());
                };
            }

            private Action CreateReduceCallback(int ruleIndex)
            {
                return () => {

                };
            }

            private SSDT             parent;
            private List<TItemState> stateList;

            private List<TransRule>     ruleList;
            private Stack<int>          stateStack;
            private Stack<TransNonTerm> tokenStack;
            private Queue<TransNonTerm> inputQueue;

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

                public int stateId;
                public HashSet<TItem> stateItems;
                public Dictionary<TransNonTerm, TItemState> transitions;
            }
        }
    }

}
