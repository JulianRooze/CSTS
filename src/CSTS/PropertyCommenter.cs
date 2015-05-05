using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSTS
{
  internal class PropertyCommenter
  {
    private GeneratorOptions _options;

    internal PropertyCommenter(GeneratorOptions options)
    {
      _options = options;
    }

    private string GetPropertyComment(ValueType p)
    {
      return p.ClrType.Name + (p.IsNullable ? ", nullable" : "");
    }

    private string GetPropertyComment(BooleanType p)
    {
      return null;
    }

    private string GetPropertyComment(EnumType p)
    {
      return null;
    }

    private string GetPropertyComment(TypeScriptType p)
    {
      return null;
    }

    public string GetPropertyCommentPrefixed(TypeScriptProperty p)
    {
      var func = _options.CommentingOptions.PrefixedCommentGenerator;

      if (func != null)
      {
        var comment = func(p.Property);

        if (!string.IsNullOrEmpty(comment))
        {
          return "// " + comment;
        }
      }

      return null;
    }

    public string GetPropertyCommentPostfixed(TypeScriptProperty p)
    {
      var obsolete = p.Property.GetCustomAttributes(typeof(ObsoleteAttribute), false).FirstOrDefault() as ObsoleteAttribute;

      var comments = new List<string>();

      if (obsolete != null)
      {
        comments.Add(string.Format("obsolete {0}", string.IsNullOrEmpty(obsolete.Message) ? "" : ": " + obsolete.Message));
      }

      var propertyComment = this.GetPropertyComment((dynamic)p.Type);

      if (propertyComment != null)
      {
        comments.Add(propertyComment);
      }

      if (comments.Any())
      {
        return "// " + string.Join(", ", comments);
      }

      return "";
    }

  }
}
