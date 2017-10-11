using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CSTS;
using FluentAssertions;
using System.Linq;
using System.Collections.Generic;

namespace TypeScriptDefinitionGeneratorTests
{
  [TestClass]
  public class SimpleTests
  {
    public class BasicType
    {
      public int ID { get; set; }
      public string Name { get; set; }
    }

    [TestMethod]
    public void When_generating_for_one_type_the_module_count_should_be_one()
    {
      var generator = new Generator(typeof(BasicType));

      var modules = generator.GenerateMapping();

      modules.Count.Should().Be(1);
    }

    [TestMethod]
    public void The_class_should_be_converted_to_CustomType()
    {
      var generator = new Generator(typeof(BasicType));

      var modules = generator.GenerateMapping();

      var type = modules[0].ModuleMembers[0].Should().BeOfType<CustomType>();
    }

    [TestMethod]
    public void An_Int32_should_be_converted_to_TypeScriptType_number()
    {
      var generator = new Generator(typeof(BasicType));

      var modules = generator.GenerateMapping();

      var type = ((CustomType)modules[0].ModuleMembers[0]).Properties.SingleOrDefault(p => p.Property.Name == "ID");

      type.Type.Should().BeOfType<NumberType>();
    }

    class DictionaryType
    {
      public IDictionary<string, string> Dictionary { get; set; }
    }

    [TestMethod]
    public void Simple_dictionary_should_work()
    {
      var generator = new Generator(typeof(DictionaryType));

      var modules = generator.GenerateMapping();

      var classGenerator = new ClassDefinitionsGenerator(modules, new GeneratorOptions());

      var code = classGenerator.Generate();

      code.Should().Contain("Dictionary : { [ key : string ] : string }; ");
    }
  }
}
