using Microsoft.FSharp.Collections;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

		public IEnumerable<Rule> Rules { get { return rules.Values; } }

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

		public static void InitializeCore(FiberPool pool, RuleSet rules)
		{
			Core = new Module("Core");
			Core.Register(new Rule("+".GetHashCode(), 2, t =>
			{
				var left = (NumberTerm)t[0];
				var right = (NumberTerm)t[1];
				return new Term[] { new NumberTerm(left.number + right.number) };
			}));
			Core.Register(new Rule("-".GetHashCode(), 2, t =>
			{
				var left = (NumberTerm)t[0];
				var right = (NumberTerm)t[1];
				return new Term[] { new NumberTerm(left.number - right.number) };
			}));
			Core.Register(new Rule("*".GetHashCode(), 2, t =>
			{
				var left = (NumberTerm)t[0];
				var right = (NumberTerm)t[1];
				return new Term[] { new NumberTerm(left.number * right.number) };
			}));
			Core.Register(new Rule("/".GetHashCode(), 2, t =>
			{
				var left = (NumberTerm)t[0];
				var right = (NumberTerm)t[1];
				return new Term[] { new NumberTerm(left.number / right.number) };
			}));
			Core.Register(new Rule("=".GetHashCode(), 2, t =>
			{
				Term left = t[0];
				Term right = t[1];
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
				Term left = t[0];
				Term right = t[1];
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
				var left = (NumberTerm)t[0];
				var right = (NumberTerm)t[1];
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
				var left = (NumberTerm)t[0];
				var right = (NumberTerm)t[1];
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
				var left = (NumberTerm)t[0];
				var right = (NumberTerm)t[1];
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
				ListTerm left = t[0] as ListTerm;
				if (left == null)
				{
					left = new ListTerm(new[] { t[0] });
				}
				ListTerm right = t[1] as ListTerm;
				if (right == null)
				{
					right = new ListTerm(new[] { t[1] });
				}

				if (right.terms.Count == 0)
				{
					return new[] { left };
				}
				else if (right.terms.Count == 1)
				{
					return new[] { new ListTerm(left.terms.With(right.terms[0])) };
				}
				else
				{
					return new[] { new ListTerm(left.terms.WithRange(right.terms)) };
				}
			}));
			/*Core.Register(new Rule("cons".GetHashCode(), 2, t =>
			{
				Term left = t[0];
				ListTerm right = (ListTerm)t[1];
				return new Term[] { new ListTerm(right.terms.Insert(0, left)) };
			}));*/
			Core.Register(new Rule("unquote".GetHashCode(), 1, t =>
			{
				ListTerm list = (ListTerm)t[0];
				return list.terms;
			}));
			Core.Register(new Rule("pass".GetHashCode(), 2, t =>
			{
				ListTerm name = (ListTerm)t[0];
				ListTerm message = (ListTerm)t[1];
				pool.Send(((LiteralTerm)name.terms.First()).name, message.terms);
				return new Term[] { };
			}));
			Core.Register(new Rule("trace".GetHashCode(), 1, t =>
			{
				Console.WriteLine(t[0]);
				return new Term[] { };
			}));
			Core.Register(new Rule("showFiber".GetHashCode(), 1, t =>
			{
				ListTerm name = (ListTerm)t[0];
				return new Term[] { new ListTerm((pool.Receiver[((LiteralTerm)name.terms.First()).name] as Fiber).Terms.ToList()) };
			}));

			Core.Register(new Rule("do".GetHashCode(), 1, t =>
			{
				Fiber fiber = new Fiber("temp");
				fiber.Terms.Clear();
				fiber.Receive(((ListTerm)t[0]).terms);
				fiber.Evaluate(rules, false);
				return new Term[] { new ListTerm(fiber.Terms) };
			}));
		}
	}
}
