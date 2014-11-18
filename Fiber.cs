﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mantra
{
	class Fiber
	{
		public Term Head { get; set; }

		public enum Status
		{
			Active,
			Blocking
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
			while (PerformStep(rules) == Status.Active) ;
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
			for (int i = 0; i < rule.nArgs; ++i)
			{
				if (after == null)
				{
					return Status.Blocking;
				}
				after = after.next;
			}

			Term result = null;
			if (rule.hardCoded != null)
			{
				result = rule.hardCoded(primary.next);
			}
			else
			{
				Status error;
				result = DoRule(rule, primary.next, out error);
			}

			Insert(before, result, after);
			return Status.Active;
		}

		private Term DoRule(Rule rule, Term arguments, out Status error)
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
				error = Match(matches, pattern, arguments);
				if (error == Status.Blocking)
				{
					continue;
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

		private Status Match(Dictionary<int, Term> toMatch, Term pattern, Term arguments)
		{
			if (pattern == null) return Status.Active;
			if (arguments == null) return Status.Blocking;
			if (pattern is LiteralTerm)
			{
				toMatch.Add((pattern as LiteralTerm).name, arguments);
			}
			else if (pattern is ListTerm)
			{
				if (!(arguments is ListTerm)) return Status.Blocking;
				Match(toMatch, (pattern as ListTerm).head, (arguments as ListTerm).head);
			}
			else if (pattern is NumberTerm)
			{
				if ((pattern as NumberTerm).number != (arguments as NumberTerm).number)
				{
					return Status.Blocking;
				}
			}
			return Match(toMatch, pattern.next, arguments.next);
		}

		public void Receive(Term term)
		{
			Term last = null;
			for (Term it = Head; it != null; it = it.next)
			{
				last = it;
			}
			if (last == null)
			{
				Head = term;
			}
			else
			{
				last.next = term;
			}
		}
	}
}
