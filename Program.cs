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

		static void Main(string[] args)
		{
			ReceiverPool pool = new ReceiverPool();
			Fiber repl = new Fiber("repl");
			RuleSet rules = new RuleSet(pool);
			new Parser().ParseFile(File.ReadAllText("prelude.tra"), rules);

			while (true)
			{
				Console.Write(":> ");
				string input = Console.ReadLine();
				if (input.Length > 0 && input[0] == '#')
				{
					Command(input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), pool, rules);
				}
				repl.Head = new Parser().ParseExpression(input);
				do
				{
					Console.WriteLine(repl.Head);
				} while (repl.PerformStep(rules) == Fiber.Status.Active);
				Console.WriteLine();
			}
		}

		private static void Command(string[] p, ReceiverPool pool, RuleSet rules)
		{
			if (p[0] == "#load")
			{
				try
				{
					new Parser().ParseFile(File.ReadAllText(p[1]), rules);
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
