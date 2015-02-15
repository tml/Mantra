using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mantra
{
	public class Parser
	{
		private int i;

		public void ParseFile(string path, RuleSet rules)
		{
			string text = File.ReadAllText(path);
			SkipWhitespace(text);
			string version = ParseWord(text);
			if (version != "version")
			{
				Console.WriteLine("This file doesn't start with version. It should start with \"version 0\" to support this compiler (" + path + ").");
				return;
			}
			string number = ParseWord(text);
			int versionValue;
			if (!int.TryParse(number, out versionValue))
			{
				Console.WriteLine("The version number at the start of the file isn't formatted as a number (" + path + ").");
				return;
			}
			if (versionValue != 0)
			{
				Console.WriteLine("The version of this file is " + versionValue + ", but this compiler only supports major version zero (" + path + ").");
				return;
			}
			Module module = new Module(Path.GetFileNameWithoutExtension(path));
			while (i < text.Length)
			{
				if (text[i] == '#')
				{
					while (i < text.Length && text[i] != '\n')
					{
						i += 1;
					}
					SkipWhitespace(text);
					continue;
				}
				ParseDeclaration(text, module);
			}
			rules.Register(module);
		}

		public void ParseDeclaration(string text, Module module)
		{
			Term nameTerm = ParseLiteral(text);
			if (nameTerm is NumberTerm)
			{
				throw new Exception("Numbers can't be rule specifiers.");
			}
			LiteralTerm name = (LiteralTerm)nameTerm;

			var pattern = new List<Term>();
			while (i < text.Length)
			{
				Term t = ParseTerm(text);
				if (t is LiteralTerm && ((LiteralTerm)t).name == "=>".GetHashCode())
				{
					break;
				}
				pattern.Add(t);
			}

			int last = text.IndexOf(';', i);
			if (last == -1)
			{
				Console.WriteLine("Missing semicolon for rule '" + Program.literalDictionary[name.name] + "'.");
				return;
			}
			List<Term> body = new Parser().ParseExpression(text.Substring(i, last - i));
			i = last + 1;
			SkipWhitespace(text);

			Rule rule = module.Get(name.name);
			if (rule == null)
			{
				rule = new Rule(name.name);
				module.Register(rule);
			}
			rule.patternHeads.Add(pattern);
			rule.bodyHeads.Add(body);
		}

		public List<Term> ParseExpression(string text)
		{
			SkipWhitespace(text);
			if (i >= text.Length) return new List<Term>();

			var list = new List<Term>(64);
			while (i < text.Length)
			{
				list.Add(ParseTerm(text));
			}
			return list;
		}

		private void SkipWhitespace(string text)
		{
			while (i < text.Length && char.IsWhiteSpace(text[i]))
			{
				i += 1;
			}
		}

		private Term ParseTerm(string text)
		{
			if (text[i] == '[')
			{
				return ParseList(text);
			}
			else
			{
				return ParseLiteral(text);
			}
		}

		private Term ParseLiteral(string text)
		{
			if (text[i] == '"')
			{
				return ParseString(text);
			}
			string word = ParseWord(text);
			decimal number;
			if (decimal.TryParse(word.ToString(), out number))
			{
				return new NumberTerm(number);
			}
			return new LiteralTerm(word.ToString());
		}

		private Term ParseString(string text)
		{
			i += 1;
			List<Term> characters = new List<Term>();
			while (i < text.Length && text[i] != '"')
			{
				characters.Add(new LiteralTerm(text[i].ToString()));
				i += 1;
			}
			i += 1;
			return new ListTerm(characters);
		}

		private string ParseWord(string text)
		{
			StringBuilder builder = new StringBuilder();
			while (i < text.Length && !char.IsWhiteSpace(text[i]) && text[i] != '[' && text[i] != ']')
			{
				builder.Append(text[i]);
				i += 1;
			}
			SkipWhitespace(text);
			return builder.ToString();
		}

		private Term ParseList(string text)
		{
			i += 1;
			SkipWhitespace(text);
			if (i >= text.Length || text[i] == ']')
			{
				i += 1;
				SkipWhitespace(text);
				return new ListTerm(new Term[] { });
			}
			var list = new List<Term>();
			while (i < text.Length && text[i] != ']')
			{
				list.Add(ParseTerm(text));
			}
			i += 1;
			SkipWhitespace(text);
			return new ListTerm(list);
		}
	}
}
