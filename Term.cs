using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mantra
{
	public abstract class Term
	{
		public ListTerm Quote()
		{
			var list = new List<Term>();
			list.Add(this);
			return new ListTerm(list); ;
		}

		public abstract Term Copy();
	}

	public class ListTerm : Term
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
			ListTerm o = obj as ListTerm;
			if (o == null) return false;
			if (o.terms.Count != terms.Count) return false;
			for (int i = 0; i < terms.Count; ++i)
			{
				if (terms[i] != o.terms[i]) return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			return terms.GetHashCode();
		}

		public override Term Copy()
		{
			return new ListTerm(terms.ToList());
		}
	}

	public class NumberTerm : Term
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
			NumberTerm o = obj as NumberTerm;
			if (o == null) return false;
			return o.number == number;
		}

		public override int GetHashCode()
		{
			return number.GetHashCode();
		}

		public override Term Copy()
		{
			return new NumberTerm(number);
		}
	}

	public class LiteralTerm : Term
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
			LiteralTerm o = obj as LiteralTerm;
			if (o == null) return false;
			return o.name == name;
		}

		public override int GetHashCode()
		{
			return name.GetHashCode();
		}

		public override Term Copy()
		{
			return new LiteralTerm(name);
		}
	}
}
