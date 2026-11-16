using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length >= 3 && args[0] == "find")
        {
            return FindCallers.Run(args[1], args[2], args.Length > 3 ? args[3] : null);
        }

        if (args.Length >= 4 && args[0] == "il")
        {
            return IlDump.Run(args[1], args[2], args[3]);
        }

        if (args.Length < 2)
        {
            Console.WriteLine("usage: AsmDump <assembly> <out.txt> [keyword-regex]");
            return 1;
        }

        var asmPath = args[0];
        var outPath = args[1];
        var keyword = args.Length > 2 ? args[2] : "GameSpeed|Inventory|Storage|Drifter|Townheart|Resource|TimeManager|Difficulty";

        var asm = AssemblyDefinition.ReadAssembly(asmPath);
        var sb = new StringBuilder();
        sb.AppendLine("# assembly: " + asm.Name.FullName);
        sb.AppendLine("# keyword: " + keyword);
        sb.AppendLine();

        var allTypes = new List<TypeDefinition>();
        Collect(asm.MainModule, allTypes);

        sb.AppendLine("## ALL TYPES (" + allTypes.Count + ")");
        foreach (var t in allTypes.OrderBy(x => x.FullName, StringComparer.Ordinal))
        {
            sb.AppendLine(t.FullName);
        }
        sb.AppendLine();

        var rx = new System.Text.RegularExpressions.Regex(keyword, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        sb.AppendLine("## MATCHED TYPE DETAILS");
        foreach (var t in allTypes.OrderBy(x => x.FullName, StringComparer.Ordinal))
        {
            if (!rx.IsMatch(t.FullName)) continue;
            sb.AppendLine("### " + t.FullName + "  (base: " + (t.BaseType == null ? "-" : t.BaseType.FullName) + ")");
            foreach (var f in t.Fields)
            {
                sb.AppendLine("    F " + Acc(f) + (f.IsStatic ? "static " : "") + f.FieldType.Name + " " + f.Name);
            }
            foreach (var p in t.Properties)
            {
                sb.AppendLine("    P " + p.PropertyType.Name + " " + p.Name);
            }
            foreach (var m in t.Methods)
            {
                var ps = string.Join(", ", m.Parameters.Select(x => x.ParameterType.Name + " " + x.Name));
                sb.AppendLine("    M " + Acc(m) + (m.IsStatic ? "static " : "") + (m.IsVirtual ? "virtual " : "") + m.ReturnType.Name + " " + m.Name + "(" + ps + ")" + (m.HasBody ? "" : " [no-body]"));
            }
            sb.AppendLine();
        }

        File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(false));
        Console.WriteLine("wrote " + outPath + " (" + sb.Length + " chars, " + allTypes.Count + " types)");
        return 0;
    }

    private static string Acc(MethodDefinition m)
    {
        if (m.IsPublic) return "public ";
        if (m.IsPrivate) return "private ";
        if (m.IsFamily) return "protected ";
        return "internal ";
    }

    private static string Acc(FieldDefinition f)
    {
        if (f.IsPublic) return "public ";
        if (f.IsPrivate) return "private ";
        if (f.IsFamily) return "protected ";
        return "internal ";
    }

    private static void Collect(ModuleDefinition module, List<TypeDefinition> sink)
    {
        foreach (var t in module.Types)
        {
            sink.Add(t);
            foreach (var n in t.NestedTypes) sink.Add(n);
            // nested-nested (one more level is enough in practice)
            foreach (var n in t.NestedTypes)
                foreach (var nn in n.NestedTypes) sink.Add(nn);
        }
    }
}
