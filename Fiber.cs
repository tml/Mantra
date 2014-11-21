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
		public Term Head { get; set; }
		private ConcurrentQueue<Term> messages = new ConcurrentQueue<Term>();

		public Fiber(string name)
		{
			Name = name.GetHashCode();
		}

		public enum Status
		{
			Active,
			Blocking
		}

		public void Receive(Term term)
		{
			messages.Enqueue(term);
		}

		private void FlushReceivedMessages()
		{
			int n = messages.Count;
			for (int i = 0; i < n; ++i)
			{
				Term message;
				messages.TryDequeue(out message);
				if (message != null)
				{
					AppendMessage(message);
				}
			}
		}

		private void AppendMessage(Term message)
		{
			Term last = null;
			for (Term it = Head; it != null; it = it.next)
			{
				last = it;
			}
			if (last == null)
			{
				Head = message;
			}
			else
			{
				last.next = message;
			}
		}

		private LiteralTerm LastLiteral(out Term before)
		{
			before = null;
			LiteralTerm result = null;
			Term last = null;
			for (Term t = Head; t != null; t = t.next)
			{
				if (t is LiteralTerm)
				{
					before = last;
					result = t as LiteralTerm;
				}
				last = t;
			}
			return result;
		}

		private void Insert(Term before, Term middle, Term after)
		{
			if (middle != null)
			{
				if (before == null)
				{
					Head = middle;
				}
				else
				{
					before.next = middle;
				}
				Term last = null;
				for (Term it = middle; it != null; it = it.next)
				{
					last = it;
				}
				last.next = after;
			}
			else if (before == null)
			{
				Head = after;
			}
			else
			{
				before.next = after;
			}
		}

		public void Evaluate(RuleSet rules)
		{
			FlushReceivedMessages();
			while (PerformStep(rules) == Status.Active) ;
			Term it;
			for (it = Head; it != null && !(it is LiteralTerm); it = it.next) ;
			Head = it;
		}

		public Status PerformStep(RuleSet rules)
		{
			Term before;
			LiteralTerm primary = LastLiteral(out before);
			if (primary == null)
			{
				return Status.Blocking;
			}

			Rule rule = rules.Get(primary.name);
			if (rule == null)
			{
				return Status.Blocking;
			}

			Term after = primary.next;
			int numConsumed = 0;

			Term result = null;
			if (rule.hardCoded != null)
			{
				numConsumed = rule.nArgs;
				for (int i = 0; i < numConsumed; ++i)
				{
					if (after == null)
					{
						return Status.Blocking;
					}
					after = after.next;
				}
				result = rule.hardCoded(primary.next);
			}
			else
			{
				Status error;
				result = DoRule(rule, primary.next, out error, ref numConsumed);
				if (error == Status.Blocking)
				{
					return Status.Blocking;
				}
				for (int i = 0; i < numConsumed; ++i)
				{
					after = after.next;
				}
			}

			Insert(before, result, after);
			return Status.Active;
		}

		private Term DoRule(Rule rule, Term arguments, out Status error, ref int numConsumed)
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
				error = Match(matches, pattern, arguments, ref numConsumed);
				if (error == Status.Blocking)
				{
					continue;
				}
				if (body == null)
				{
					return null;
				}
				return Rewrite(body.CopyChain(), matches);
			}
			error = Status.Blocking;
			return null;
		}

		private Term Rewrite(Term term, Dictionary<int, Term> matched)
		{
			if (term == null) return null;
			if (term is LiteralTerm)
			{
				LiteralTerm literal = term as LiteralTerm;
				Term result;
				matched.TryGetValue(literal.name, out result);
				if (result != null)
				{
					result = result.CopySingle();
					result.next = Rewrite(term.next, matched);
					return result;
				}
			}
			else if (term is ListTerm)
			{
				(term as ListTerm).head = Rewrite((term as ListTerm).head, matched);
			}
			term.next = Rewrite(term.next, matched);
			return term;
		}

		private Status Match(Dictionary<int, Term> toMatch, Term pattern, Term arguments, ref int numConsumed)
		{
			if (pattern is LiteralTerm && (pattern as LiteralTerm).name == "..".GetHashCode()) return Status.Active;
			if (pattern == null) return Status.Active;
			if (arguments == null) return Status.Blocking;
			if (pattern is LiteralTerm && (pattern as LiteralTerm).name != "_".GetHashCode())
			{
				toMatch.Add((pattern as LiteralTerm).name, arguments);
			}
			else if (pattern is ListTerm)
			{
				if (!(arguments is ListTerm)) return Status.Blocking;
				int i = 0;
				var list = pattern as ListTerm;
				if (list.head != null &&
					list.head.Count >= 3)
				{
					LiteralTerm butLast = null;
					LiteralTerm last = null;
					int numTaken = 0;
					for (Term it = list.head; it != null; it = it.next)
					{
						butLast = last;
						last = it as LiteralTerm;
						numTaken += 1;
					}
					numTaken -= 2;
					if (butLast.name == "..".GetHashCode())
					{
						if (Match(toMatch, list.head, (arguments as ListTerm).head, ref i) == Status.Blocking)
						{
							return Status.Blocking;
						}
						Term tail = (arguments as ListTerm).head;
						for (int j = 0; j < numTaken; ++j)
						{
							tail = tail.next;
						}
						if (tail == null)
						{
							toMatch.Add(last.name, new ListTerm(null, null));
						}
						else
						{
							toMatch.Add(last.name, new ListTerm(tail.CopyChain(), null));
						}
						numConsumed += 1;
						return Match(toMatch, pattern.next, arguments.next, ref numConsumed);
					}
				}
				if ((pattern as ListTerm).head == null && (arguments as ListTerm).head == null)
				{
					numConsumed += 1;
					return Match(toMatch, pattern.next, arguments.next, ref numConsumed);
				}
				if ((pattern as ListTerm).head == null || (arguments as ListTerm).head == null) return Status.Blocking;
				if ((pattern as ListTerm).head.Count != (arguments as ListTerm).head.Count) return Status.Blocking;
				if (Match(toMatch, (pattern as ListTerm).head, (arguments as ListTerm).head, ref i) == Status.Blocking) return Status.Blocking;
			}
			else if (pattern is NumberTerm &&
				(pattern as NumberTerm).number != (arguments as NumberTerm).number)
			{
				return Status.Blocking;
			}
			numConsumed += 1;
			return Match(toMatch, pattern.next, arguments.next, ref numConsumed);
		}
	}
}
