using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mantra
{
	public class FiberPool
	{
		private class Worker
		{
			private ConcurrentBag<Fiber> fibers = new ConcurrentBag<Fiber>();

			public void Add(Fiber fiber)
			{
				fibers.Add(fiber);
			}

			public void Dirty(RuleSet rules)
			{
				Thread thread = new Thread((object r) =>
				{
					RuleSet rs = r as RuleSet;
					foreach (var fiber in fibers)
					{
						fiber.Evaluate(rs, false);
					}
				});
				thread.Start(rules);
			}
		}

		private int nextWorker = 0;
		private Worker[] workers;
		private Dictionary<int, Worker> workerMap = new Dictionary<int, Worker>();
		private Dictionary<int, Fiber> receivers = new Dictionary<int, Fiber>();
		private RuleSet rules;
		public IReadOnlyDictionary<int, Fiber> Receiver { get { return receivers; } }

		public FiberPool(RuleSet rules, int numWorkers = 2)
		{
			this.rules = rules;
			workers = new Worker[numWorkers];
			for (int i = 0; i < numWorkers; ++i)
			{
				workers[i] = new Worker();
			}
		}

		public void Add(Fiber receiver)
		{
			workers[nextWorker].Add(receiver);
			workerMap[receiver.Name] = workers[nextWorker];
			nextWorker += 1;
			if (nextWorker >= workers.Length)
			{
				nextWorker = 0;
			}

			receivers.Add(receiver.Name, receiver);
		}

		public void Send(int name, IEnumerable<Term> term)
		{
			if (!receivers.ContainsKey(name))
			{
				Add(new Fiber(Program.literalDictionary[name]));
			}
			receivers[name].Receive(term);
			workerMap[name].Dirty(rules);
		}
	}
}
