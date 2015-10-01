using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CSTS
{
  public class GeneratorOptions
  {
    public GeneratorOptions()
    {
      this.CommentingOptions = CommentingOptions.Default;
      this.CodeGenerationOptions = CodeGenerationOptions.Default;

      this.TypeFilter = t => this.Types.Any(x => x.Assembly == t.Assembly);
      this.BaseTypeFilter = t => this.Types.Any(x => x.Assembly == t.Assembly);
      this.ModuleNameGenerator = t => t.Namespace;
    }

    public GeneratorOptions(IEnumerable<Type> types) : this()
    {
      this.Types = types;
    }

    public IEnumerable<Type> Types { get; set; }

    public Func<Type, bool> TypeFilter { get; set; }

    public Func<Type, bool> BaseTypeFilter { get; set; }

    public Func<MemberInfo, bool> PropertyFilter { get; set; }

    private Func<Type, string> _userSuppliedModuleNameGenerator;

    public Func<Type, string> ModuleNameGenerator
    {
      get
      {
        return _userSuppliedModuleNameGenerator;
      }
      set
      {
        _userSuppliedModuleNameGenerator = t =>
        {
          if (TypeHelper.IsNullableValueType(t))
          {
            t = Nullable.GetUnderlyingType(t);
          }

          return value(t);
        };
      }
    }

    public CommentingOptions CommentingOptions { get; set; }
    public CodeGenerationOptions CodeGenerationOptions { get; set; }

  }

  public class CodeGenerationOptions
  {
    public static CodeGenerationOptions Default
    {
      get
      {
        return new CodeGenerationOptions()
        {
          IndentationCharacter = '\t',
          IndentationIncrementAmount = 1
        };
      }
    }

    public Func<Type, IEnumerable<string>> AdditionalMembers { get; set; }

    public bool GenerateExternalModules { get; set; }

    public char IndentationCharacter { get; set; }

    public int IndentationIncrementAmount { get; set; }
  }

  public class CommentingOptions
  {
    public bool RenderObsoleteAttributesAsComments { get; set; }

    public Func<MemberInfo, string> PrefixedCommentGenerator { get; set; }

    public static CommentingOptions Default
    {
      get
      {
        return new CommentingOptions
        {
          RenderObsoleteAttributesAsComments = true
        };
      }
    }
  }
}
