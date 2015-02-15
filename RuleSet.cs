using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mantra
{
	public class RuleSet
	{
		private SortedDictionary<string, Module> modules = new SortedDictionary<string, Module>();

		private Dictionary<int, Rule> cache = new Dictionary<int, Rule>();

		public void Register(Module module, bool loaded = true)
		{
			if (modules.ContainsKey(module.Name))
			{
				Console.WriteLine("Reloading module " + module.Name);
				modules.Remove(module.Name);
			}
			modules.Add(module.Name, module);
			cache = new Dictionary<int, Rule>();
		}

		public void Load(string name)
		{
			cache = new Dictionary<int, Rule>();
		}

		public Rule Get(int name)
		{
			Rule rule;
			cache.TryGetValue(name, out rule);
			if (rule != null) return rule;
			foreach (var module in modules.Values)
			{
				rule = module.Get(name);
				if (rule != null)
				{
					cache.Add(name, rule);
					return rule;
				}
			}
			cache[name] = null;
			return null;
		}
	}
}
