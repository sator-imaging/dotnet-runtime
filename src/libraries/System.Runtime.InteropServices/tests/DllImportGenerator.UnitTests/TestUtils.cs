﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DllImportGenerator.UnitTests
{
    internal static class TestUtils
    {
        /// <summary>
        /// Assert the pre-srouce generator compilation has only
        /// the expected failure diagnostics.
        /// </summary>
        /// <param name="comp"></param>
        public static void AssertPreSourceGeneratorCompilation(Compilation comp)
        {
            var compDiags = comp.GetDiagnostics();
            foreach (var diag in compDiags)
            {
                Assert.True(
                    "CS8795".Equals(diag.Id)        // Partial method impl missing
                    || "CS0234".Equals(diag.Id)     // Missing type or namespace - GeneratedDllImportAttribute
                    || "CS0246".Equals(diag.Id)     // Missing type or namespace - GeneratedDllImportAttribute
                    || "CS8019".Equals(diag.Id));   // Unnecessary using
            }
        }

        /// <summary>
        /// Create a compilation given source
        /// </summary>
        /// <param name="source">Source to compile</param>
        /// <param name="outputKind">Output type</param>
        /// <param name="allowUnsafe">Whether or not use of the unsafe keyword should be allowed</param>
        /// <returns>The resulting compilation</returns>
        public static async Task<Compilation> CreateCompilation(string source, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary, bool allowUnsafe = true)
        {
            var (mdRefs, ancillary) = GetReferenceAssemblies();

            return CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)) },
                (await mdRefs.ResolveAsync(LanguageNames.CSharp, CancellationToken.None)).Add(ancillary),
                new CSharpCompilationOptions(outputKind, allowUnsafe: allowUnsafe));
        }

        /// <summary>
        /// Create a compilation given source and reference assemblies
        /// </summary>
        /// <param name="source">Source to compile</param>
        /// <param name="referenceAssemblies">Reference assemblies to include</param>
        /// <param name="outputKind">Output type</param>
        /// <param name="allowUnsafe">Whether or not use of the unsafe keyword should be allowed</param>
        /// <returns>The resulting compilation</returns>
        public static async Task<Compilation> CreateCompilationWithReferenceAssemblies(string source, ReferenceAssemblies referenceAssemblies, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary, bool allowUnsafe = true)
        {
            return CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)) },
                (await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None)),
                new CSharpCompilationOptions(outputKind, allowUnsafe: allowUnsafe));
        }

        public static (ReferenceAssemblies, MetadataReference) GetReferenceAssemblies()
        {
            // TODO: When .NET 5.0 releases, we can simplify this.
            var referenceAssemblies = ReferenceAssemblies.Net.Net50;

            // Include the assembly containing the new attribute and all of its references.
            // [TODO] Remove once the attribute has been added to the BCL
            var attrAssem = typeof(GeneratedDllImportAttribute).GetTypeInfo().Assembly;

            return (referenceAssemblies, MetadataReference.CreateFromFile(attrAssem.Location));
        }

        /// <summary>
        /// Run the supplied generators on the compilation.
        /// </summary>
        /// <param name="comp">Compilation target</param>
        /// <param name="diagnostics">Resulting diagnostics</param>
        /// <param name="generators">Source generator instances</param>
        /// <returns>The resulting compilation</returns>
        public static Compilation RunGenerators(Compilation comp, out ImmutableArray<Diagnostic> diagnostics, params ISourceGenerator[] generators)
        {
            CreateDriver(comp, generators).RunGeneratorsAndUpdateCompilation(comp, out var d, out diagnostics);
            return d;
        }

        private static GeneratorDriver CreateDriver(Compilation c, params ISourceGenerator[] generators)
            => CSharpGeneratorDriver.Create(
                ImmutableArray.Create(generators),
                parseOptions: (CSharpParseOptions)c.SyntaxTrees.First().Options);
    }
}
