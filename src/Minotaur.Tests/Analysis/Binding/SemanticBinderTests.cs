/*
 * This file is part of Minotaur.
 *
 * Minotaur is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Minotaur is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with Minotaur. If not, see <https://www.gnu.org/licenses/>.
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minotaur.Analysis.Binding;
using Minotaur.Core;
using Minotaur.GrammarGeneration.Models;

namespace Minotaur.Tests.Analysis.Binding;

[TestClass]
public class SemanticBinderTests
{
    /// <summary>
    /// C#-shaped grammar annotations: namespace declarations, using directives,
    /// class declarations with a base list.
    /// </summary>
    private static Grammar CreateCSharpLikeGrammar()
    {
        return new Grammar
        {
            Name = "CSharpLike",
            Language = "csharp-like",
            Metadata = new Dictionary<string, string>
            {
                ["binding"] = """
                {
                  "scopeOpeners": ["method_body", "block"],
                  "declarations": {
                    "class_declaration": { "name": "identifier" },
                    "method_declaration": { "name": "identifier" }
                  },
                  "containers": ["namespace_declaration"],
                  "imports": {
                    "using_directive": { "import": "qualified_name" }
                  },
                  "references": {
                    "base_list": { "reference": "identifier" }
                  },
                  "typeDeclarationTokens": ["class", "interface"]
                }
                """
            }
        };
    }

    /// <summary>
    /// Java-shaped grammar annotations: package declarations, import statements,
    /// class declarations with an extends clause.
    /// </summary>
    private static Grammar CreateJavaLikeGrammar()
    {
        return new Grammar
        {
            Name = "JavaLike",
            Language = "java-like",
            Metadata = new Dictionary<string, string>
            {
                ["binding"] = """
                {
                  "declarations": {
                    "class_declaration": { "name": "identifier" }
                  },
                  "containers": ["package_declaration"],
                  "imports": {
                    "import_declaration": { "import": "qualified_name" }
                  },
                  "references": {
                    "extends_clause": { "reference": "identifier" }
                  },
                  "typeDeclarationTokens": ["class"]
                }
                """
            }
        };
    }

    /// <summary>
    /// Python-shaped grammar annotations: module-level nesting, import statements,
    /// class definitions with an inheritance list.
    /// </summary>
    private static Grammar CreatePythonLikeGrammar()
    {
        return new Grammar
        {
            Name = "PythonLike",
            Language = "python-like",
            Metadata = new Dictionary<string, string>
            {
                ["binding"] = """
                {
                  "declarations": {
                    "class_definition": { "name": "identifier" }
                  },
                  "imports": {
                    "import_statement": { "import": "dotted_name" }
                  },
                  "references": {
                    "inheritance_list": { "reference": "identifier" }
                  },
                  "typeDeclarationTokens": ["class"]
                }
                """
            }
        };
    }

    private static NonTerminalNode Text(string rule, string tokenType, string text)
    {
        var node = new NonTerminalNode(rule, 0);
        node.AddChild(new TerminalNode(text, tokenType));
        return node;
    }

    [TestMethod]
    public void Bind_CSharpLike_SameNamedClassesInDifferentNamespaces_ResolvedPerUsing()
    {
        var grammar = CreateCSharpLikeGrammar();
        var binder = new SemanticBinder("csharp-like", GrammarBindingProfile.FromGrammar(grammar));

        // File 1: namespace N1 { class A }  +  namespace N2 { class A }
        var file1 = new NonTerminalNode("compilation_unit", 0);
        var ns1 = new NonTerminalNode("namespace_declaration", 0);
        ns1.AddChild(Text("qualified_name", "identifier", "N1"));
        ns1.AddChild(CreateClassDeclaration("A", "class"));
        file1.AddChild(ns1);
        var ns2 = new NonTerminalNode("namespace_declaration", 0);
        ns2.AddChild(Text("qualified_name", "identifier", "N2"));
        ns2.AddChild(CreateClassDeclaration("A", "class"));
        file1.AddChild(ns2);
        binder.BindFile(file1, "File1.cs");

        // File 2: using N2; class B : A
        var file2 = new NonTerminalNode("compilation_unit", 0);
        var usingN2 = new NonTerminalNode("using_directive", 0);
        usingN2.AddChild(Text("qualified_name", "identifier", "N2"));
        file2.AddChild(usingN2);
        var classB = CreateClassDeclaration("B", "class");
        var baseList = new NonTerminalNode("base_list", 0);
        baseList.AddChild(Text("identifier_list", "identifier", "A"));
        classB.AddChild(baseList);
        file2.AddChild(classB);
        binder.BindFile(file2, "File2.cs");

        binder.ResolvePendingReferences();

        Assert.AreEqual(3, binder.Result.Symbols.Count, "N1.A, N2.A and B should be declared");
        var reference = binder.Result.References.Single(r => r.Name == "A");
        Assert.IsTrue(reference.IsResolved, "A should resolve through the using directive");
        Assert.AreEqual("csharp-like|N2.A", reference.ResolvedSymbolId, "A should bind to N2.A per the using directive, not N1.A");
    }

    [TestMethod]
    public void Bind_JavaLike_SameNamedClassesInDifferentPackages_ResolvedPerImport()
    {
        var grammar = CreateJavaLikeGrammar();
        var binder = new SemanticBinder("java-like", GrammarBindingProfile.FromGrammar(grammar));

        var file1 = new NonTerminalNode("compilation_unit", 0);
        var pkg1 = new NonTerminalNode("package_declaration", 0);
        pkg1.AddChild(Text("qualified_name", "identifier", "com.example.one"));
        pkg1.AddChild(CreateClassDeclaration("A", "class"));
        file1.AddChild(pkg1);
        var pkg2 = new NonTerminalNode("package_declaration", 0);
        pkg2.AddChild(Text("qualified_name", "identifier", "com.example.two"));
        pkg2.AddChild(CreateClassDeclaration("A", "class"));
        file1.AddChild(pkg2);
        binder.BindFile(file1, "File1.java");

        var file2 = new NonTerminalNode("compilation_unit", 0);
        var import = new NonTerminalNode("import_declaration", 0);
        import.AddChild(Text("qualified_name", "identifier", "com.example.two"));
        file2.AddChild(import);
        var classB = CreateClassDeclaration("B", "class");
        var extends = new NonTerminalNode("extends_clause", 0);
        extends.AddChild(Text("type", "identifier", "A"));
        classB.AddChild(extends);
        file2.AddChild(classB);
        binder.BindFile(file2, "File2.java");

        binder.ResolvePendingReferences();

        var reference = binder.Result.References.Single(r => r.Name == "A");
        Assert.IsTrue(reference.IsResolved);
        Assert.AreEqual("java-like|com.example.two.A", reference.ResolvedSymbolId, "A should bind to the imported package's A");
    }

    [TestMethod]
    public void Bind_PythonLike_SameNamedClassesInDifferentModules_ResolvedPerImport()
    {
        var grammar = CreatePythonLikeGrammar();
        var binder = new SemanticBinder("python-like", GrammarBindingProfile.FromGrammar(grammar));

        // Python has no package production; nested module blocks carry the
        // module path on a dotted_name terminal inside a container production.
        var file1 = new NonTerminalNode("module", 0);
        var mod1 = new NonTerminalNode("module_block", 0);
        mod1.AddChild(Text("dotted_name", "identifier", "models.one"));
        mod1.AddChild(CreateClassDeclaration("A", "class"));
        file1.AddChild(mod1);
        var mod2 = new NonTerminalNode("module_block", 0);
        mod2.AddChild(Text("dotted_name", "identifier", "models.two"));
        mod2.AddChild(CreateClassDeclaration("A", "class"));
        file1.AddChild(mod2);
        binder.BindFile(file1, "file1.py");

        var file2 = new NonTerminalNode("module", 0);
        var import = new NonTerminalNode("import_statement", 0);
        import.AddChild(Text("dotted_name", "dotted_name", "models.two"));
        file2.AddChild(import);
        var classB = CreateClassDeclaration("B", "class");
        var bases = new NonTerminalNode("inheritance_list", 0);
        bases.AddChild(Text("identifier_list", "identifier", "A"));
        classB.AddChild(bases);
        file2.AddChild(classB);
        binder.BindFile(file2, "file2.py");

        binder.ResolvePendingReferences();

        var reference = binder.Result.References.Single(r => r.Name == "A");
        Assert.IsTrue(reference.IsResolved);
        Assert.AreEqual("python-like|models.two.A", reference.ResolvedSymbolId, "A should bind to the imported module's A");
    }

    [TestMethod]
    public void Bind_NewGrammarWithAnnotations_RequiresNoEngineChanges()
    {
        // A "new language" (e.g. from Minotaur-Marketplace): all binding behavior
        // comes from annotations; the SemanticBinder code is untouched.
        var grammar = new Grammar
        {
            Name = "FictionalLang",
            Language = "fictional",
            Metadata = new Dictionary<string, string>
            {
                ["binding"] = """
                {
                  "declarations": {
                    "entity_declaration": { "name": "symbol_name" }
                  },
                  "containers": ["domain_declaration"],
                  "imports": {
                    "expose_directive": { "import": "domain_path" }
                  },
                  "references": {
                    "dependency_clause": { "reference": "symbol_name" }
                  },
                  "typeDeclarationTokens": ["entity"]
                }
                """
            }
        };

        var binder = new SemanticBinder("fictional", GrammarBindingProfile.FromGrammar(grammar));

        var file1 = new NonTerminalNode("unit", 0);
        var domain1 = new NonTerminalNode("domain_declaration", 0);
        domain1.AddChild(Text("domain_path", "domain_path", "Alpha"));
        var entityA = new NonTerminalNode("entity_declaration", 0);
        entityA.AddChild(new TerminalNode("entity", "keyword"));
        entityA.AddChild(new TerminalNode("A", "symbol_name"));
        domain1.AddChild(entityA);
        file1.AddChild(domain1);
        var domain2 = new NonTerminalNode("domain_declaration", 0);
        domain2.AddChild(Text("domain_path", "domain_path", "Beta"));
        var entityA2 = new NonTerminalNode("entity_declaration", 0);
        entityA2.AddChild(new TerminalNode("entity", "keyword"));
        entityA2.AddChild(new TerminalNode("A", "symbol_name"));
        domain2.AddChild(entityA2);
        file1.AddChild(domain2);
        binder.BindFile(file1, "unit1.fic");

        var file2 = new NonTerminalNode("unit", 0);
        var expose = new NonTerminalNode("expose_directive", 0);
        expose.AddChild(Text("domain_path", "domain_path", "Beta"));
        file2.AddChild(expose);
        var entityB = new NonTerminalNode("entity_declaration", 0);
        entityB.AddChild(new TerminalNode("entity", "keyword"));
        entityB.AddChild(new TerminalNode("B", "symbol_name"));
        var deps = new NonTerminalNode("dependency_clause", 0);
        deps.AddChild(Text("symbol_list", "symbol_name", "A"));
        entityB.AddChild(deps);
        file2.AddChild(entityB);
        binder.BindFile(file2, "unit2.fic");

        binder.ResolvePendingReferences();

        var reference = binder.Result.References.Single(r => r.Name == "A");
        Assert.IsTrue(reference.IsResolved, "A new grammar with annotations should bind without engine changes");
        Assert.AreEqual("fictional|Beta.A", reference.ResolvedSymbolId);
        Assert.AreEqual("entity", binder.Result.Symbols.Single(s => s.Name == "B").Kind);
    }

    [TestMethod]
    public void Bind_UnresolvedReference_ReportsUnresolved()
    {
        var grammar = CreateCSharpLikeGrammar();
        var binder = new SemanticBinder("csharp-like", GrammarBindingProfile.FromGrammar(grammar));

        var file = new NonTerminalNode("compilation_unit", 0);
        var classB = CreateClassDeclaration("B", "class");
        var baseList = new NonTerminalNode("base_list", 0);
        baseList.AddChild(Text("identifier_list", "identifier", "Missing"));
        classB.AddChild(baseList);
        file.AddChild(classB);
        binder.BindFile(file, "File.cs");

        binder.ResolvePendingReferences();

        var reference = binder.Result.References.Single();
        Assert.AreEqual("Missing", reference.Name);
        Assert.IsFalse(reference.IsResolved);
        Assert.AreEqual(1, binder.Result.UnresolvedReferences.Count());
    }

    [TestMethod]
    public void Bind_QualifiedReference_ResolvesAcrossContainers()
    {
        var grammar = CreateCSharpLikeGrammar();
        var binder = new SemanticBinder("csharp-like", GrammarBindingProfile.FromGrammar(grammar));

        var file1 = new NonTerminalNode("compilation_unit", 0);
        var ns1 = new NonTerminalNode("namespace_declaration", 0);
        ns1.AddChild(Text("qualified_name", "identifier", "N1"));
        ns1.AddChild(CreateClassDeclaration("A", "class"));
        file1.AddChild(ns1);
        binder.BindFile(file1, "File1.cs");

        var file2 = new NonTerminalNode("compilation_unit", 0);
        var classB = CreateClassDeclaration("B", "class");
        var baseList = new NonTerminalNode("base_list", 0);
        baseList.AddChild(Text("identifier_list", "identifier", "N1.A"));
        classB.AddChild(baseList);
        file2.AddChild(classB);
        binder.BindFile(file2, "File2.cs");

        binder.ResolvePendingReferences();

        var reference = binder.Result.References.Single();
        Assert.IsTrue(reference.IsResolved, "A fully qualified reference should resolve without any using directive");
        Assert.AreEqual("csharp-like|N1.A", reference.ResolvedSymbolId);
    }

    private static NonTerminalNode CreateClassDeclaration(string name, string keyword)
    {
        var node = new NonTerminalNode("class_declaration", 0);
        node.AddChild(new TerminalNode(keyword, "keyword"));
        node.AddChild(new TerminalNode(name, "identifier"));
        return node;
    }
}
