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
        public async Task NoVariablesBeginningWithNot_NoDiagnostic()
        {
            const string test = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class Program
    {   
        static void Main(string[] args, bool goodOne)
        {
            bool odd = false;
            var hello = odd;

            var gh = ReturnBool(!hello);

            if(hello)
                Console.WriteLine(gh);
        }

        static bool ReturnBool(bool today)
        {
            return today;
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task LocalNameBeginningWithNot_NameChangedUsagesReversed()
        {
            const string test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class Program
    {   
        static void Main(string[] args)
        {
            bool odd = false;
            var {|#0:notHello|} = !odd;

            var gh = ReturnBool(notHello);

            if(!notHello)
                Console.WriteLine(gh);
        }

        static bool ReturnBool(bool today)
        {
            return today;
        }
    }
}";

            const string fixedSource = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class Program
    {   
        static void Main(string[] args)
        {
            bool odd = false;
            var hello = odd;

            var gh = ReturnBool(!hello);

            if(hello)
                Console.WriteLine(gh);
        }

        static bool ReturnBool(bool today)
        {
            return today;
        }
    }
}";


            var expected = VerifyCS.Diagnostic("AnalyzerTemplate").WithLocation(0).WithArguments("notHello");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixedSource);
        }
 
        [TestMethod]
        public async Task ParameterNameBeginningWithNot_NameChangedUsagesReversed()
        {
            const string test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class Program
    {   
        static void Main(string[] args)
        {
            bool odd = false;
        }

        static bool ReturnBool(bool {|#0:notHello|})
        {
            var gh = ReturnBool(notHello);

            if(!notHello)
                Console.WriteLine(gh);
            return true;
        }
    }
}";

            const string fixedSource = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class Program
    {   
        static void Main(string[] args)
        {
            bool odd = false;
        }

        static bool ReturnBool(bool hello)
        {
            var gh = ReturnBool(!hello);

            if(hello)
                Console.WriteLine(gh);
            return true;
        }
    }
}";


            var expected = VerifyCS.Diagnostic("AnalyzerTemplate").WithLocation(0).WithArguments("notHello");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixedSource);
        }
    }
}
