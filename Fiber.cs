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
				int index = Terms.FindIndex(t => t is LiteralTerm);
				if (index == -1)
				{
					Terms.Clear();
				}
				else
				{
					Terms.RemoveRange(0, index);
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
			LiteralTerm primary = Terms[primaryIndex] as LiteralTerm;

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
				result = rule.hardCoded(Terms.Skip(primaryIndex + 1).Take(numConsumed));
			}
			else
			{
				Status error;
				result = DoRule(rule, Terms.Skip(primaryIndex + 1), out error, ref numConsumed);
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

		private IEnumerable<Term> DoRule(Rule rule, IEnumerable<Term> arguments, out Status error, ref int numConsumed)
		{
			if (rule.patternHeads.Count == 0)
			{
				error = Status.Active;
				return null;
			}
			foreach (var tuple in rule.patternHeads.Zip(rule.bodyHeads, (a, b) => Tuple.Create(a, b)))
			{
				var pattern = tuple.Item1;
				var body = tuple.Item2;
				Dictionary<int, Term> matches = new Dictionary<int, Term>();
				numConsumed = 0;
				if (!Match(matches, pattern, arguments, ref numConsumed))
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

		private IEnumerable<Term> Rewrite(IEnumerable<Term> terms, Dictionary<int, Term> matched)
		{
			foreach (var term in terms)
			{
				if (term is LiteralTerm)
				{
					Term replaceWith;
					matched.TryGetValue((term as LiteralTerm).name, out replaceWith);
					if (replaceWith != null)
					{
						yield return replaceWith.Copy();
					}
					else
					{
						yield return term;
					}
				}
				else if (term is ListTerm)
				{
					yield return new ListTerm(Rewrite((term as ListTerm).terms, matched));
				}
				else
				{
					yield return term;
				}
			}
		}

		private bool Match(Dictionary<int, Term> toMatch, IEnumerable<Term> pattern, IEnumerable<Term> arguments, ref int numConsumed)
		{
			if (arguments.Count() < pattern.Count()) return false;
			var patternIt = pattern.GetEnumerator();
			var argumentsIt = arguments.GetEnumerator();
			while (patternIt.MoveNext() && argumentsIt.MoveNext())
			{
				var left = patternIt.Current;
				var right = argumentsIt.Current;
				if (left is LiteralTerm &&
					(left as LiteralTerm).name != "_".GetHashCode())
				{
					toMatch.Add((left as LiteralTerm).name, right);
				}
				else if (left is ListTerm)
				{
					if (!(right is ListTerm)) return false;

					var list = left as ListTerm;
					if (list.terms.Count >= 3 &&
						list.terms[list.terms.Count - 2] is LiteralTerm &&
						(list.terms[list.terms.Count - 2] as LiteralTerm).name == "..".GetHashCode())
					{
						int i = 0;
						if (!Match(toMatch, list.terms.Take(list.terms.Count - 2), (right as ListTerm).terms, ref i))
						{
							return false;
						}
						toMatch.Add((list.terms[list.terms.Count - 1] as LiteralTerm).name,
							new ListTerm((right as ListTerm).terms.Skip(list.terms.Count - 2)));
					}
					else if ((left as ListTerm).terms.Count != (right as ListTerm).terms.Count ||
						!Match(toMatch, (left as ListTerm).terms, (right as ListTerm).terms, ref numConsumed))
					{
						return false;
					}
				}
				else if (left is NumberTerm)
				{
					if (!(right is NumberTerm)) return false;
					if ((left as NumberTerm).number != (right as NumberTerm).number) return false;
				}
				numConsumed += 1;
			}
			return true;
		}
	}
}
