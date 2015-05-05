using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CSTS
{
  public class Generator
  {
    private Dictionary<Type, TypeScriptType> _processedTypes = new Dictionary<Type, TypeScriptType>();

    private HashSet<Type> _excludedTypes = new HashSet<Type>();
    private GeneratorOptions _options;

    public Generator(params Type[] types)
      : this(new GeneratorOptions(types))
    {

    }

    public Generator(IEnumerable<Type> types, Func<Type, bool> processType, Func<Type, bool> processBaseType, Func<Type, string> moduleNameGenerator)
      : this(new GeneratorOptions
      {
        Types = types,
        ModuleNameGenerator = moduleNameGenerator,
        TypeFilter = processType,
        BaseTypeFilter = processBaseType
      })
    {
    }

    public Generator(GeneratorOptions options)
    {
      _options = options;
    }

    private void ProcessProperties(CustomType tst)
    {
      BindingFlags flags;

      if (tst.IncludeInheritedProperties)
      {
        flags = BindingFlags.Instance | BindingFlags.Public;
      }
      else
      {
        flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
      }

      var properties = tst.ClrType.GetProperties(flags).Cast<MemberInfo>().Union(tst.ClrType.GetFields(flags));

      foreach (var property in properties)
      {
        var type = property.MemberType == MemberTypes.Property ? ((PropertyInfo)property).PropertyType : ((FieldInfo)property).FieldType;

        var propertyTst = ProcessTypeScriptType(type, (dynamic)GetTypeScriptType(type));

        tst.Properties.Add(new TypeScriptProperty
        {
          Property = property,
          Type = propertyTst
        });
      }
    }

    private TypeScriptType ProcessTypeScriptType(Type t, ArrayType tst)
    {
      var typeInside = TypeHelper.GetTypeInsideEnumerable(t);

      var typeInsideTst = GetTypeScriptType(typeInside);

      tst.ElementType = ProcessTypeScriptType(typeInside, (dynamic)typeInsideTst);

      return tst;
    }

    private TypeScriptType ProcessTypeScriptType(Type t, DictionaryType tst)
    {
      if (tst.ClrType.IsGenericType)
      {
        var args = tst.ClrType.GetGenericArguments();

        if (typeof(IDictionary).IsAssignableFrom(tst.ClrType) && args.Length == 2)
        {
          var keyTst = GetTypeScriptType(args[0]);

          tst.ElementKeyType = ProcessTypeScriptType(args[0], (dynamic)keyTst);

          var valueTst = GetTypeScriptType(args[1]);

          tst.ElementValueType = ProcessTypeScriptType(args[1], (dynamic)valueTst);
        }
      }

      return tst;
    }

    private TypeScriptType ProcessTypeScriptType(Type t, EnumType tst)
    {
      TypeScriptType processedType;

      if (!_processedTypes.TryGetValue(tst.ClrType, out processedType))
      {
        processedType = tst;

        _processedTypes.Add(tst.ClrType, processedType);

        tst.Module = _options.ModuleNameGenerator(t);
      }

      return processedType;
    }

    private TypeScriptType ProcessTypeScriptType(Type t, CustomType tst)
    {
      TypeScriptType processedType;

      if (!_processedTypes.TryGetValue(t, out processedType))
      {
        if (!(t.IsGenericType && !t.IsGenericTypeDefinition))
        {
          _processedTypes.Add(t, tst);
        }
        else if (t.IsGenericType)
        {
          ProcessTypeScriptType(t.GetGenericTypeDefinition(), (dynamic)GetTypeScriptType(t.GetGenericTypeDefinition()));
        }

        processedType = tst;

        bool skippedBaseType;

        var baseType = GetBaseType(t, out skippedBaseType);

        if (baseType != null)
        {
          if (_options.BaseTypeFilter(baseType))
          {
            var processedBaseType = ProcessTypeScriptType(baseType, (dynamic)GetTypeScriptType(baseType));

            tst.BaseType = processedBaseType;
          }

          tst.IncludeInheritedProperties = skippedBaseType;
        }

        ProcessProperties(tst);

        tst.Module = _options.ModuleNameGenerator(t);

        ProcessGenericArguments(t, tst);

        ProcessGenericConstraints(t, tst);

        ProcessNestedType(t, tst);

        ProcessInterfaces(t, tst);
      }

      return processedType;
    }

    private void ProcessGenericConstraints(Type t, CustomType tst)
    {
      if (t.IsGenericTypeDefinition)
      {
        tst.GenericParameters = new List<GenericParameter>();

        foreach (var param in t.GetGenericArguments())
        {
          var genericParam = new GenericParameter()
          {
            GenericConstraints = new List<TypeScriptType>(),
            ClrGenericArgument = param
          };

          tst.GenericParameters.Add(genericParam);
          var constraints = param.GetGenericParameterConstraints();

          if (constraints.Any())
          {
            foreach (var constraint in constraints)
            {
              var constraintArg = ProcessTypeScriptType(constraint, (dynamic)GetTypeScriptType(constraint));

              genericParam.GenericConstraints.Add((TypeScriptType)constraintArg);
            }
          }
        }
      }
    }

    private void ProcessInterfaces(Type t, CustomType tst)
    {
      var implementedInterfaces = t.BaseType != null ? t.GetInterfaces().Except(t.BaseType.GetInterfaces()) : t.GetInterfaces();

      foreach (var implementedInterface in implementedInterfaces)
      {
        TypeScriptType processedInterfaceType = ProcessTypeScriptType(implementedInterface, (dynamic)GetTypeScriptType(implementedInterface));

        if (processedInterfaceType is InterfaceType)
        {
          tst.ImplementedInterfaces.Add(processedInterfaceType);
        }
      }
    }

    private void ProcessNestedType(Type t, CustomType tst)
    {
      if (t.IsNestedPublic && t.DeclaringType != null)
      {
        var declaringTypeTst = ProcessTypeScriptType(t.DeclaringType, (dynamic)GetTypeScriptType(t.DeclaringType));
        tst.DeclaringType = declaringTypeTst;
      }
    }

    private void ProcessGenericArguments(Type t, CustomType tst)
    {
      if (t.IsGenericType && !t.IsGenericTypeDefinition)
      {
        var baseTypeGenericArguments = t.GetGenericArguments();
        tst.GenericArguments = new List<TypeScriptType>();

        foreach (var arg in baseTypeGenericArguments)
        {
          var baseGenericArgTst = ProcessTypeScriptType(arg, (dynamic)GetTypeScriptType(arg));

          tst.GenericArguments.Add((TypeScriptType)baseGenericArgTst);
        }
      }
    }

    private bool BaseTypeHasSameNameAsSubType(Type t, Type baseType)
    {
      var baseTypename = TypeHelper.GetNameOfGenericType(baseType);

      return baseTypename != null && baseTypename == t.Name;
    }

    private Type GetBaseType(Type t, out bool skippedBaseType)
    {
      skippedBaseType = false;

      var baseType = t.BaseType;

      while (baseType != null && baseType.IsGenericType)
      {
        var sameName = BaseTypeHasSameNameAsSubType(t, baseType);

        if (sameName || _excludedTypes.Contains(baseType.GetGenericTypeDefinition()))
        {
          skippedBaseType = true;

          _excludedTypes.Add(baseType.GetGenericTypeDefinition());

          baseType = baseType.BaseType;
        }
        else
        {
          break;
        }
      }

      return baseType;
    }

    private TypeScriptType ProcessTypeScriptType(Type t, TypeScriptType tst)
    {
      return tst;
    }

    private TypeScriptType GetTypeScriptType(Type type)
    {
      TypeScriptType tst;

      if (TypeHelper.Is(type, typeof(string)))
      {
        tst = new StringType();
      }
      else if (TypeHelper.Is(type, typeof(bool)))
      {
        tst = new BooleanType();
      }
      else if (TypeHelper.Is(type, typeof(int),
                                        typeof(decimal),
                                        typeof(double),
                                        typeof(long),
                                        typeof(float),
                                        typeof(short),
                                        typeof(byte),
                                        typeof(uint),
                                        typeof(ushort),
                                        typeof(ulong),
                                        typeof(sbyte)
                                        ))
      {
        tst = new NumberType();
      }
      else if (TypeHelper.Is(type, typeof(DateTime)))
      {
        tst = new DateTimeType();
      }
      else if (TypeHelper.Is(type, typeof(TimeSpan)))
      {
        tst = new TimeSpanType();
      }
      else if (type.IsGenericParameter)
      {
        tst = new GenericTypeParameter();
      }
      else if (TypeHelper.IsDictionary(type))
      {
        tst = new DictionaryType();
      }
      else if (TypeHelper.IsEnumerable(type))
      {
        tst = new ArrayType();
      }
      else if (TypeHelper.IsEnum(type))
      {
        tst = new EnumType();
      }
      else
      {
        var processType = _options.TypeFilter(type);

        if (processType)
        {
          if (type.IsInterface)
          {
            tst = new InterfaceType(type);
          }
          else
          {
            tst = new CustomType(type);
          }
        }
        else
        {
          tst = new AnyType();
        }
      }

      if (TypeHelper.IsNullableValueType(type))
      {
        ((ValueType)tst).IsNullable = true;
        type = Nullable.GetUnderlyingType(type);
      }

      tst.ClrType = type;

      return tst;
    }

    public string GenerateTypes()
    {
      var mapping = GenerateMapping();

      var generator = new ClassDefinitionsGenerator(mapping, this._options);
      var result = generator.Generate();

      return result;
    }


    public string GenerateInterfaceDefinitions()
    {
      var mapping = GenerateMapping();

      var generator = new InterfaceDefinitionsGenerator(mapping, this._options);
      var result = generator.Generate();

      return result;
    }

    internal IList<TypeScriptModule> GenerateMapping()
    {
      foreach (var type in _options.Types.ToList())
      {
        var tst = GetTypeScriptType(type);

        ProcessTypeScriptType(type, (dynamic)tst);
      }

      var groupedByModule = _processedTypes.Values.OfType<IModuleMember>()
        .GroupBy(m => m.Module)
        .Select(m => new TypeScriptModule
        {
          Module = m.Key,
          ModuleMembers = m.ToList()
        }).ToList();

      return groupedByModule;
    }
  }
}
