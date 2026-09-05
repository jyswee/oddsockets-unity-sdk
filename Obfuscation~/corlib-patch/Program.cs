using System;
using System.Linq;
using Mono.Cecil;

class Program
{
    static int Main(string[] args)
    {
        string input = args[0];
        string output = args[1];

        var mod = ModuleDefinition.ReadModule(input, new ReaderParameters { ReadWrite = false });

        // --- Restore anonymous-type constructor parameter names ---
        // Obfuscar strips ALL method parameter names (metadata-shrink pass) even
        // for skipped types. Newtonsoft matches an anonymous type's single
        // parameterized constructor to its properties BY NAME; empty param names
        // collide ("A member with the name '' already exists"). C# guarantees an
        // anon type's ctor params match its properties 1:1 in declaration order,
        // so copy the property names back onto the ctor parameters.
        int fixedParams = 0, fixedCtors = 0;
        foreach (var t in mod.Types)
        {
            if (t.Name == null || t.Name.IndexOf("AnonymousType", StringComparison.Ordinal) < 0)
                continue;
            var props = t.Properties;
            var ctor = t.Methods.FirstOrDefault(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == props.Count && props.Count > 0);
            if (ctor == null) continue;
            for (int i = 0; i < ctor.Parameters.Count; i++)
            {
                var pName = props[i].Name;
                if (!string.IsNullOrEmpty(pName) && ctor.Parameters[i].Name != pName)
                {
                    ctor.Parameters[i].Name = pName;
                    fixedParams++;
                }
            }
            fixedCtors++;
        }
        if (fixedCtors > 0)
            Console.WriteLine($"Restored {fixedParams} ctor param names across {fixedCtors} anonymous types.");

        var bad = mod.AssemblyReferences.FirstOrDefault(a => a.Name == "System.Private.CoreLib");
        if (bad == null)
        {
            Console.WriteLine("No System.Private.CoreLib reference found — nothing to patch.");
            mod.Write(output);
            return 0;
        }

        // Prefer an existing netstandard ref as the new corlib scope; else mscorlib.
        var target = mod.AssemblyReferences.FirstOrDefault(a => a.Name == "netstandard")
                  ?? mod.AssemblyReferences.FirstOrDefault(a => a.Name == "mscorlib");
        if (target == null)
        {
            Console.Error.WriteLine("No netstandard/mscorlib reference to repoint onto — aborting.");
            return 2;
        }

        int repointed = 0;
        foreach (var t in mod.GetTypeReferences())
        {
            if (t.Scope == bad)
            {
                t.Scope = target;
                repointed++;
            }
        }

        mod.AssemblyReferences.Remove(bad);
        mod.Write(output);
        Console.WriteLine($"Repointed {repointed} type refs from System.Private.CoreLib -> {target.Name}; removed the ref.");
        return 0;
    }
}
