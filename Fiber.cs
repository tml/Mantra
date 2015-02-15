using Microsoft.FSharp.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mantra
{
	public class Fiber
	{
		public int Name { get; set; }
		public List<Term> Terms { get; private set; }
		private ConcurrentQueue<IEnumerable<Term>> messages = new ConcurrentQueue<IEnumerable<Term>>();

		public Fiber(string name)
		{
			Terms = new List<Term>(1024);
			Name = name.GetHashCode();
		}

		public enum Status
		{
			Active,
			Blocking
		}

		public override string ToString()
		{
			return string.Join(" ", Terms);
		}

		public void Receive(IEnumerable<Term> term)
		{
			messages.Enqueue(term);
		}

		public void FlushReceivedMessages()
		{
			int n = messages.Count;
			for (int i = 0; i < n; ++i)
			{
				IEnumerable<Term> message;
				messages.TryDequeue(out message);
				if (message != null)
				{
					AppendMessage(message);
				}
			}
		}

		private void AppendMessage(IEnumerable<Term> message)
		{
			Terms.AddRange(message);
		}

		public void Evaluate(RuleSet rules, bool cleanUp = true)
		{
			FlushReceivedMessages();
			while (PerformStep(rules) == Status.Active) ;
			if (cleanUp)
			{
				int last = -1;
				for (int i = 0; i < Terms.Count; ++i)
				{
					if (!(Terms[i] is LiteralTerm))
					{
						last = i;
					}
				}
				if (last == -1)
				{
					Terms.Clear();
				}
				else
				{
					Terms.RemoveRange(0, last);
				}
			}
		}

		public Status PerformStep(RuleSet rules)
		{
			int primaryIndex = LastLiteral();
			if (primaryIndex == -1)
			{
				return Status.Blocking;
			}
			LiteralTerm primary = (LiteralTerm)Terms[primaryIndex];

			Rule rule = rules.Get(primary.name);
			if (rule == null)
			{
				return Status.Blocking;
			}

			int numConsumed = 0;

			IList<Term> result = null;
			if (rule.hardCoded != null)
			{
				numConsumed = rule.nArgs;
				if (numConsumed + primaryIndex >= Terms.Count)
				{
					return Status.Blocking;
				}
				result = rule.hardCoded(Terms.Slice(primaryIndex + 1, numConsumed));
			}
			else
			{
				Status error;
				var a = Terms.GetRange(primaryIndex + 1, Terms.Count - (primaryIndex + 1));
				result = DoRule(rule, a, out error, ref numConsumed);
				if (error == Status.Blocking)
				{
					return Status.Blocking;
				}
			}

			int min = Math.Min(result.Count, numConsumed + 1);
			for (int i = 0; i < min; ++i)
			{
				Terms[primaryIndex + i] = result[i];
			}
			if (min < numConsumed + 1)
			{
				Terms.RemoveRange(primaryIndex + min, numConsumed + 1 - min);
			}
			if (min < result.Count)
			{
				Terms.InsertRange(primaryIndex + min, result.Skip(min));
			}
			return Status.Active;
		}

		private int LastLiteral()
		{
			for (int i = Terms.Count - 1; i >= 0; --i)
			{
				if (Terms[i] is LiteralTerm)
				{
					return i;
				}
			}
			return -1;
		}

		private List<Term> DoRule(Rule rule, List<Term> arguments, out Status error, ref int numConsumed)
		{
			if (rule.patternHeads.Count == 0)
			{
				error = Status.Active;
				return null;
			}
			int argsCount = arguments.Count;
			for (int i = 0; i < rule.patternHeads.Count; ++i)
			{
				var pattern = rule.patternHeads[i];
				var body = rule.bodyHeads[i];
				Dictionary<int, Term> matches = new Dictionary<int, Term>();
				numConsumed = 0;
				if (!Match(matches, pattern, arguments, argsCount, ref numConsumed))
				{
					error = Status.Blocking;
					continue;
				}
				error = Status.Active;
				if (body == null)
				{
					return null;
				}
				return Rewrite(body, matches);
			}
			error = Status.Blocking;
			return null;
		}

		private List<Term> Rewrite(IList<Term> terms, Dictionary<int, Term> matched)
		{
			List<Term> rewritten = new List<Term>(terms.Count);
			foreach (var term in terms)
			{
				if (term is LiteralTerm)
				{
					Term replaceWith;
					matched.TryGetValue(((LiteralTerm)term).name, out replaceWith);
					if (replaceWith != null)
					{
						rewritten.Add(replaceWith.Copy());
					}
					else
					{
						rewritten.Add(term.Copy());
					}
				}
				else if (term is ListTerm)
				{
					rewritten.Add(new ListTerm(Rewrite(((ListTerm)term).terms, matched)));
				}
				else
				{
					rewritten.Add(term.Copy());
				}
			}
			return rewritten;
		}

		private bool Match(Dictionary<int, Term> toMatch, IEnumerable<Term> pattern, IEnumerable<Term> arguments, int numArgs, ref int numConsumed)
		{
			if (numArgs < pattern.Count()) return false;
			var patternIt = pattern.GetEnumerator();
			var argumentsIt = arguments.GetEnumerator();
			while (patternIt.MoveNext() && argumentsIt.MoveNext())
			{
				var left = patternIt.Current;
				var right = argumentsIt.Current;
				if (left is LiteralTerm &&
					((LiteralTerm)left).name != "_".GetHashCode())
				{
					if (toMatch.ContainsKey(((LiteralTerm)left).name))
					{
						if (!toMatch[((LiteralTerm)left).name].Equals(right)) return false;
					}
					else
					{
						toMatch.Add(((LiteralTerm)left).name, right);
					}
				}
				else if (left is ListTerm)
				{
					if (!(right is ListTerm)) return false;

					var list = ((ListTerm)left).terms;
					if (list.Count >= 3 &&
						list[list.Count - 2] is LiteralTerm &&
						((LiteralTerm)list[list.Count - 2]).name == "..".GetHashCode())
					{
						int i = 0;
						var innerPattern = ((ListTerm)left).terms.GetRange(0, list.Count - 2);
						var innerRHS = ((ListTerm)right).terms;
						var len = ((ListTerm)right).terms.Count;
						if (!Match(toMatch, innerPattern, innerRHS, len, ref i))
						{
							return false;
						}
						if (((ListTerm)right).terms.Count > 2)
						{
							toMatch.Add(((LiteralTerm)list[list.Count - 1]).name,
								new ListTerm(((ListTerm)right).terms.GetRange(1, ((ListTerm)right).terms.Count - 1)));
						}
						else if (((ListTerm)right).terms.Count > 1)
						{
							toMatch.Add(((LiteralTerm)list[list.Count - 1]).name,
								new ListTerm(((ListTerm)right).terms.GetRange(1, 1)));
						}
						else
						{
							toMatch.Add(((LiteralTerm)list[list.Count - 1]).name,
								new ListTerm(new Term[] { }));
						}
					}
					else if (((ListTerm)left).terms.Count != ((ListTerm)right).terms.Count ||
						!Match(toMatch, ((ListTerm)left).terms, ((ListTerm)right).terms, ((ListTerm)right).terms.Count, ref numConsumed))
					{
						return false;
					}
				}
				else if (left is NumberTerm)
				{
					if (!(right is NumberTerm)) return false;
					if (((NumberTerm)left).number != ((NumberTerm)right).number) return false;
				}
				numConsumed += 1;
			}
			return true;
		}

		private IEnumerable<Term> FTake(FSharpList<Term> fSharpList, int n)
		{
			for (int i = 0; i < n; ++i)
			{
				yield return fSharpList.Head;
				fSharpList = fSharpList.Tail;
			}
		}

		private FSharpList<Term> FSkip(FSharpList<Term> fSharpList, int n)
		{
			for (int i = 0; i < n; ++i)
			{
				fSharpList = fSharpList.Tail;
			}
			return fSharpList;
		}
	}
}
