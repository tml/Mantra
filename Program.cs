using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mantra
{
	class Program
	{
		public static Dictionary<int, string> literalDictionary = new Dictionary<int, string>();
		private static bool steps = false;

		static void Main(string[] args)
		{
			ReceiverPool pool = new ReceiverPool();
			Fiber repl = new Fiber("repl");
			RuleSet rules = new RuleSet();
			Module.InitializeCore(pool, rules);
			rules.Register(Module.Core);
			new Parser().ParseFile("prelude.tra", rules);

			IEnumerable<Term> lastResult = new Term[] { };

			while (true)
			{
				Console.Write(":> ");
				string input = Console.ReadLine();
				if (input.Length > 0 && input[0] == '#')
				{
					Command(input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), pool, rules);
				}
				repl.Terms = new Parser().ParseExpression(input);
				for (int i = 0; i < repl.Terms.Count; ++i)
				{
					if (repl.Terms[i] is LiteralTerm && ((LiteralTerm)repl.Terms[i]).name == "answer".GetHashCode())
					{
						repl.Terms.RemoveAt(i);
						repl.Terms.InsertRange(i, lastResult.ToList());
						i += lastResult.Count();
					}
				}
				if (steps)
				{
					do
					{
						Console.WriteLine(repl);
					} while (repl.PerformStep(rules) == Fiber.Status.Active);
				}
				else
				{
					repl.Evaluate(rules, false);
					//Console.WriteLine(repl);
				}
				lastResult = repl.Terms;
				Console.WriteLine();
			}
		}

		private static void Command(string[] p, ReceiverPool pool, RuleSet rules)
		{
			if (p[0] == "#load")
			{
				try
				{
					new Parser().ParseFile(p[1], rules);
				}
				catch
				{
					Console.WriteLine("Can't load that file.");
				}
			}
			else if (p[0] == "#extend")
			{
				if (Path.HasExtension(p[1]))
				{
					LoadExtension(p[1], pool, rules);
				}
				else
				{
					LoadExtension(p[1] + ".dll", pool, rules);
				}
			}
			else if (p[0] == "#steps")
			{
				steps = !steps;
			}
		}

		static void LoadExtension(string path, ReceiverPool pool, RuleSet rules)
		{
			Thread thread = new Thread(() =>
			{
				try
				{
					Assembly assembly = Assembly.LoadFile(path);
					Type extension = assembly.GetType("Mandala.Extension");
					extension.GetMethod("Extend").Invoke(Activator.CreateInstance(extension), new object[] { pool, rules, true });
				}
				catch
				{
					Console.WriteLine("Can't load that extension.");
				}
			});
			thread.Start();
		}
	}
}
