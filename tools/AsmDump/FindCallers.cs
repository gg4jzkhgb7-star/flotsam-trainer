using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class FindCallers
{
    /// <summary>
    /// 扫描整个程序集，找出所有调用/读取指定成员的方法。
    /// 用法： find &lt;asm&gt; &lt;MemberName&gt; [DeclaringTypeNamePart]
    /// </summary>
    public static int Run(string asmPath, string memberName, string typeFilter)
    {
        var asm = AssemblyDefinition.ReadAssembly(asmPath);
        var sb = new StringBuilder();
        int hits = 0;

        foreach (var type in AllTypes(asm.MainModule))
        {
            foreach (var method in type.Methods)
            {
                if (!method.HasBody) continue;

                foreach (var ins in method.Body.Instructions)
                {
                    string declType = null;
                    string name = null;

                    if (ins.Operand is MethodReference mr)
                    {
                        declType = mr.DeclaringType?.Name;
                        name = mr.Name;
                    }
                    else if (ins.Operand is FieldReference fr)
                    {
                        declType = fr.DeclaringType?.Name;
                        name = fr.Name;
                    }

                    if (name == null || name != memberName) continue;
                    if (!string.IsNullOrEmpty(typeFilter) &&
                        (declType == null || declType.IndexOf(typeFilter, StringComparison.OrdinalIgnoreCase) < 0)) continue;

                    hits++;
                    sb.AppendLine($"--- {type.FullName}::{method.Name}({string.Join(", ", method.Parameters.Select(p => p.ParameterType.Name))})   [{declType}::{name}]");

                    int idx = method.Body.Instructions.IndexOf(ins);
                    int from = Math.Max(0, idx - 6);
                    int to = Math.Min(method.Body.Instructions.Count - 1, idx + 10);
                    for (int i = from; i <= to; i++)
                    {
                        var cur = method.Body.Instructions[i];
                        string mark = (i == idx) ? ">>" : "  ";
                        sb.AppendLine($"    {mark} {cur.Offset:X4} {cur.OpCode} {FormatOperand(cur.Operand)}");
                    }
                    sb.AppendLine();
                }
            }
        }

        Console.WriteLine($"# hits: {hits}");
        Console.WriteLine(sb.ToString());
        return 0;
    }

    private static string FormatOperand(object op)
    {
        if (op == null) return "";
        if (op is string s) return "\"" + s + "\"";
        if (op is MethodReference mr) return mr.DeclaringType?.Name + "::" + mr.Name;
        if (op is FieldReference fr) return fr.DeclaringType?.Name + "::" + fr.Name;
        if (op is TypeReference tr) return tr.Name;
        if (op is ParameterDefinition pd) return pd.Name;
        if (op is VariableDefinition vd) return "V" + vd.Index;
        return op.ToString();
    }

    private static IEnumerable<TypeDefinition> AllTypes(ModuleDefinition module)
    {
        foreach (var t in module.Types)
        {
            yield return t;
            foreach (var n in Walk(t)) yield return n;
        }
    }

    private static IEnumerable<TypeDefinition> Walk(TypeDefinition t)
    {
        foreach (var n in t.NestedTypes)
        {
            yield return n;
            foreach (var nn in Walk(n)) yield return nn;
        }
    }
}
