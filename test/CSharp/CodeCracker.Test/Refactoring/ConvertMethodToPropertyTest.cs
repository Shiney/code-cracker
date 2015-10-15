using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeCracker.CSharp.Refactoring;
using Microsoft.CodeAnalysis;
using Xunit;

namespace CodeCracker.Test.CSharp.Refactoring
{
    public class ConvertMethodToPropertyTest : CodeFixVerifier<ConvertMethodToPropertyAnalyzer, ConvertMethodToPropertyCodeFixProvider>
    {
        [Fact]
        public async Task IgnoresWhenNoMethods()
        {
            const string test = @"public class A{}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }


        [Fact]
        public async Task IgnoresWhenOnlyVoidMethods()
        {
            const string test = @"public class A{ 
private int a= 0;

void Increment(){ a= a +1;}

}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresWhenOnlyMethodsWithArguments()
        {

            const string test = @"public class A{ 
private int a= 0;

int Increment(int b){ reture a = a+b;}
int Increment(int b, string c){ reture a = a+b + c.Length;}


}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }


        [Fact]
        public async Task IgnoresWhenMethodWithTypeParameterButNoArguments()
        {
            const string test = @"public class A{ 
            public int LengthOfTypeName<T>{return typeof(T).FullName.Length;}


}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }


        [Fact]
        public async Task IgnoresWhenMethodOverloaded()
        {
            const string test = @"public class A
        {

            A Increment() => new A();
            int Increment(int b) => b+ 2;
        

}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresWhenMethodAsync()
        {
            const string test = @"public class A
        {

            async Task<A> Increment() {return new A();}
        

}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task DiagnosticOnMethodWithNoArgumentsNewStyle()
        {
            const string test = @"public class A{ 

A Increment() => new A();
}";
            var expected = CreateExpectedDiagnostic(3, 1);

            await VerifyCSharpDiagnosticAsync(test, expected);


        }

        [Fact]
        public async Task DiagnosticOnMethodWithNoArgumentsOldStyle()
        {
            const string test = @"public class A{ 
private int a= 0;

int Increment() {return a++;}

}";
            var expected = CreateExpectedDiagnostic(4, 1);

            await VerifyCSharpDiagnosticAsync(test, expected);
        }



        private static DiagnosticResult CreateExpectedDiagnostic(int line, int column)
        {
            return new DiagnosticResult
            {
                Id = DiagnosticId.ConvertMethodToProperty.ToDiagnosticId(),
                Message = "Convert this method to a property",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", line, column) }
            };
        }

        [Fact]
        public async Task FixReplacesOneMethodInOneTypeWithProperty()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                return 10;
            }
        }
    }";

            const string expected = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo
            {
                get
                {
                    return 10;
                }
            }
        }
    }";
            await VerifyCSharpFixAllAsync(test, expected);
        }

        [Fact]
        public async Task FixReplacesOneMethodInAbstractClassWithProperty()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        abstract class TypeName
        {
            public abstract int Foo();
        }
    }";

            const string expected = @"
    using System;

    namespace ConsoleApplication1
    {
        abstract class TypeName
        {
            public abstract int Foo {get;}
        }
    }";
            await VerifyCSharpFixAllAsync(test, expected);
        }

        [Fact]
        public async Task FixReplacesOneMethodInInterfaceWithProperty()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        interface TypeName
        {
            int Foo();
        }
    }";

            const string expected = @"
    using System;

    namespace ConsoleApplication1
    {
        interface TypeName
        {
            int Foo {get;}
        }
    }";
            await VerifyCSharpFixAllAsync(test, expected);
        }

        [Fact]
        public async Task FixReplacesExpressionBodiedMethodWithProperty()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            internal static int Foo() => return 9 * 7;
        }
    }";

            const string expected = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            internal static int Foo => return 9 * 7;
        }
    }";
            await VerifyCSharpFixAllAsync(test, expected);
        }


        //TODO
        //Cannot change if overload in supertype/subtype
        //Change interface
        //Change references
        //Change interface/abstract and current? or don't support/
        //Reference as delegate should be changed.#
        //Change reference if used as delegate.
    }
}
