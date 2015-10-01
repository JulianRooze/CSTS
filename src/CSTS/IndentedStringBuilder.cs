using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSTS
{
  internal class IndentedStringBuilder
  {
    private readonly StringBuilder _sb;
    private readonly int _indentIncrement;
    private readonly char _indentChar;

    public IndentedStringBuilder(int capacity, char indentChar, int indentIncrement)
    {
      _sb = new StringBuilder(capacity);
      _currentIndentation = 0;
      _indentIncrement = indentIncrement;
      _indentChar = indentChar;
    }

    private int _currentIndentation;

    public void IncreaseIndentation()
    {
      _currentIndentation += _indentIncrement;
    }

    public void DecreaseIndentation()
    {
      _currentIndentation = Math.Max(0, _currentIndentation - _indentIncrement);
    }

    private void AppendWithIndentation(string template, params object[] args)
    {
      _sb.AppendLine(new string(_indentChar, _currentIndentation) + string.Format(template, args));
    }

    public IndentedStringBuilder AppendLine(string template, params object[] args)
    {
      AppendWithIndentation(template, args);
      return this;
    }

    public override string ToString()
    {
      return _sb.ToString();
    }
  }
}
