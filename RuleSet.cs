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
		}

		public void Load(string name)
		{
			loaded[name] = true;
		}

		public Rule Get(int name)
		{
			foreach (var module in modules.Values.Where(m => loaded[m.Name]))
			{
				Rule rule = module.Get(name);
				if (rule != null)
				{
					return rule;
				}
			}
			return null;
		}
	}
}
