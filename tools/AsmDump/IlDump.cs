using System;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class IlDump
{
    public static int Run(string asmPath, string typeName, string methodName)
    {
        var asm = AssemblyDefinition.ReadAssembly(asmPath);
        var sb = new StringBuilder();
        var type = asm.MainModule.Types.FirstOrDefault(t => t.FullName == typeName)
                   ?? asm.MainModule.Types.SelectMany(AllNested).FirstOrDefault(t => t.FullName == typeName);
        if (type == null)
        {
            Console.WriteLine("type not found: " + typeName);
            return 2;
        }

        foreach (var m in type.Methods.Where(x => x.Name == methodName))
        {
            sb.AppendLine("== " + type.FullName + "::" + m.Name);
            sb.AppendLine("   " + m.FullName);
            if (!m.HasBody)
            {
                sb.AppendLine("   [no body]");
                continue;
            }
            foreach (var v in m.Body.Variables)
                sb.AppendLine("   .local " + v.Index + " " + v.VariableType.FullName);
            foreach (var ins in m.Body.Instructions)
            {
                sb.AppendLine("   " + ins.Offset.ToString("X4") + "  " + ins.OpCode + " " + FormatOperand(ins.Operand));
            }
            sb.AppendLine();
        }

        Console.WriteLine(sb.ToString());
        return 0;
    }

    private static string FormatOperand(object op)
    {
        if (op == null) return "";
        if (op is string s) return "\"" + s + "\"";
        if (op is MethodReference mr) return mr.DeclaringType.Name + "::" + mr.Name;
        if (op is FieldReference fr) return fr.DeclaringType.Name + "::" + fr.Name;
        if (op is TypeReference tr) return tr.Name;
        if (op is ParameterDefinition pd) return pd.Name;
        if (op is VariableDefinition vd) return "V" + vd.Index;
        return op.ToString();
    }

    private static System.Collections.Generic.IEnumerable<TypeDefinition> AllNested(TypeDefinition t)
    {
        foreach (var n in t.NestedTypes)
        {
            yield return n;
            foreach (var nn in AllNested(n)) yield return nn;
        }
    }
}
