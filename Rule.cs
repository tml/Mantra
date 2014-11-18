using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mantra
{
	class Rule
	{
		public readonly int name;
		public readonly Func<Term, Term> hardCoded;
		public readonly List<Term> bodyHeads;
		public readonly List<Term> patternHeads;
		public readonly int nArgs;

		public Rule(int name, int nArgs, Func<Term, Term> hardCoded)
		{
			this.name = name;
			this.hardCoded = hardCoded;
			this.nArgs = nArgs;
		}

		public Rule(int name)
		{
			this.name = name;
			this.bodyHeads = new List<Term>();
			this.patternHeads = new List<Term>();
		}
	}
}
