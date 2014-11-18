using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mantra
{
	public interface IReceiver
	{
		int Name { get; }
		void Receive(Term message);
	}
}
