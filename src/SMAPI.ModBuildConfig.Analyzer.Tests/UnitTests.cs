using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using SMAPI.ModBuildConfig.Analyzer.Tests.Framework;
using StardewModdingAPI.ModBuildConfig.Analyzer;

namespace SMAPI.ModBuildConfig.Analyzer.Tests
{
    /// <summary>Unit tests for the C# analyzers.</summary>
    [TestFixture]
    public class UnitTests : DiagnosticVerifier
    {
        /*********
        ** Properties
        *********/
        /// <summary>Sample C# code which contains a simplified representation of Stardew Valley's <c>Netcode</c> types, and sample mod code with a {{test-code}} placeholder for the code being tested.</summary>
        const string SampleProgram = @"
            using System;
            using StardewValley;
            using Netcode;

            namespace Netcode
            {
                public class NetInt : NetFieldBase<int, NetInt> { }
                public class NetRef : NetFieldBase<object, NetRef> { }
                public class NetFieldBase<T, TSelf> where TSelf : NetFieldBase<T, TSelf>
                {
                    public T Value { get; set; }
                    public static implicit operator T(NetFieldBase<T, TSelf> field) => field.Value;
                }
            }

            namespace StardewValley
            {
                class Item
                {
                    public NetInt category { get; } = new NetInt { Value = 42 };
                }
            }

            namespace SampleMod
            {
                class ModEntry
                {
                    public void Entry()
                    {
                        NetInt intField = new NetInt { Value = 42 };
                        NetRef refField = new NetRef { Value = null };
                        Item item = null;

                        // this line should raise diagnostics
                        {{test-code}} // line 36

                        // these lines should not
                        if ((int)intField != 42);
                    }
                }
            }
        ";

        /// <summary>The line number where the unit tested code is injected into <see cref="SampleProgram"/>.</summary>
        private const int SampleCodeLine = 36;

        /// <summary>The column number where the unit tested code is injected into <see cref="SampleProgram"/>.</summary>
        private const int SampleCodeColumn = 25;


        /*********
        ** Unit tests
        *********/
        /// <summary>Test that no diagnostics are raised for an empty code block.</summary>
        [TestCase]
        public void EmptyCode_HasNoDiagnostics()
        {
            // arrange
            string test = @"";

            // assert
            this.VerifyCSharpDiagnostic(test);
        }

        /// <summary>Test that the expected diagnostic message is raised for implicit net field comparisons.</summary>
        /// <param name="codeText">The code line to test.</param>
        /// <param name="column">The column within the code line where the diagnostic message should be reported.</param>
        /// <param name="expression">The expression which should be reported.</param>
        /// <param name="fromType">The source type name which should be reported.</param>
        /// <param name="toType">The target type name which should be reported.</param>
        [TestCase("if (intField < 42);", 4, "intField", "NetInt", "Int32")]
        [TestCase("if (intField <= 42);", 4, "intField", "NetInt", "Int32")]
        [TestCase("if (intField > 42);", 4, "intField", "NetInt", "Int32")]
        [TestCase("if (intField >= 42);", 4, "intField", "NetInt", "Int32")]
        [TestCase("if (intField == 42);", 4, "intField", "NetInt", "Int32")]
        [TestCase("if (intField != 42);", 4, "intField", "NetInt", "Int32")]
        [TestCase("if (refField != null);", 4, "refField", "NetRef", "Object")]
        [TestCase("if (item?.category != 42);", 4, "item?.category", "NetInt", "Int32")]
        public void AvoidImplicitNetFieldComparisons_RaisesDiagnostic(string codeText, int column, string expression, string fromType, string toType)
        {
            // arrange
            string code = UnitTests.SampleProgram.Replace("{{test-code}}", codeText);
            DiagnosticResult expected = new DiagnosticResult
            {
                Id = "SMAPI001",
                Message = $"This implicitly converts '{expression}' from {fromType} to {toType}, but {fromType} has unintuitive implicit conversion rules. Consider comparing against the actual value instead to avoid bugs. See https://smapi.io/buildmsg/SMAPI001 for details.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", UnitTests.SampleCodeLine, UnitTests.SampleCodeColumn + column) }
            };

            // assert
            this.VerifyCSharpDiagnostic(code, expected);
        }

        /// <summary>Test that the expected diagnostic message is raised for avoidable net field references.</summary>
        /// <param name="codeText">The code line to test.</param>
        /// <param name="column">The column within the code line where the diagnostic message should be reported.</param>
        /// <param name="expression">The expression which should be reported.</param>
        /// <param name="netType">The net type name which should be reported.</param>
        /// <param name="suggestedProperty">The suggested property name which should be reported.</param>
        [TestCase("int category = item.category;", 15, "item.category", "NetInt", "Category")]
        [TestCase("int category = (item).category;", 15, "(item).category", "NetInt", "Category")]
        [TestCase("int category = ((Item)item).category;", 15, "((Item)item).category", "NetInt", "Category")]
        public void AvoidNetFields_RaisesDiagnostic(string codeText, int column, string expression, string netType, string suggestedProperty)
        {
            // arrange
            string code = UnitTests.SampleProgram.Replace("{{test-code}}", codeText);
            DiagnosticResult expected = new DiagnosticResult
            {
                Id = "SMAPI002",
                Message = $"'{expression}' is a {netType} field; consider using the {suggestedProperty} property instead. See https://smapi.io/buildmsg/SMAPI002 for details.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", UnitTests.SampleCodeLine, UnitTests.SampleCodeColumn + column) }
            };

            // assert
            this.VerifyCSharpDiagnostic(code, expected);
        }


        /*********
        ** Helpers
        *********/
        /// <summary>Get the analyzer being tested.</summary>
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new NetFieldAnalyzer();
        }
    }
}
