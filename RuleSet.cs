using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mantra
{
	public class RuleSet
	{
		private Dictionary<int, Rule> rules = new Dictionary<int, Rule>();

		public RuleSet(ReceiverPool pool)
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
					return right.CopySingle();
				}
				else if (right.head == null)
				{
					return left.CopySingle();
				}
				Term last = null;
				for (Term it = left.head; it != null; it = it.next)
				{
					last = it;
				}
				last.next = right.head;
				return new ListTerm(left.head, null);
			}));
			Register(new Rule("unquote".GetHashCode(), 1, t =>
			{
				ListTerm list = t as ListTerm;
				return list.head;
			}));
			Register(new Rule("cons".GetHashCode(), 2, t =>
			{
				Term a = t.CopySingle();
				ListTerm list = t.next as ListTerm;
				a.next = list.head;
				return new ListTerm(a, null);
			}));
			Register(new Rule("pass".GetHashCode(), 2, t =>
			{
				ListTerm name = t.CopySingle() as ListTerm;
				ListTerm message = t.next.CopySingle() as ListTerm;
				pool.Send((name.head as LiteralTerm).name, message.head);
				return null;
			}));
			Register(new Rule("trace".GetHashCode(), 1, t =>
			{
				Console.WriteLine(t);
				return null;
			}));
			Register(new Rule("showFiber".GetHashCode(), 1, t =>
			{
				ListTerm name = t.CopySingle() as ListTerm;
				return new ListTerm((pool.Receiver[(name.head as LiteralTerm).name] as Fiber).Head.CopyChain(), null);
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
