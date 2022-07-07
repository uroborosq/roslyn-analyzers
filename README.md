# Rolsyn analyzers

Репозиторий представляет два решения - два анализатора исходного кода, которые работают только под VS. 

### ReverseBoolNames

Анализатор ищет локальные объявления и объявление параметров булевого типа, которые начинаются с not. \
Пример:
``` c#
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
            var notHello = !odd;

            var gh = ReturnBool(notHello);

            if(!notHello)
                Console.WriteLine(gh);
        }

        static bool ReturnBool(bool today)
        {
            return today;
        }
    }
}
```

В данном примере есть идентификатор, попадающие под условия анализатора - notHello  \
Анализатор предложит заменить его на просто hello, и обратит его использование в теле функции.

``` c#
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
}
```
--- 
### NotOverloadedOperatorsAnalyzer

Анализатор находит использование неперегруженного оператора == и предлагает заменить его на вызов Equals()

Пример
``` c#
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
}
```

В примере сравниваются два объекта типа TestClass, у которого не перегружен оператор сравнения.

Пример исправленного кода:

``` c#
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
}
```

Анализатор не будет бросать диагностику, если сравниваются интерфейс и его имплементация, суперкласс и наследник.

