using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mantra
{
	public abstract class Term
	{
		public Term next;

		public Term(Term next)
		{
			this.next = next;
		}

		public ListTerm Quote()
		{
			return new ListTerm(this, next);
		}

		public bool ChainEquals(Term o)
		{
			if ((next == null && o.next != null) ||
				(next != null && o.next == null))
			{
				return false;
			}
			if (next == null && o.next == null)
			{
				return Equals(o);
			}
			return Equals(o) && next.ChainEquals(o.next);
		}

		public abstract Term CopySingle();
		public Term CopyChain()
		{
			Term copy = CopySingle();
			if (next == null)
			{
				return copy;
			}
			copy.next = next.CopyChain();
			return copy;
		}

		public int Count
		{
			get
			{
				if (next == null) return 1;
				return 1 + next.Count;
			}
		}
	}

	public class ListTerm : Term
	{
		public Term head;

		public ListTerm(Term head, Term next)
			: base(next)
		{
			this.head = head;
		}

		public override string ToString()
		{
			if (next == null)
			{
				return "[" + head + "]";
			}
			else
			{
				return "[" + head + "] " + next;
			}
		}

		public override bool Equals(object obj)
		{
			ListTerm o = obj as ListTerm;
			if (o == null) return false;
			return head.ChainEquals(o.head);
		}

		public override Term CopySingle()
		{
			if (head == null)
			{
				return new ListTerm(null, null);
			}
			return new ListTerm(head.CopyChain(), null);
		}
	}

	public class NumberTerm : Term
	{
		public double number;

		public NumberTerm(double number, Term next)
			: base(next)
		{
			this.number = number;
		}

		public override string ToString()
		{
			if (next == null)
			{
				return number.ToString();
			}
			else
			{
				return number + " " + next;
			}
		}

		public override bool Equals(object obj)
		{
			NumberTerm o = obj as NumberTerm;
			if (o == null) return false;
			return o.number == number;
		}

		public override Term CopySingle()
		{
			return new NumberTerm(number, null);
		}
	}

	public class LiteralTerm : Term
	{
		public int name;

		public LiteralTerm(string name, Term next)
			: base(next)
		{
			this.name = name.GetHashCode();
			if (!Program.literalDictionary.ContainsKey(name.GetHashCode()))
				Program.literalDictionary.Add(name.GetHashCode(), name);
		}

		public override string ToString()
		{
			if (Program.literalDictionary.ContainsKey(name))
			{
				if (next == null)
				{
					return Program.literalDictionary[name];
				}
				else
				{
					return Program.literalDictionary[name] + " " + next;
				}
			}
			else
			{
				if (next == null)
				{
					return "<noname>";
				}
				else
				{
					return "<noname> " + next;
				}
			}
		}

		public override bool Equals(object obj)
		{
			LiteralTerm o = obj as LiteralTerm;
			if (o == null) return false;
			return o.name == name;
		}

		public override Term CopySingle()
		{
			return new LiteralTerm(Program.literalDictionary[name], null);
		}
	}
}
