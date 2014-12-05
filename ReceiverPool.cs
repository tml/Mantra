using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mantra
{
	public class ReceiverPool
	{
		private Dictionary<int, IReceiver> receivers = new Dictionary<int, IReceiver>();
		public IReadOnlyDictionary<int, IReceiver> Receiver { get { return receivers; } }

		public void Add(IReceiver receiver)
		{
			receivers.Add(receiver.Name, receiver);
		}

		public void Send(int name, IEnumerable<Term> term)
		{
			receivers[name].Receive(term);
		}
	}
}
