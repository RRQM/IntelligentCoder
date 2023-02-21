# IntelligentCoder

## 介绍

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

又例如：对于A接口，在添加标识**AsyncMethodPoster**后，他会自动补充异步方法。

```
[AsyncMethodPoster]
partial interface IA
{
    int Add(int a, int b);
}
```

```
partial interface IA
{
   Task<int> AddAsync(int a,int b);
}
```

## 使用场景

在目前的编程流行写法中，使用异步编程，已经是一个非常普遍的事，所以我们在开发程序时，可能需要写同步，异步两种方法。这样才能方便其他人调用。

一般的，当我们写好一个同步方法后，会用一个Task封装一下，变成一个异步方法。目前来说，我最近的工作就是这个，但是太枯燥了，尤其是面对有接口实现的业务，得先加异步接口，然后再加异步实现。于是我就开发了这个工具库。他可以帮你自动的完善你的异步调用。

不仅如此，如果，你有些业务代码已经被编译成dll，那么他还可以生成扩展异步调用。能更加方便的使用。

## 使用说明

这个库可以帮你自动实现异步方法的接口，实现或者扩展方法。

（1）你需要安装IntelligentCoder的包。


[![NuGet(IntelligentCoder)](https://img.shields.io/nuget/v/IntelligentCoder.svg?label=IntelligentCoder)](https://www.nuget.org/packages/IntelligentCoder/)

```
NuGet\Install-Package IntelligentCoder
```

（2）需要在即将使用的地方，添加引用命名空间。

```
using IntelligentCoder;
```

（3）对需要实现异步的接口，类，或者结构体上面添加标签。

```
[AsyncMethodPoster]
partial interface IA
{
    int Add(int a, int b);
}
```

一般的，如果接口添加了标签，实现类应该也添加。

```
[AsyncMethodPoster]
partial class AClass : IA
{
    public int Add(int a, int b)
    {
        return a + b;
    }
}
```

最终，所有操作在vs的源代码生成中，会呈现出一个异步方法。可以直接用于调用。

```
IA @interface = new AClass();
int c = await @interface.AddAsync(10, 20);
```



## 扩展调用功能

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

## 其他

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




如果你有其他好的想法，我们可以一起交流哦。












