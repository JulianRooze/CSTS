using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CSTS
{

  internal class ClassDefinitionsGenerator
  {
    private StringBuilder _sb;
    private PropertyCommenter _propertyCommenter;
    private ModuleNameGenerator _moduleNameGenerator = new ModuleNameGenerator();
    private TypeNameGenerator _typeNameGenerator;
    private IEnumerable<TypeScriptModule> _modules;

    private HashSet<Type> _processedTypes = new HashSet<Type>();
    private HashSet<string> _processedModules = new HashSet<string>();
    private Dictionary<string, TypeScriptModule> _modulesByName;
    private GeneratorOptions _options;

    public ClassDefinitionsGenerator(IEnumerable<TypeScriptModule> modules, GeneratorOptions options)
    {
      _modules = modules;
      _sb = new StringBuilder(modules.Sum(m => m.ModuleMembers.Count) * 256);
      _modulesByName = modules.ToDictionary(k => k.Module);
      _options = options;
      _propertyCommenter = new PropertyCommenter(options);
    }

    public IEnumerable<TypeScriptModule> Modules
    {
      get
      {
        return _modules;
      }
    }

    public string Generate()
    {
      _typeNameGenerator = new TypeNameGenerator(this.Modules, _moduleNameGenerator);

      foreach (var module in _modules)
      {
        RenderModule(module);
      }

      return _sb.ToString();
    }

    private void RenderModule(string module)
    {
      RenderModule(_modulesByName[module]);
    }

    private void RenderModule(TypeScriptModule module)
    {
      if (_processedModules.Contains(module.Module))
      {
        return;
      }

      _processedModules.Add(module.Module);

      var moduleBuffer = new IndentedStringBuilder(module.ModuleMembers.Count * 256, _options.CodeGenerationOptions.IndentationCharacter, _options.CodeGenerationOptions.IndentationIncrementAmount);

      if (!string.IsNullOrEmpty(module.Module))
      {
        moduleBuffer.AppendLine("{1}module {0} {{", module.Module, _options.CodeGenerationOptions.GenerateExternalModules ? "export " : "");
        moduleBuffer.IncreaseIndentation();
        moduleBuffer.AppendLine("");
      }

      foreach (var type in module.ModuleMembers)
      {
        Render(moduleBuffer, (dynamic)type);
      }

      if (!string.IsNullOrEmpty(module.Module))
      {
        moduleBuffer.DecreaseIndentation();
        moduleBuffer.AppendLine("}}");
        moduleBuffer.AppendLine("");
      }

      _sb.AppendLine(moduleBuffer.ToString());
    }

    private void Render(IndentedStringBuilder sb, CustomType type)
    {
      if (_processedTypes.Contains(type.ClrType))
      {
        return;
      }

      _processedTypes.Add(type.ClrType.IsGenericType ? type.ClrType.GetGenericTypeDefinition() : type.ClrType);

      ProcessBaseType(sb, type);

      var interfaceType = type as InterfaceType;

      sb.AppendLine("export {2} {0}{1} {3}{{", _typeNameGenerator.GetTypeName(type), RenderBaseType(type), interfaceType == null ? "class" : "interface", RenderInterfaces(type));
      sb.IncreaseIndentation();

      foreach (var p in type.Properties)
      {
        Render(sb, p);
      }

      if (_options.CodeGenerationOptions.AdditionalMembers != null)
      {
        foreach (var member in _options.CodeGenerationOptions.AdditionalMembers(type.ClrType))
        {
          sb.AppendLine(member);
        }
      }

      sb.DecreaseIndentation();
      sb.AppendLine("}}");
      sb.AppendLine("");

    }

    private void ProcessBaseType(IndentedStringBuilder sb, CustomType type)
    {
      if (type.BaseType != null)
      {
        var baseType = type.BaseType.ClrType;

        if (baseType.IsGenericType)
        {
          baseType = baseType.GetGenericTypeDefinition();
        }

        if (!_processedTypes.Contains(baseType))
        {
          var moduleMember = type.BaseType as IModuleMember;

          if (moduleMember != null)
          {
            if (moduleMember.Module == type.Module)
            {
              Render(sb, (dynamic)type.BaseType);
            }
            else
            {
              RenderModule(moduleMember.Module);
            }
          }
        }
      }
    }

    private void Render(IndentedStringBuilder sb, TypeScriptProperty p)
    {
      var prefixComment = _propertyCommenter.GetPropertyCommentPrefixed(p);

      if (!string.IsNullOrEmpty(prefixComment))
      {
        sb.AppendLine(prefixComment);
      }

      sb.AppendLine("{0} : {1}{2}; {3}", p.Property.Name, _moduleNameGenerator.GetModuleName((dynamic)p.Type), _typeNameGenerator.GetTypeName((dynamic)p.Type), _propertyCommenter.GetPropertyCommentPostfixed(p));
    }

    private string RenderInterfaces(CustomType type)
    {
      if (type.ImplementedInterfaces.Count == 0)
      {
        return "";
      }

      string implements = "implements";

      if (type is InterfaceType)
      {
        implements = "extends";
      }

      return string.Format("{0} {1} ", implements, string.Join(", ", type.ImplementedInterfaces.Select(i => string.Format("{0}{1}", _moduleNameGenerator.GetModuleName((dynamic)i), _typeNameGenerator.GetTypeName((dynamic)i)))));
    }

    private string RenderBaseType(CustomType type)
    {
      if (type.BaseType == null)
      {
        return "";
      }

      var baseType = string.Format(" extends {0}{1}", _moduleNameGenerator.GetModuleName((dynamic)type.BaseType), _typeNameGenerator.GetTypeName((dynamic)type.BaseType));

      return baseType;
    }

    private void Render(IndentedStringBuilder sb, EnumType type)
    {
      sb.AppendLine("export enum {0} {{", type.ClrType.Name);
      sb.IncreaseIndentation();

      var values = Enum.GetValues(type.ClrType);
      var names = Enum.GetNames(type.ClrType);

      int i = 0;

      foreach (var val in values)
      {
        var name = names[i];
        i++;

        sb.AppendLine("{0} = {1},", name, Convert.ChangeType(val, typeof(int)));
      }

      sb.DecreaseIndentation();
      sb.AppendLine("}}");
      sb.AppendLine("");
    }
  }
}
