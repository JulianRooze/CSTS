using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CSTS;
using System.Linq;
using FluentAssertions;
using System.Collections.Generic;

namespace CSTS.Tests
{
  [TestClass]
  public class GenericTests
  {
    public class GenericBase1<T>
    {

    }

    public class NonGeneric1 : GenericBase1<string>
    {

    }

    [TestMethod]
    public void When_the_BaseType_of_a_class_is_generic_this_should_be_reflected_in_the_mapping()
    {
      var generator = new Generator(typeof(NonGeneric1));

      var modules = generator.GenerateMapping();

      var definition = (CustomType)modules[0].ModuleMembers.OfType<TypeScriptType>().Single(t => t.ClrType.Name == "NonGeneric1");

      definition.BaseType.ClrType.Should().Be(typeof(GenericBase1<string>));
      ((CustomType)definition.BaseType).GenericArguments[0].Should().BeOfType<StringType>();
    }

    public class GenericBase2<T>
    {
      public T Property { get; set; }
    }

    public class NonGeneric2 : GenericBase2<string>
    {

    }

    [TestMethod]
    public void When_a_generic_property_has_been_defined_on_the_BaseClass_it_should_not_be_present_on_the_subclass()
    {
      var generator = new Generator(typeof(NonGeneric2));

      var modules = generator.GenerateMapping();

      var definition = (CustomType)modules[0].ModuleMembers.OfType<TypeScriptType>().Single(t => t.ClrType.Name == "NonGeneric2");

      definition.Properties.Should().BeEmpty();
      ((CustomType)definition.BaseType).Properties.Should().HaveCount(1);
    }

    public class GenericBase3<T>
    {
      public T Property { get; set; }
    }

    public class GenericBase3 : GenericBase3<string>
    {

    }

    /// <summary>
    /// TypeScript doesn't support two interfaces with the same name that only differ in generic type parameters while C# does support this
    /// </summary>
    [TestMethod]
    public void When_the_BaseClass_has_the_same_name_as_the_subclass_it_should_not_be_the_BaseType_in_the_TS_definition()
    {
      var generator = new Generator(typeof(GenericBase3));

      var modules = generator.GenerateMapping();

      var definition = (CustomType)modules[0].ModuleMembers.OfType<TypeScriptType>().Single(t => t.ClrType.Name == "GenericBase3");

      definition.BaseType.Should().BeNull();
    }

    /// <summary>
    /// TypeScript doesn't support two interfaces with the same name that only differ in generic type parameters while C# does support this
    /// </summary>
    [TestMethod]
    public void When_the_BaseClass_has_the_same_name_as_the_subclass_it_should_include_the_properties_of_the_BaseType()
    {
      var generator = new Generator(typeof(GenericBase3));

      var modules = generator.GenerateMapping();

      var definition = (CustomType)modules[0].ModuleMembers.OfType<TypeScriptType>().Single(t => t.ClrType.Name == "GenericBase3");

      definition.Properties.Single().Type.Should().BeOfType(typeof(StringType));
    }


    public class GenericBaseBase4
    {
      public string Property { get; set; }
    }

    public class GenericBase4<T> : GenericBaseBase4
    {
      public T AnotherProperty { get; set; }
    }

    public class GenericBase4 : GenericBase4<int>
    {

    }

    public class AnotherGenericBase4 : GenericBase4<DateTime>
    {

    }


    [TestMethod]
    public void When_the_direct_BaseType_has_the_same_name_the_BaseType_of_the_definition_should_be_BaseType_of_the_BaseType()
    {
      var generator = new Generator(typeof(GenericBase4));

      var modules = generator.GenerateMapping();

      var definition = (CustomType)modules[0].ModuleMembers.OfType<TypeScriptType>().Single(t => t.ClrType.Name == "GenericBase4");

      definition.BaseType.ClrType.Should().Be(typeof(GenericBaseBase4));
    }


    [TestMethod]
    public void Conflicting_BaseType_names_should_not_be_used_as_BaseType_for_other_types_whose_name_does_not_conflict()
    {
      var generator = new Generator(typeof(GenericBase4), typeof(AnotherGenericBase4));

      var modules = generator.GenerateMapping();

      var definition = (CustomType)modules[0].ModuleMembers.OfType<TypeScriptType>().Single(t => t.ClrType.Name == "GenericBase4");

      definition.BaseType.ClrType.Should().Be(typeof(GenericBaseBase4));

      var definition1 = (CustomType)modules[0].ModuleMembers.OfType<TypeScriptType>().Single(t => t.ClrType.Name == "AnotherGenericBase4");

      definition1.BaseType.ClrType.Should().Be(typeof(GenericBaseBase4));
    }

    public class BaseClass
    {

    }

    public class BaseClass<T> : BaseClass
    {
      public T Property { get; set; }
    }

    [TestMethod]
    public void Test()
    {
      var generator = new Generator(typeof(BaseClass));

      var modules = generator.GenerateMapping();

    }

    public class ConstraintType
    {

    }

    public class ClassWithGenericConstraint<T> where T : ConstraintType
    {

    }

    [TestMethod]
    public void Generic_constraints_should_be_included_in_definition()
    {
      var generator = new Generator(typeof(ClassWithGenericConstraint<>));

      var result = generator.GenerateTypes();

      result.Should().Contain("class GenericTestsClassWithGenericConstraint<T extends CSTS.Tests.GenericTestsConstraintType>");

    }

    public class ClassWithRecursiveConstraint<T> where T : IEnumerable<T>
    {

    }


    // Is the constraint recursive? Not supported in TypeScript. Just treat the self referential constraint as 'any' 
    // https://typescript.codeplex.com/wikipage?title=Known%20breaking%20changes%20between%200.8%20and%200.9&referringTitle=Documentation
    [TestMethod]
    public void Recursive_generic_constraints_should_be_treated_as_type_any()
    {
      var generator = new Generator(typeof(ClassWithRecursiveConstraint<>));

      var result = generator.GenerateTypes();

      result.Should().Contain("class GenericTestsClassWithRecursiveConstraint<T extends any[]>");

    }

    public class ClassWithDictionaries
    {
      public Dictionary<string, string> StringStringDictionary { get; set; }
      public Dictionary<DictEnum, string> EnumStringDictionary { get; set; }
    }

    public enum DictEnum
    {
      Test = 1
    }

    [TestMethod]
    public void Dictionary_with_Enum_as_key_should_be_handled_as_Number()
    {
      var generator = new Generator(typeof(ClassWithDictionaries));

      var result = generator.GenerateTypes();

      result.Should().Contain("EnumStringDictionary : { [ DictEnum : number ] : string };");

    }
  }
}

