using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mantra
{
	public class RuleSet
	{
		private Dictionary<string, Module> modules = new Dictionary<string, Module>();
		private Dictionary<string, bool> loaded = new Dictionary<string, bool>();

		private Dictionary<int, Rule> cache = new Dictionary<int, Rule>();

		public void Register(Module module, bool loaded = true)
		{
			if (modules.ContainsKey(module.Name))
			{
				Console.WriteLine("Reloading module " + module.Name);
				modules.Remove(module.Name);
				this.loaded.Remove(module.Name);
			}
			modules.Add(module.Name, module);
			this.loaded.Add(module.Name, loaded);
			cache = new Dictionary<int, Rule>();
		}

		public void Load(string name)
		{
			loaded[name] = true;
			cache = new Dictionary<int, Rule>();
		}

		public Rule Get(int name)
		{
			Rule rule;
			cache.TryGetValue(name, out rule);
			if (rule != null) return rule;
			foreach (var module in modules.Values.Where(m => loaded[m.Name]))
			{
				rule = module.Get(name);
				if (rule != null)
				{
					cache.Add(name, rule);
					return rule;
				}
			}
			cache.Add(name, null);
			return null;
		}
	}
}
