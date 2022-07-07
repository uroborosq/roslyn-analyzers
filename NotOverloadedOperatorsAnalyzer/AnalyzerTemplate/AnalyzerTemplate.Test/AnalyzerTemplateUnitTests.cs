using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = AnalyzerTemplate.Test.CSharpCodeFixVerifier<
    AnalyzerTemplate.AnalyzerTemplateAnalyzer,
    AnalyzerTemplate.AnalyzerTemplateCodeFixProvider>;

namespace AnalyzerTemplate.Test
{
    [TestClass]
    public class AnalyzerTemplateUnitTest
    {
        [TestMethod]
        public async Task OperatorNotOverloaded_ReplacedWithEquals()
        {
            const string test = @"
public class Program
{
    public static void Main() 
    {
        var a = new TestClass(1);
        var b = new TestClass(2);
        var c = {|#0:a|} == b;
    }
}

public class TestClass
{
    private int _int;
    
    public TestClass(int a)
    {
        _int = a;
    }
}";

            const string fixTest = @"
public class Program
{
    public static void Main() 
    {
        var a = new TestClass(1);
        var b = new TestClass(2);
        var c = a.Equals(b);
    }
}

public class TestClass
{
    private int _int;
    
    public TestClass(int a)
    {
        _int = a;
    }
}";

            var expected = VerifyCS.Diagnostic("AnalyzerTemplate").WithLocation(0).WithArguments("TestClass and TestClass");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixTest);
        }
        
        
        [TestMethod]
        public async Task OperatorOverriden_DiagnosticDontThrow()
        {
            const string test = @"
using System;
public class Program
{
    public static void Main() 
    {
        var a = new TestClass(1);
        var b = new TestClass(2);
        var c = a == b;
    }
}

public class TestClass
{
    private int _int;

    public TestClass(int a)
    {
        _int = a;
    }
    public static bool operator ==(TestClass a, TestClass b)
    {
        return true;
    }
    public static bool operator !=(TestClass a, TestClass b)
    {
        return false;
    }
}
    ";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
        
        [TestMethod]
        public async Task ComparingWithInterfaceImplementation_DiagnosticDontThrow()
        {
            const string test = @"using System;
public class Program
{
    public static void Main() 
        {
            var a = new TestClass(1);
            ITestInterface b = new TestClass(2);
            var c = a == b;
        }
    }

    public interface ITestInterface
    {
        public int SomeDigit { get; }
    }
    

    public class TestClass : ITestInterface
    {
        private int _int;
    
        public TestClass(int a)
        {
            _int = a;
        }
        
        public int SomeDigit => _int;
    
        public static bool operator ==(TestClass a, ITestInterface b)
        {
            return true;
        }
        public static bool operator !=(TestClass a, ITestInterface b)
        {
            return false;
        }
    }
    ";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task ComparingWithChildClass_DiagnosticDontThrown()
        {
            const string source = @"
using System;
public class Program
{
    public static void Main() 
    {
        var a = new TestClass(1);
        var b = new ChildTestClass(2);
        var c = a == b;
    }
}

public class TestClass
{
    private int _int;
    
    public TestClass(int a)
    {
        _int = a;
    }
        
    public int SomeDigit => _int;
    
    public static bool operator ==(TestClass a, TestClass b)
    {
        return true;
    }
    public static bool operator !=(TestClass a, TestClass b)
    {
        return false;
    }
}

public class ChildTestClass : TestClass
{
    public ChildTestClass(int a) : base(a)
    {
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(source);
        }
    }
}
