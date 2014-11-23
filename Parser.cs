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
			Module module = new Module(Path.GetDirectoryName(path));
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
			Term term = ParseLiteral(text);
			if (term is NumberTerm)
			{
				throw new Exception("Numbers can't be rule specifiers.");
			}
			LiteralTerm name = term as LiteralTerm;

			Term patternHead = ParseTerm(text);
			Term current = patternHead;
			if (patternHead is LiteralTerm && (patternHead as LiteralTerm).name == "=>".GetHashCode())
			{
				patternHead = null;
				current = null;
			}
			else
			{
				while (i < text.Length)
				{
					current.next = ParseTerm(text);
					if (current.next is LiteralTerm && (current.next as LiteralTerm).name == "=>".GetHashCode())
					{
						current.next = null;
						break;
					}
					current = current.next;
				}
			}

			int last = text.IndexOf(';', i);
			if (last == -1)
			{
				Console.WriteLine("Missing semicolon for rule '" + Program.literalDictionary[name.name] + "'.");
				return;
			}
			Term bodyHead = new Parser().ParseExpression(text.Substring(i, last - i));
			i = last + 1;
			SkipWhitespace(text);

			Rule rule = module.Get(name.name);
			if (rule == null)
			{
				rule = new Rule(name.name);
				module.Register(rule);
			}
			rule.patternHeads.Add(patternHead);
			rule.bodyHeads.Add(bodyHead);
		}

		public Term ParseExpression(string text)
		{
			SkipWhitespace(text);
			if (i >= text.Length) return null;

			Term head = ParseTerm(text);
			Term current = head;
			while (i < text.Length)
			{
				current.next = ParseTerm(text);
				current = current.next;
			}
			return head;
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
			if (text[i] == '(')
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
			string word = ParseWord(text);
			double number;
			if (double.TryParse(word.ToString(), out number))
			{
				return new NumberTerm(number, null);
			}
			return new LiteralTerm(word.ToString(), null);
		}

		private string ParseWord(string text)
		{
			StringBuilder builder = new StringBuilder();
			while (i < text.Length && !char.IsWhiteSpace(text[i]) && text[i] != '(' && text[i] != ')')
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
			if (i >= text.Length || text[i] == ')')
			{
				i += 1;
				SkipWhitespace(text);
				return new ListTerm(null, null);
			}
			Term head = ParseTerm(text);
			Term current = head;
			while (i < text.Length && text[i] != ')')
			{
				current.next = ParseTerm(text);
				current = current.next;
			}
			i += 1;
			SkipWhitespace(text);
			return new ListTerm(head, null);
		}
	}
}
