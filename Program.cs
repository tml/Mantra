using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mantra
{
	class Program
	{
		public static Dictionary<int, string> literalDictionary = new Dictionary<int, string>();

		static void Main(string[] args)
		{
			Fiber repl = new Fiber();
			RuleSet rules = new RuleSet();
			new Parser().ParseFile(File.ReadAllText("prelude.tra"), rules);

			while (true)
			{
				Console.Write(":> ");
				string input = Console.ReadLine();
				if (input.Length > "load ".Length && input.Substring(0, "load ".Length) == "load ")
				{
					try
					{
						new Parser().ParseFile(File.ReadAllText(input.Substring("load ".Length)), rules);
					}
					catch
					{
						Console.WriteLine("Can't load that file.");
						continue;
					}
				}
				repl.Head = new Parser().ParseExpression(input);
				while (repl.PerformStep(rules) == Fiber.Status.Active)
				{
					Console.WriteLine(repl.Head);
				}
				Console.WriteLine();
			}
		}
	}
}
