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

			while (true)
			{
				Console.Write(":> ");
				string input = Console.ReadLine();
				if (input.Length > "load ".Length && input.Substring(0, "load ".Length) == "load ")
				{
					new Parser().ParseFile(File.ReadAllText(input.Substring("load ".Length)), rules);
				}
				repl.Head = new Parser().ParseExpression(input);
				repl.Evaluate(rules);
				Console.WriteLine(repl.Head);
			}
		}
	}
}
