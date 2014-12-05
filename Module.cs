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
				var left = (t.First() as NumberTerm);
				var right = (t.Skip(1).First() as NumberTerm);
				return new[] { new NumberTerm(left.number + right.number) };
			}));
			Core.Register(new Rule("-".GetHashCode(), 2, t =>
			{
				var left = (t.First() as NumberTerm);
				var right = (t.Skip(1).First() as NumberTerm);
				return new[] { new NumberTerm(left.number - right.number) };
			}));
			Core.Register(new Rule("*".GetHashCode(), 2, t =>
			{
				var left = (t.First() as NumberTerm);
				var right = (t.Skip(1).First() as NumberTerm);
				return new[] { new NumberTerm(left.number * right.number) };
			}));
			Core.Register(new Rule("/".GetHashCode(), 2, t =>
			{
				var left = (t.First() as NumberTerm);
				var right = (t.Skip(1).First() as NumberTerm);
				return new[] { new NumberTerm(left.number / right.number) };
			}));
			Core.Register(new Rule("=".GetHashCode(), 2, t =>
			{
				Term left = t.First();
				Term right = t.Skip(1).First();
				if (left.Equals(right))
				{
					return new[] { new LiteralTerm("true").Quote() };
				}
				else
				{
					return new[] { new ListTerm(new Term[] { }) };
				}
			}));
			Core.Register(new Rule("!=".GetHashCode(), 2, t =>
			{
				Term left = t.First();
				Term right = t.Skip(1).First();
				if (left.Equals(right))
				{
					return new[] { new ListTerm(new Term[] { }) };
				}
				else
				{
					return new[] { new LiteralTerm("true").Quote() };
				}
			}));
			Core.Register(new Rule(">".GetHashCode(), 2, t =>
			{
				var left = (t.First() as NumberTerm);
				var right = (t.Skip(1).First() as NumberTerm);
				if (left.number > right.number)
				{
					return new[] { new LiteralTerm("true").Quote() };
				}
				else
				{
					return new[] { new ListTerm(new Term[] { }) };
				}
			}));
			Core.Register(new Rule("<".GetHashCode(), 2, t =>
			{
				var left = (t.First() as NumberTerm);
				var right = (t.Skip(1).First() as NumberTerm);
				if (left.number < right.number)
				{
					return new[] { new LiteralTerm("true").Quote() };
				}
				else
				{
					return new[] { new ListTerm(new Term[] { }) };
				}
			}));
			Core.Register(new Rule(">=".GetHashCode(), 2, t =>
			{
				var left = (t.First() as NumberTerm);
				var right = (t.Skip(1).First() as NumberTerm);
				if (left.number >= right.number)
				{
					return new[] { new LiteralTerm("true").Quote() };
				}
				else
				{
					return new[] { new ListTerm(new Term[] { }) };
				}
			}));
			Core.Register(new Rule("<=".GetHashCode(), 2, t =>
			{
				var left = (t.First() as NumberTerm);
				var right = (t.Skip(1).First() as NumberTerm);
				if (left.number <= right.number)
				{
					return new[] { new LiteralTerm("true").Quote() };
				}
				else
				{
					return new[] { new ListTerm(new Term[] { }) };
				}
			}));
			Core.Register(new Rule("cat".GetHashCode(), 2, t =>
			{
				ListTerm left = t.First() as ListTerm;
				ListTerm right = t.Skip(1).First() as ListTerm;
				if (left.terms.Count == 0)
				{
					return new[] { right };
				}
				else if (right.terms.Count == 0)
				{
					return new[] { left };
				}
				left.terms.AddRange(right.terms);
				return new[] { left };
			}));
			Core.Register(new Rule("unquote".GetHashCode(), 1, t =>
			{
				ListTerm list = (t.First()) as ListTerm;
				return list.terms;
			}));
			Core.Register(new Rule("pass".GetHashCode(), 2, t =>
			{
				ListTerm name = t.First() as ListTerm;
				ListTerm message = t.Skip(1).First() as ListTerm;
				pool.Send((name.terms.First() as LiteralTerm).name, message.terms);
				return null;
			}));
			Core.Register(new Rule("trace".GetHashCode(), 1, t =>
			{
				Console.WriteLine(t);
				return null;
			}));
			Core.Register(new Rule("showFiber".GetHashCode(), 1, t =>
			{
				ListTerm name = t.First() as ListTerm;
				return new[] { new ListTerm((pool.Receiver[(name.terms.First() as LiteralTerm).name] as Fiber).Terms.ToList()) };
			}));

			Core.Register(new Rule("do".GetHashCode(), 1, t =>
			{
				Fiber fiber = new Fiber("temp");
				fiber.Terms = (t.First() as ListTerm).terms.ToList();
				fiber.Evaluate(rules, false);
				return new[] { new ListTerm(fiber.Terms) };
			}));
		}
	}
}
