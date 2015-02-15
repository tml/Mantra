using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mantra
{
	public class Rule
	{
		public readonly int name;
		public readonly Func<Slice<Term>, IList<Term>> hardCoded;
		public readonly List<List<Term>> bodyHeads;
		public readonly List<List<Term>> patternHeads;
		public readonly int nArgs;

		public Rule(int name, int nArgs, Func<Slice<Term>, IList<Term>> hardCoded)
		{
			this.name = name;
			this.hardCoded = hardCoded;
			this.nArgs = nArgs;
		}

		public Rule(int name)
		{
			this.name = name;
			this.bodyHeads = new List<List<Term>>();
			this.patternHeads = new List<List<Term>>();
		}
	}
}
