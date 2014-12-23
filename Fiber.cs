using Microsoft.FSharp.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mantra
{
	public class Fiber : IReceiver
	{
		public int Name { get; set; }
		public List<Term> Terms { get; set; }
		private ConcurrentQueue<IEnumerable<Term>> messages = new ConcurrentQueue<IEnumerable<Term>>();

		public Fiber(string name)
		{
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

		private void FlushReceivedMessages()
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

			IEnumerable<Term> result = null;
			if (rule.hardCoded != null)
			{
				numConsumed = rule.nArgs;
				if (numConsumed + primaryIndex > Terms.Count)
				{
					return Status.Blocking;
				}
				var a = Terms.Skip(primaryIndex + 1).Take(numConsumed).ToList();
				result = rule.hardCoded(a);
			}
			else
			{
				Status error;
				var a = Terms.Skip(primaryIndex + 1);
				result = DoRule(rule, a, out error, ref numConsumed);
				if (error == Status.Blocking)
				{
					return Status.Blocking;
				}
			}

			Terms.RemoveRange(primaryIndex, numConsumed + 1);
			Terms.InsertRange(primaryIndex, result);
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

		private List<Term> DoRule(Rule rule, IEnumerable<Term> arguments, out Status error, ref int numConsumed)
		{
			if (rule.patternHeads.Count == 0)
			{
				error = Status.Active;
				return null;
			}
			int argsCount = arguments.Count();
			foreach (var tuple in rule.patternHeads.Zip(rule.bodyHeads, (a, b) => Tuple.Create(a, b)))
			{
				var pattern = tuple.Item1;
				var body = tuple.Item2;
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

		private List<Term> Rewrite(IEnumerable<Term> terms, Dictionary<int, Term> matched)
		{
			List<Term> rewritten = new List<Term>();
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
						rewritten.Add(term);
					}
				}
				else if (term is ListTerm)
				{
					rewritten.Add(new ListTerm(Rewrite(((ListTerm)term).terms, matched)));
				}
				else
				{
					rewritten.Add(term);
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
					toMatch.Add(((LiteralTerm)left).name, right);
				}
				else if (left is ListTerm)
				{
					if (!(right is ListTerm)) return false;

					var list = (ListTerm)left;
					if (ListModule.Length(list.terms) >= 3 &&
						list.terms[ListModule.Length(list.terms) - 2] is LiteralTerm &&
						((LiteralTerm)list.terms[ListModule.Length(list.terms) - 2]).name == "..".GetHashCode())
					{
						int i = 0;
						if (!Match(toMatch, list.terms.Take(ListModule.Length(list.terms) - 2), ((ListTerm)right).terms, ((ListTerm)right).terms.Count(), ref i))
						{
							return false;
						}
						toMatch.Add(((LiteralTerm)list.terms[ListModule.Length(list.terms) - 1]).name,
							new ListTerm(((ListTerm)right).terms.Skip(ListModule.Length(list.terms) - 2)));
					}
					else if (ListModule.Length(((ListTerm)left).terms) != ListModule.Length(((ListTerm)right).terms) ||
						!Match(toMatch, ((ListTerm)left).terms, ((ListTerm)right).terms, ((ListTerm)right).terms.Count(), ref numConsumed))
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
	}
}
