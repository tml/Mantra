using Microsoft.FSharp.Collections;
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
				var left = (NumberTerm)t.First();
				var right = (NumberTerm)t.Skip(1).First();
				return new Term[] { new NumberTerm(left.number + right.number) };
			}));
			Core.Register(new Rule("-".GetHashCode(), 2, t =>
			{
				var left = (NumberTerm)t.First();
				var right = (NumberTerm)t.Skip(1).First();
				return new Term[] { new NumberTerm(left.number - right.number) };
			}));
			Core.Register(new Rule("*".GetHashCode(), 2, t =>
			{
				var left = (NumberTerm)t.First();
				var right = (NumberTerm)t.Skip(1).First();
				return new Term[] { new NumberTerm(left.number * right.number) };
			}));
			Core.Register(new Rule("/".GetHashCode(), 2, t =>
			{
				var left = (NumberTerm)t.First();
				var right = (NumberTerm)t.Skip(1).First();
				return new Term[] { new NumberTerm(left.number / right.number) };
			}));
			Core.Register(new Rule("=".GetHashCode(), 2, t =>
			{
				Term left = t.First();
				Term right = t.Skip(1).First();
				if (left.Equals(right))
				{
					return new Term[] { new LiteralTerm("true").Quote() };
				}
				else
				{
					return new Term[] { new ListTerm(new Term[] { }) };
				}
			}));
			Core.Register(new Rule("!=".GetHashCode(), 2, t =>
			{
				Term left = t.First();
				Term right = t.Skip(1).First();
				if (left.Equals(right))
				{
					return new Term[] { new ListTerm(new Term[] { }) };
				}
				else
				{
					return new Term[] { new LiteralTerm("true").Quote() };
				}
			}));
			Core.Register(new Rule(">".GetHashCode(), 2, t =>
			{
				var left = (NumberTerm)t.First();
				var right = (NumberTerm)t.Skip(1).First();
				if (left.number > right.number)
				{
					return new Term[] { new LiteralTerm("true").Quote() };
				}
				else
				{
					return new Term[] { new ListTerm(new Term[] { }) };
				}
			}));
			Core.Register(new Rule("<".GetHashCode(), 2, t =>
			{
				var left = (NumberTerm)t.First();
				var right = (NumberTerm)t.Skip(1).First();
				if (left.number < right.number)
				{
					return new Term[] { new LiteralTerm("true").Quote() };
				}
				else
				{
					return new Term[] { new ListTerm(new Term[] { }) };
				}
			}));
			Core.Register(new Rule(">=".GetHashCode(), 2, t =>
			{
				var left = (NumberTerm)t.First();
				var right = (NumberTerm)t.Skip(1).First();
				if (left.number >= right.number)
				{
					return new Term[] { new LiteralTerm("true").Quote() };
				}
				else
				{
					return new Term[] { new ListTerm(new Term[] { }) };
				}
			}));
			Core.Register(new Rule("<=".GetHashCode(), 2, t =>
			{
				var left = (NumberTerm)t.First();
				var right = (NumberTerm)t.Skip(1).First();
				if (left.number <= right.number)
				{
					return new Term[] { new LiteralTerm("true").Quote() };
				}
				else
				{
					return new Term[] { new ListTerm(new Term[] { }) };
				}
			}));
			Core.Register(new Rule("cat".GetHashCode(), 2, t =>
			{
				ListTerm left = (ListTerm)t.First();
				ListTerm right = (ListTerm)t.Skip(1).First();
				return new Term[] { new ListTerm(ListModule.Concat(new[] { left.terms, right.terms })) };
			}));
			Core.Register(new Rule("cons".GetHashCode(), 2, t =>
			{
				Term left = t.First();
				ListTerm right = (ListTerm)t.Skip(1).First();
				return new Term[] { new ListTerm(new FSharpList<Term>(left, right.terms)) };
			}));
			Core.Register(new Rule("unquote".GetHashCode(), 1, t =>
			{
				ListTerm list = (ListTerm)t.First();
				return list.terms;
			}));
			Core.Register(new Rule("pass".GetHashCode(), 2, t =>
			{
				ListTerm name = (ListTerm)t.First();
				ListTerm message = (ListTerm)t.Skip(1).First();
				pool.Send(((LiteralTerm)name.terms.First()).name, message.terms);
				return null;
			}));
			Core.Register(new Rule("trace".GetHashCode(), 1, t =>
			{
				Console.WriteLine(t);
				return null;
			}));
			Core.Register(new Rule("showFiber".GetHashCode(), 1, t =>
			{
				ListTerm name = (ListTerm)t.First();
				return new Term[] { new ListTerm((pool.Receiver[((LiteralTerm)name.terms.First()).name] as Fiber).Terms.ToList()) };
			}));

			Core.Register(new Rule("do".GetHashCode(), 1, t =>
			{
				Fiber fiber = new Fiber("temp");
				fiber.Terms = ((ListTerm)t.First()).terms.ToList();
				fiber.Evaluate(rules, false);
				return new Term[] { new ListTerm(fiber.Terms) };
			}));
		}
	}
}
