using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace MPTranslator
{
    public struct TransToken
    {
        public TransToken(string token, int attrVal = 0)
        {
            this.token = token;
            this.attrVal = attrVal;
        }

        public override bool Equals(object obj)
        {
            if ( !(obj is TransToken) )
                { return false; }

            TransToken other = (TransToken)obj;
            return this.token == other.token;
        }

        public override int GetHashCode()
        {
            return token.GetHashCode();
        }

        readonly public string token;
        public int attrVal;
    }

    public class TransRule
    {
        public TransRule(TransToken head, List<TransToken> body, Action<TransRule> semanticRule)
        {
            this.head = head;
            this.body = body;
            this.semanticRule = semanticRule;
        }

        public void ApplyRule()
        {
            semanticRule(this);
        }

        public TransToken       head;
        public List<TransToken> body;
        readonly private Action<TransRule> semanticRule;
    }

    public class SSDT
    {
        public SSDT(string[] terms, string[] nonterms, string beginToken) 
        {
            this.beginToken = beginToken;
            this.terms      = Array.ConvertAll(terms,    (str) => new TransToken(str));
            this.nonterms   = Array.ConvertAll(nonterms, (str) => new TransToken(str));

            rules = new List<TransRule> { };
        }

        public void AddRule(string head, List<string> body, Action<TransRule> semanticRule)
        {
            List<TransToken> convertedBody = body.ConvertAll((str) => new TransToken(str));
            rules.Add(new TransRule(new TransToken(head), convertedBody, semanticRule));
        }

        public int Execute(string input)
        {
            if (parsingTable == null)
            { 
                PrintGrammar();
                Initilize();
                PrintSets();
            }

            return parsingTable.Parse(ConvertInput(input));
        }

        private void Initilize()
        {
            AddZeroRule();
            firstSets  = ComputeFirstSets();
            followSets = ComputeFollowSets();

            parsingTable = new TParsingTable(this);
            parsingTable.BuildTable();
        }

        private void AddZeroRule()
        {
            string newHead = beginToken + "'";
            TransRule zeroRule = new TransRule(new TransToken(newHead),
                                               new List<TransToken> { new TransToken(beginToken) },
                                               (TransRule thisPlaceholder) => 
                                                    { thisPlaceholder.head.attrVal = thisPlaceholder.body[0].attrVal; } );
            rules.Insert(0, zeroRule);
            beginToken = newHead;
        }

        private Queue<TransToken> ConvertInput(string input)
        {
            Queue<TransToken> newInputQueue = new Queue<TransToken> { };
            string[] splitInput = input.Split(' ');

            foreach(string token in splitInput)
            {
                int val = 0;
                TransToken termToAdd;
                if (Int32.TryParse(token, out val))
                {
                    termToAdd = new TransToken("i", val);
                }
                else
                {
                    termToAdd = new TransToken(token);
                }

                newInputQueue.Enqueue(termToAdd);
            }
            newInputQueue.Enqueue(new TransToken("$"));

            return newInputQueue;
        }

        private Dictionary<TransToken, HashSet<TransToken>> ComputeFirstSets()
        {
            Dictionary<TransToken, HashSet<TransToken>> firstSets = InitilizeSets();

            bool notDone;
            do {
                notDone = false;

                // "bool1 |= bool2" is the same is "bool1 = bool1 | bool2"
                // If either of them is true, then bool1 is true
                // If we get at least one true, then the set changed
                //     and computation continues
                foreach (TransRule rule in rules)
                {
                    if (rule.body.Count == 1 && rule.body[0].token == "")
                    {
                        notDone |= firstSets[rule.head].Add(new TransToken(""));
                    }
                    else
                    {
                        notDone |= ComputeBodyFirstSets(firstSets, rule);
                    }
                }
            } while (notDone);

            foreach (TransToken term in terms)
            {
                firstSets.Add(term, new HashSet<TransToken> { term });
            }

            return firstSets;
        }

        // If rule contains body other than one empty symbol, create firsts set for it
        private bool ComputeBodyFirstSets(Dictionary<TransToken, HashSet<TransToken>> firstSets, TransRule rule)
        {
            bool notDone = false;
            bool containsEpsilon = true;

            foreach(TransToken token in rule.body)
            {
                containsEpsilon = false;

                if (terms.Contains(token))
                {
                    notDone |= firstSets[rule.head].Add(token);
                    break;
                }

                foreach(TransToken element in firstSets[token])
                {
                    if (element.Equals(new TransToken("")))
                        { containsEpsilon = true; }

                    notDone |= firstSets[rule.head].Add(element);
                }

                if (!containsEpsilon)
                    { break; }
            }

            if (containsEpsilon)
            {
                notDone |= firstSets[rule.head].Add(new TransToken(""));
            }

            return notDone;
        }

        private Dictionary<TransToken, HashSet<TransToken>> ComputeFollowSets()
        {
            Dictionary<TransToken, HashSet<TransToken>> followSets = InitilizeSets();
            followSets[rules[0].head].Add(new TransToken("$"));

            bool notDone;
            do {
                notDone = false;

                foreach(TransRule rule in rules)
                {
                    for(int i = 0; i < rule.body.Count; ++i)
                    {
                        TransToken token = rule.body[i];
                        if (!nonterms.Contains(token))
                            { continue; }

                        HashSet<TransToken> nextTokenFirsts = GetNextTokenFirsts(rule, i + 1);
                        foreach(TransToken first in nextTokenFirsts)
                        {
                            if ( first.Equals(new TransToken("")) )
                            {
                                foreach(TransToken headFirst in followSets[rule.head])
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

        private HashSet<TransToken> GetNextTokenFirsts(TransRule rule, int nextTokenIndex)
        {
            HashSet<TransToken> result = new HashSet<TransToken> { };
            bool containsEpsilon = true;

            for (int i = nextTokenIndex; i < rule.body.Count; ++i)
            {
                TransToken token = rule.body[i];
                containsEpsilon = false;

                if (terms.Contains(token))
                {
                    result.Add(token);
                    break;
                }

                foreach(TransToken first in firstSets[token])
                {
                    if (first.Equals(new TransToken("")))
                        { containsEpsilon = true; }

                    result.Add(first);
                }

                containsEpsilon |= firstSets[token].Count == 0;

                if (!containsEpsilon)
                    { break; }
            }

            if (containsEpsilon)
                { result.Add(new TransToken("")); }

            return result;
        }

        private Dictionary<TransToken, HashSet<TransToken>> InitilizeSets()
        {
            Dictionary<TransToken, HashSet<TransToken>> fSets = new Dictionary<TransToken, HashSet<TransToken>> { };
            fSets.Add(new TransToken(beginToken), new HashSet<TransToken> { });
            foreach (TransToken nonterm in nonterms)
            {
                fSets.Add(nonterm, new HashSet<TransToken> { });
            }

            return fSets;
        }

        private void PrintGrammar()
        {
            Console.Write("Non-terminals: ");
            foreach(TransToken nonterm in nonterms)
            {
                Console.Write("{0} ", nonterm.token);
            }
            Console.WriteLine();

            Console.Write("Terminals: ");
            foreach (TransToken term in terms)
            {
                Console.Write("{0} ", term.token);
            }
            Console.WriteLine();

            Console.WriteLine("Starting symbol: {0}", beginToken);

            Console.WriteLine("Rules:");
            foreach(TransRule rule in rules)
            {
                Console.Write("{0} -> ", rule.head.token);
                foreach(TransToken bodyToken in rule.body)
                {
                    Console.Write("{0} ", bodyToken.token);
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        private void PrintSets()
        {
            Console.WriteLine("First sets:");
            foreach(KeyValuePair<TransToken, HashSet<TransToken>> set in firstSets)
            {
                if (terms.Contains(set.Key)) continue;

                Console.Write("{0} : ", set.Key.token);
                foreach(TransToken reachableToken in set.Value)
                {
                    Console.Write("{0} ", reachableToken.token);
                }
                Console.WriteLine();
            }
            Console.WriteLine();

            Console.WriteLine("Follow sets:");
            foreach (KeyValuePair<TransToken, HashSet<TransToken>> set in followSets)
            {
                Console.Write("{0} : ", set.Key.token);
                foreach (TransToken followingToken in set.Value)
                {
                    Console.Write("{0} ", followingToken.token);
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        private string          beginToken;
        private TransToken[]    terms;
        private TransToken[]    nonterms;
        private List<TransRule> rules;
        private Dictionary<TransToken, HashSet<TransToken>> firstSets;
        private Dictionary<TransToken, HashSet<TransToken>> followSets;

        private TParsingTable parsingTable;

        public class TParsingTable
        {
            public TParsingTable(SSDT parent)
            {
                this.parent = parent;
            }
            
            public void BuildTable()
            {
                stateSet = FindAllStates();
                foreach(TItemState state in stateSet)
                {
                    PrintState(state);
                    Console.WriteLine();
                }

                actionTable = GenerateActionTable();
                Console.WriteLine();
                gotoTable   = GenerateGotoTable();
                Console.WriteLine();
            }

            public int Parse(Queue<TransToken> input)
            {
                inputQueue = input;
                tokenStack = new Stack<TransToken> { };
                stateStack = new Stack<int> { };
                stateStack.Push(0);

                while ( !GetEndCondition() )
                {
                    var tablePosition =
                        new Tuple<int, TransToken> (stateStack.Peek(), inputQueue.Peek());
                    actionTable[tablePosition]();
                }

                return tokenStack.Peek().attrVal;
            }

            private bool GetEndCondition()
            {
                TransToken inputEndSymbol = new TransToken("$");
                return inputQueue.Peek().Equals(inputEndSymbol) && stateStack.Peek() == 1;
            }

            private HashSet<TItemState> FindAllStates()
            {
                var allTokens = new HashSet<TransToken> { };
                allTokens.UnionWith(parent.nonterms);
                allTokens.UnionWith(parent.terms);

                var foundStates         = new HashSet<TItemState> { };
                var transitionlessItems = new Queue<TItemState> { };
                int stateIdCounter = 0;

                    // Didn't want to create a variable for one time use
                    // So, created a variable with reuseable name
                    // These three lines add initial state kernel [S' -> .S]
                    // anNtYWNoaW5lcy5zb3VyY2Vmb3JnZS5uZXQ=
                TItemState kernelState = new TItemState(new TItem(0, 0));
                kernelState.stateId = stateIdCounter++;
                foundStates.Add(kernelState);
                transitionlessItems.Enqueue(kernelState);

                while(transitionlessItems.Count != 0)
                {
                    kernelState = transitionlessItems.Dequeue();
                    TItemState kernelClosure = ComputeClosure(kernelState);

                    foreach(TransToken token in allTokens)
                    {
                        TItemState newState = Goto(kernelClosure, token);
                        if (newState.stateItems.Count == 0)
                            { continue; }
                        kernelState.transitions[token] = newState;

                        bool stateInSet = false;
                        foreach (TItemState oldState in foundStates)
                        {
                            if (oldState.Equals(newState))
                            {
                                kernelState.transitions[token] = oldState;
                                stateInSet = true;
                                break;
                            }
                        }

                        if (!stateInSet)
                        {
                            newState.stateId = stateIdCounter++;
                            foundStates.Add(newState);
                            transitionlessItems.Enqueue(newState);
                        }
                    }
                }

                return foundStates;
            }

            private Dictionary<Tuple<int, TransToken>, Action> GenerateActionTable()
            {
                var table = new Dictionary<Tuple<int, TransToken>, Action> { };

                Console.WriteLine();
                Console.WriteLine("ACTION TABLE:");

                foreach (TItemState state in stateSet)
                {
                    foreach (var transition in state.transitions.Where(entry => parent.terms.Contains(entry.Key))) 
                    {
                        table.Add(
                            new Tuple<int, TransToken> (state.stateId, transition.Key),
                            CreateShiftCallback(transition.Value.stateId)
                        );
                        Console.WriteLine("ACTION[{0}, {1}] = Shift({2})",
                            state.stateId, transition.Key.token, transition.Value.stateId);
                    }

                    for (int i = 0; i < parent.rules.Count; ++i)
                    {
                        // If it doesn't contain rule of type N -> a.
                        if ( !state.stateItems.Contains(new TItem(i, parent.rules[i].body.Count)) )
                        { continue; }

                        foreach(TransToken followToken in parent.followSets[parent.rules[i].head])
                        {
                            table.Add(
                                new Tuple<int, TransToken>(state.stateId, followToken),
                                CreateReduceCallback(i)
                            );
                            Console.WriteLine("ACTION[{0}, {1}] = Reduce({2})",
                                state.stateId, followToken.token, i);
                        }
                    }
                }

                return table;
            }

            private Dictionary<Tuple<int, TransToken>, int> GenerateGotoTable()
            {
                var table = new Dictionary<Tuple<int, TransToken>, int> { };

                Console.WriteLine();
                Console.WriteLine("GOTO TABLE:");

                foreach (TItemState state in stateSet)
                {
                    foreach(var transition in state.transitions.Where(entry => parent.nonterms.Contains(entry.Key)))
                    {
                        table.Add(
                            new Tuple<int, TransToken> (state.stateId, transition.Key),
                            transition.Value.stateId
                        );
                        Console.WriteLine("GOTO[{0}, {1}] = {2}",
                            state.stateId, transition.Key.token, transition.Value.stateId);
                    }
                }

                return table;
            }

            private TItemState ComputeClosure(TItemState itemState)
            {
                List<TransRule> rules     = parent.rules;
                HashSet<TItem>  closure   = new HashSet<TItem>(itemState.stateItems);
                Queue<TItem>    closeless = new Queue<TItem>(itemState.stateItems);

                while(closeless.Count != 0)
                {
                    TItem item = closeless.Dequeue();
                    if (rules[item.ruleNum].body.Count == item.dotPos)
                        { continue; }

                    TransToken nextToken = rules[item.ruleNum].body[item.dotPos];
                    for (int i = 0; i < rules.Count; ++i)
                    {
                        if (nextToken.token != rules[i].head.token) continue;
                        TItem newItem = new TItem(i, 0);

                        if (closure.Add(newItem)) { closeless.Enqueue(newItem); }
                    }
                }

                return new TItemState(closure);
            }

            private TItemState Goto(TItemState itemState, TransToken nextTerm)
            {
                HashSet<TItem> resultingSet = new HashSet<TItem> { };

                foreach(TItem item in itemState.stateItems)
                {
                    TransRule rule = parent.rules[item.ruleNum];
                    if (rule.body.Count == item.dotPos) continue;

                    if (rule.body[item.dotPos].token == nextTerm.token)
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
                    TransRule rule = parent.rules[ruleIndex];

                    for(int i = rule.body.Count; i > 0; --i)
                        { stateStack.Pop(); }
                    for(int i = rule.body.Count - 1; i >= 0; --i)
                    {
                        rule.body[i] = tokenStack.Pop();
                    }

                    rule.ApplyRule();
                    tokenStack.Push(rule.head);

                    stateStack.Push(GetStateFromGoto(stateStack.Peek(), tokenStack.Peek()) );
                };
            }

            private int GetStateFromGoto(int state, TransToken token)
            {
                return gotoTable[new Tuple<int, TransToken> (state, token)];
            }

            private SSDT                parent;
            private HashSet<TItemState> stateSet;

            private Stack<int>        stateStack;
            private Stack<TransToken> tokenStack;
            private Queue<TransToken> inputQueue;

            private Dictionary<Tuple<int, TransToken>, Action> actionTable;
            private Dictionary<Tuple<int, TransToken>, int>    gotoTable;

            public void PrintState(TItemState state)
            {
                TItemState closure = ComputeClosure(state);

                Console.Write("I{0} : ", state.stateId);
                PrintItemSet(state.stateItems);
                Console.WriteLine();

                Console.Write("Closure: ");
                PrintItemSet(closure.stateItems);
                Console.WriteLine();

                foreach(KeyValuePair<TransToken, TItemState> trans in state.transitions)
                {
                    Console.Write("GOTO[{0}, {1}] : ", trans.Value.stateId, trans.Key.token);
                    PrintItemSet(trans.Value.stateItems);
                    Console.WriteLine();
                }
            }

            private void PrintItemSet(HashSet<TItem> itemSet)
            {
                Console.Write("{ ");
                foreach (TItem item in itemSet)
                {
                    PrintItem(item);
                    Console.Write("; ");
                }
                Console.Write("}");
            }

            private void PrintItem(TItem item)
            {
                TransRule rule = parent.rules[item.ruleNum];
                Console.Write("{0} -> ", rule.head.token);
                for (int i = 0; i < item.dotPos; ++i)
                {
                    Console.Write("{0} ", rule.body[i].token);
                }
                Console.Write(".");
                for (int i = item.dotPos; i < rule.body.Count; ++i)
                {
                    Console.Write("{0} ", rule.body[i].token);
                }
            }

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
                    transitions = new Dictionary<TransToken, TItemState> { };
                }

                public TItemState(HashSet<TItem> itemList)
                {
                    stateItems = itemList;
                    transitions = new Dictionary<TransToken, TItemState> { };
                }

                public override bool Equals(object obj)
                {
                    if ( !(obj is TItemState) )
                        { return false; }

                    TItemState other = (TItemState) obj;
                    if (this.stateItems.SetEquals(other.stateItems))
                        return true;
                    else
                        return false;
                }

                public override int GetHashCode()
                {
                    int hash = 0;
                    foreach (TItem item in stateItems)
                    {
                        hash ^= item.GetHashCode();
                    }
                    return hash;
                }

                public  int stateId;
                public  HashSet<TItem> stateItems;
                public  Dictionary<TransToken, TItemState> transitions;
            }
        }
    }

}
