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
		public readonly Func<IEnumerable<Term>, IEnumerable<Term>> hardCoded;
		public readonly List<IEnumerable<Term>> bodyHeads;
		public readonly List<IEnumerable<Term>> patternHeads;
		public readonly int nArgs;

		public Rule(int name, int nArgs, Func<IEnumerable<Term>, IEnumerable<Term>> hardCoded)
		{
			this.name = name;
			this.hardCoded = hardCoded;
			this.nArgs = nArgs;
		}

		public Rule(int name)
		{
			this.name = name;
			this.bodyHeads = new List<IEnumerable<Term>>();
			this.patternHeads = new List<IEnumerable<Term>>();
		}
	}
}
