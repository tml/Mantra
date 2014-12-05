using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mantra
{
	public class Module
	{
		private Dictionary<int, Rule> rules = new Dictionary<int, Rule>();
		public string Name { get; private set; }

		public static Module Core { get; private set; }

		public void Register(Rule rule)
		{
			rules.Add(rule.name, rule);
		}

		public Module(string name)
		{
			Name = name;
		}

		public Rule Get(int name)
		{
			Rule rule;
			rules.TryGetValue(name, out rule);
			return rule;
		}

		public static void InitializeCore(ReceiverPool pool, RuleSet rules)
		{
			Core = new Module("Core");
			Core.Register(new Rule("+".GetHashCode(), 2, t =>
			{
				return new NumberTerm((t as NumberTerm).number + (t.next as NumberTerm).number, null);
			}));
			Core.Register(new Rule("-".GetHashCode(), 2, t =>
			{
				return new NumberTerm((t as NumberTerm).number - (t.next as NumberTerm).number, null);
			}));
			Core.Register(new Rule("*".GetHashCode(), 2, t =>
			{
				return new NumberTerm((t as NumberTerm).number * (t.next as NumberTerm).number, null);
			}));
			Core.Register(new Rule("/".GetHashCode(), 2, t =>
			{
				return new NumberTerm((t as NumberTerm).number / (t.next as NumberTerm).number, null);
			}));
			Core.Register(new Rule("=".GetHashCode(), 2, t =>
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
			Core.Register(new Rule("!=".GetHashCode(), 2, t =>
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
			Core.Register(new Rule(">".GetHashCode(), 2, t =>
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
			Core.Register(new Rule("<".GetHashCode(), 2, t =>
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
			Core.Register(new Rule(">=".GetHashCode(), 2, t =>
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
			Core.Register(new Rule("<=".GetHashCode(), 2, t =>
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
			Core.Register(new Rule("cat".GetHashCode(), 2, t =>
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
			Core.Register(new Rule("unquote".GetHashCode(), 1, t =>
			{
				ListTerm list = t as ListTerm;
				return list.head;
			}));
			Core.Register(new Rule("cons".GetHashCode(), 2, t =>
			{
				Term a = t.CopySingle();
				ListTerm list = t.next as ListTerm;
				a.next = list.head;
				return new ListTerm(a, null);
			}));
			Core.Register(new Rule("pass".GetHashCode(), 2, t =>
			{
				ListTerm name = t.CopySingle() as ListTerm;
				ListTerm message = t.next.CopySingle() as ListTerm;
				pool.Send((name.head as LiteralTerm).name, message.head);
				return null;
			}));
			Core.Register(new Rule("trace".GetHashCode(), 1, t =>
			{
				Console.WriteLine(t);
				return null;
			}));
			Core.Register(new Rule("showFiber".GetHashCode(), 1, t =>
			{
				ListTerm name = t.CopySingle() as ListTerm;
				return new ListTerm((pool.Receiver[(name.head as LiteralTerm).name] as Fiber).Head.CopyChain(), null);
			}));

			Core.Register(new Rule("do".GetHashCode(), 1, t =>
			{
				Fiber fiber = new Fiber("temp");
				fiber.Head = (t as ListTerm).head.CopyChain();
				fiber.Evaluate(rules, false);
				return new ListTerm(fiber.Head, null);
			}));
		}
	}
}
