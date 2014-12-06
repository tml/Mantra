using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mantra
{
	public interface Term
	{
		Term Copy();
	}

	public static class TermExtensions
	{
		public static ListTerm Quote(this Term self)
		{
			var list = new List<Term>();
			list.Add(self);
			return new ListTerm(list); ;
		}
	}

	public struct ListTerm : Term
	{
		public List<Term> terms;

		public ListTerm(List<Term> terms)
		{
			this.terms = terms;
		}

		public ListTerm(IEnumerable<Term> terms)
		{
			this.terms = terms.ToList();
		}

		public override string ToString()
		{
			return "[" + string.Join(" ", terms) + "]";
		}

		public override bool Equals(object obj)
		{
			if (!(obj is ListTerm)) return false;
			ListTerm o = (ListTerm)obj;
			if (o.terms.Count != terms.Count) return false;
			for (int i = 0; i < terms.Count; ++i)
			{
				if (!terms[i].Equals(o.terms[i])) return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			return terms.GetHashCode();
		}

		public Term Copy()
		{
			return new ListTerm(terms.ToList());
		}
	}

	public struct NumberTerm : Term
	{
		public double number;

		public NumberTerm(double number)
		{
			this.number = number;
		}

		public override string ToString()
		{
			return number.ToString();
		}

		public override bool Equals(object obj)
		{
			if (!(obj is NumberTerm)) return false;
			NumberTerm o = (NumberTerm)obj;
			return o.number == number;
		}

		public override int GetHashCode()
		{
			return number.GetHashCode();
		}

		public Term Copy()
		{
			return new NumberTerm(number);
		}
	}

	public struct LiteralTerm : Term
	{
		public int name;

		public LiteralTerm(string name)
		{
			this.name = name.GetHashCode();
			if (!Program.literalDictionary.ContainsKey(name.GetHashCode()))
				Program.literalDictionary.Add(name.GetHashCode(), name);
		}

		private LiteralTerm(int name)
		{
			this.name = name;
		}

		public override string ToString()
		{
			if (Program.literalDictionary.ContainsKey(name))
			{
				return Program.literalDictionary[name];
			}
			else
			{
				return "<noname>";
			}
		}

		public override bool Equals(object obj)
		{
			if (!(obj is LiteralTerm)) return false;
			LiteralTerm o = (LiteralTerm)obj;
			return o.name == name;
		}

		public override int GetHashCode()
		{
			return name.GetHashCode();
		}

		public Term Copy()
		{
			return new LiteralTerm(name);
		}
	}
}
