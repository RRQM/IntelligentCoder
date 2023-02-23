# IntelligentCoder

## 一、介绍

这是一个能辅助你实现异步代码的生成器。他可以在接口，类，结构体上工作。有时候甚至还能给已编译的方法扩展异步。

例如：对于TestClass而言，Run方法是比较耗时的。

```
public class TestClass
{
    public void Run()
    {
        Thread.Sleep(2000);
    }
}
```

所以应当提供异步方法

```
public class TestClass
{
    public void Run()
    {
        Thread.Sleep(2000);
    }


    public Task RunAsync()
    {
        return Task.Run(() =>
         {
             this.Run();
         });
    }
}

```
而IntelligentCoder的作用，就是替你自动生成异步的代码。

```
public partial class TestClass
{
    public Task RunAsync()
    {
        return Task.Run(() =>
         {
             this.Run();
         });
    }
}

```

## 二、安装

（1）你需要安装IntelligentCoder的包。

[![NuGet(IntelligentCoder)](https://img.shields.io/nuget/v/IntelligentCoder.svg?label=IntelligentCoder)](https://www.nuget.org/packages/IntelligentCoder/)

```
NuGet\Install-Package IntelligentCoder
```

（2）在即将使用的地方，添加引用命名空间。

```
using IntelligentCoder;
```

## 三、使用场景

#### 3.1 常规类

对于常规类而言，直接为其添加AsyncMethodPoster标签即可。
```
[AsyncMethodPoster]
internal partial class TestClass1
{
    public int Add1()
    {
        return 0;
    }

    public void Add2()
    {

    }

    [AsyncMethod(Template = "My{0}Async")]//测试模板生成
    public void Add3()
    {

    }
}
```

他会生成直接的异步代码
```
TestClass1 testClass1 = new TestClass1();
testClass1.Add1();
testClass1.Add1Async();
testClass1.Add2();
testClass1.Add2Async();
testClass1.Add3();
testClass1.MyAdd3Async();
```

#### 3.2 接口+实现类

对于接口，他需要实现类实现所有方法，所以必须在接口和实现类中都添加标签。其次，在实现类中，必须指定`Deep`属性标签，用于深度搜索接口。该值是int类型，表示深度层次。然后他会自动补充异步方法。

```
[AsyncMethodPoster]
public partial interface TestInterface
{
    /// <summary>
    /// 添加
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    int Add(int a, int b);
}

[AsyncMethodPoster(Deep = 10)]
public partial class TestInterfaceImp : TestInterface
{
    public int Add(int a, int b)
    {
        return a + b;
    }

    public void Add2<T>(int a, int b)
    {
        
    }
}
```

#### 3.3 扩展实例调用类

上述的两个方式，仅仅是用于当前编译时。而往往，更多时候是某个代码已经被编译，所以我们需要为其生成异步扩展类代码。

例如：我们已经有个类MyClass3，已被编译。

```
public class MyClass3
{
    public int Add3(int a, int b)
    {
        return a + b;
    }
}
```

那么我们可以新建项目，然后声明一个扩展类，指向MyClass3，那么这个扩展类就会为MyClass3，生成异步扩展方法。

```
[AsyncMethodPoster(Target = typeof(MyClass3))]
static partial class MyClass3Extension
{

}
```
下列Add3Async就是扩展方法。

```
MyClass3 myClass3=new MyClass3();
myClass3.Add3Async(10,20);
```

#### 3.4 扩展调用静态类

扩展静态类，操作和实例类一致，但是需要注意的是，扩展后，必须通过生成的扩展类调用。

```
[AsyncMethodPoster(Target = typeof(TestStaticClass))]
public static partial class TestStaticClassExtension
{

}

public static class TestStaticClass
{
    public static int Add(int a, int b)
    {
        return a + b;
    }
}
```

```
TestStaticClassExtension.AddAsync(10,20);
```

## 四、其他

除了上述功能，他还支持异步方法的模板。

```
[AsyncMethodPoster(Template ="My{0}Async")]
partial interface IA
{
    int Add(int a, int b);
}
```

和条件的预编译。

```
[AsyncMethodPoster(Precompile ="!NET45")]
partial interface IA
{
    int Add(int a, int b);
}
```

## 五、案例

#### 5.1 对File类扩展

众所周知，File类是CSharp里面的最重要的静态类之一，但是遗憾的是，他并没有提供任何异步方法，这使得我们在异步使用时非常麻烦，所以需要扩展异步。

您只需要将下列5行代码复制到你的项目，你即可拥有FileAsync的异步类。

```
[AsyncMethodPoster(Target = typeof(System.IO.File))]
static partial class FileAsync
{

}
```

```
string path = "path";
FileAsync.CreateAsync(path);
FileAsync.OpenAsync(path, FileMode.Open);
```




如果你有其他好的想法，我们可以一起交流哦。