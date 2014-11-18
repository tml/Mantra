using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mantra
{
	public class Parser
	{
		private int i;

		public void ParseFile(string text, RuleSet rules)
		{
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
				ParseDeclaration(text, rules);
			}
		}

		public void ParseDeclaration(string text, RuleSet rules)
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

			Rule rule = rules.Get(name.name);
			if (rule == null)
			{
				rule = new Rule(name.name);
				rules.Register(rule);
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
			StringBuilder builder = new StringBuilder();
			while (i < text.Length && !char.IsWhiteSpace(text[i]) && text[i] != '(' && text[i] != ')')
			{
				builder.Append(text[i]);
				i += 1;
			}
			SkipWhitespace(text);
			double number;
			if (double.TryParse(builder.ToString(), out number))
			{
				return new NumberTerm(number, null);
			}
			return new LiteralTerm(builder.ToString(), null);
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
