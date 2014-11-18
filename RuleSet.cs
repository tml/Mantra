using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mantra
{
	class RuleSet
	{
		private Dictionary<int, Rule> rules = new Dictionary<int, Rule>();

		public RuleSet()
		{
			Register(new Rule("+".GetHashCode(), 2, t =>
			{
				return new NumberTerm((t as NumberTerm).number + (t.next as NumberTerm).number, null);
			}));
			Register(new Rule("-".GetHashCode(), 2, t =>
			{
				return new NumberTerm((t as NumberTerm).number - (t.next as NumberTerm).number, null);
			}));
			Register(new Rule("*".GetHashCode(), 2, t =>
			{
				return new NumberTerm((t as NumberTerm).number * (t.next as NumberTerm).number, null);
			}));
			Register(new Rule("/".GetHashCode(), 2, t =>
			{
				return new NumberTerm((t as NumberTerm).number / (t.next as NumberTerm).number, null);
			}));
			Register(new Rule("=".GetHashCode(), 2, t =>
			{
				Term left = t;
				Term right = t.next;
				if (left.Equals(right))
				{
					return new LiteralTerm("true", null).Quote();
				}
				else
				{
					return new ListTerm(null, null);
				}
			}));
			Register(new Rule("!=".GetHashCode(), 2, t =>
			{
				Term left = t;
				Term right = t.next;
				if (left.Equals(right))
				{
					return new ListTerm(null, null);
				}
				else
				{
					return new LiteralTerm("true", null).Quote();
				}
			}));
			Register(new Rule(">".GetHashCode(), 2, t =>
			{
				NumberTerm left = t as NumberTerm;
				NumberTerm right = t.next as NumberTerm;
				if (left.number > right.number)
				{
					return new LiteralTerm("true", null).Quote();
				}
				else
				{
					return new ListTerm(null, null);
				}
			}));
			Register(new Rule("<".GetHashCode(), 2, t =>
			{
				NumberTerm left = t as NumberTerm;
				NumberTerm right = t.next as NumberTerm;
				if (left.number < right.number)
				{
					return new LiteralTerm("true", null).Quote();
				}
				else
				{
					return new ListTerm(null, null);
				}
			}));
			Register(new Rule(">=".GetHashCode(), 2, t =>
			{
				NumberTerm left = t as NumberTerm;
				NumberTerm right = t.next as NumberTerm;
				if (left.number >= right.number)
				{
					return new LiteralTerm("true", null).Quote();
				}
				else
				{
					return new ListTerm(null, null);
				}
			}));
			Register(new Rule("<=".GetHashCode(), 2, t =>
			{
				NumberTerm left = t as NumberTerm;
				NumberTerm right = t.next as NumberTerm;
				if (left.number <= right.number)
				{
					return new LiteralTerm("true", null).Quote();
				}
				else
				{
					return new ListTerm(null, null);
				}
			}));
			Register(new Rule("cat".GetHashCode(), 2, t =>
			{
				ListTerm left = t as ListTerm;
				ListTerm right = t.next as ListTerm;
				if (left.head == null)
				{
					right.next = null;
					return right;
				}
				else if (right.head == null)
				{
					left.next = null;
					return left;
				}
				Term last = null;
				for (Term it = left.head; it != null; it = it.next)
				{
					last = it;
				}
				last.next = right.head;
				left.next = null;
				return left;
			}));
			Register(new Rule("head".GetHashCode(), 1, t =>
			{
				ListTerm list = t as ListTerm;
				if (list.head == null)
				{
					return new ListTerm(null, list.next);
				}
				Term tail = list.head.next;
				Term head = list.head;
				list.head = tail;
				head.next = list;
				list.next = null;
				return head;
			}));
			Register(new Rule("unquote".GetHashCode(), 1, t =>
			{
				ListTerm list = t as ListTerm;
				return list.head;
			}));
		}

		public void Register(Rule rule)
		{
			rules.Add(rule.name, rule);
		}

		public Rule Get(int name)
		{
			Rule rule;
			rules.TryGetValue(name, out rule);
			return rule;
		}
	}
}
