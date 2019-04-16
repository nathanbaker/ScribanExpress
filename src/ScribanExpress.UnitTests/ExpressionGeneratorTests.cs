using Scriban;
using Scriban.Parsing;
using Scriban.Syntax;
using ScribanExpress.UnitTests.Helpers;
using ScribanExpress.UnitTests.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Xunit;

namespace ScribanExpress.UnitTests
{
    public class ExpressionGeneratorTests
    {
        Person person;
        public ExpressionGeneratorTests()
        {
            person = new Person()
            {
                FirstName = "Billy",
                Age = 23,
                Company = new Company { Title = "compname" }
            };
        }

        [Fact]
        public void RawTextBlock()
        {
            var templateText = @"This is a World from scriban!";
            var template = Template.Parse(templateText, null, null, null);

            var result = new ExpressionGenerator().Generate<string, object>(template.Page.Body);

            var functor = result.Compile();

            Assert.Equal("This is a World from scriban!", functor("doesn'tm atter", null));
        }


        [Fact]
        public void MultiRawScriptHelloWorld()
        {
            ScriptBlockStatement scriptBlockStatement = new ScriptBlockStatement();
            scriptBlockStatement.Statements.Add(ScriptRawStatementHelper.CreateScriptRawStatement("first"));
            scriptBlockStatement.Statements.Add(ScriptRawStatementHelper.CreateScriptRawStatement("second"));

            var result = new ExpressionGenerator().Generate<string, object>(scriptBlockStatement);

            var functor = result.Compile();

            Assert.Equal("firstsecond", functor("saasdf", null));
        }


        [Fact]
        public void StringProperty()
        {
            var templateText = @"This is a {{ Length }} World from scriban!";
            var template = Template.Parse(templateText, null, null, null);

            var result = new ExpressionGenerator().Generate<string, object>(template.Page.Body);

            var functor = result.Compile();

            var stringResult = functor("Hello", null);

            Assert.Equal("This is a 5 World from scriban!", stringResult);
        }

        [Fact]
        public void NamedStringProperty()
        {
            var presonwrapper = new { person };

            var templateText = @"{{ person.FirstName }}";
            var template = Template.Parse(templateText, null, null, null);

            var result = AnonGenerate(presonwrapper, template.Page.Body);

            var functor = result.Compile();

            var stringResult = functor(presonwrapper, null);

            Assert.Equal("Billy", stringResult);
        }

        [Fact]
        public void NamedProperty_NotSet()
        {
            var person = new { person = new Person() };

            var templateText = @"{{ person.FirstName }}";
            var template = Template.Parse(templateText, null, null, null);

            var result = AnonGenerate(person, template.Page.Body);

            var functor = result.Compile();

            var stringResult = functor(person, null);

            Assert.Equal("", stringResult);
        }



        [Fact]
        public void NamedNumberProperty()
        {
            var personWrapper = new { person };

            var templateText = @"{{ person.Age }}";
            var template = Template.Parse(templateText, null, null, null);

            var result = AnonGenerate(personWrapper, template.Page.Body);
            var functor = result.Compile();

            var stringResult = functor(personWrapper, null);

            Assert.Equal("23", stringResult);
        }

        [Fact]
        public void ChainedProperty()
        {
            var personWrapper = new { person };

            var templateText = @"{{ person.Company.Title }}";
            var template = Template.Parse(templateText, null, null, null);

            var result = AnonGenerate(personWrapper, template.Page.Body);

            var functor = result.Compile();

            var stringResult = functor(personWrapper, null);

            Assert.Equal("compname", stringResult);
        }


        [Fact]
        public void Pipeline()
        {
            var personWrapper = new { person };

            var templateText = @"{{ person.FirstName | string.downcase }}"; //in our case it might be downcase
            var template = Template.Parse(templateText, null, null, null);

            var result = AnonGenerate(personWrapper, template.Page.Body);

            var functor = result.Compile();

            var stringResult = functor(personWrapper, null);

            Assert.Equal("billy", stringResult);
        }
        [Fact]
        public void Method_WithParameters_OnProperty()
        {
            var personWrapper = new { person };

            var templateText = @"blah blah {{ person.Age.ToString ""C"" }}";
            var template = Template.Parse(templateText, null, null, null);

            var result = AnonGenerate(personWrapper, template.Page.Body);

            var functor = result.Compile();

            var stringResult = functor(personWrapper, null);

            Assert.Equal("blah blah $23.00", stringResult);
        }


        //[Fact]
        //public void Pipeline_Multiple()
        //{
        //    var person = new { person = new Person() { FirstName = "Billy" } };

        //    //var templateText = @"This is a {{ text }} World from scriban!";
        //    var templateText = @"blah blah {{ person.FirstName | string.downcase | Length }}";
        //    var template = Template.Parse(templateText, null, null, null);

        //    var result = AnonGenerate(person, template.Page.Body);

        //    var functor = result.Compile();

        //    var stringResult = functor(person);

        //    // Assert.Equal(@"x => ""This is a Hello World from scriban!""", result.ToString());
        //    Assert.Equal("blah blah billy", stringResult);
        //}


        [Fact]
        public void Multi_TopLevelObjects()
        {
            Person oneperson = new Person() { FirstName = "Billy" };
            Person twoperson = new Person() { FirstName = "Bob" };
            var templateText = @"{{ oneperson.FirstName  }} {{ twoperson.FirstName }}"; //in our case it might be downcase
            var template = Template.Parse(templateText, null, null, null);

            var anon = new { oneperson, twoperson };

            var result = AnonGenerate(anon, template.Page.Body);

            var functor = result.Compile();

            var stringResult = functor(anon, null);

            Assert.Equal("Billy Bob", stringResult);
        }

        public Expression<Func<T, object, string>> AnonGenerate<T>(T value, ScriptBlockStatement scriptBlockStatement)
        {
            return new ExpressionGenerator().Generate<T, object>(scriptBlockStatement);
        }

    }
}