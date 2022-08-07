namespace EasyRun.Tests;

public class RunnerBuilderTests
{
    RunnerBuilder sut => Runner.CreateBuilder();
    
    [Fact]
    public void BuildAsEmpty()
    {
        sut.Build().Should().NotBeNull();
    }

    [Fact]
    public void RegisterAsType()
    {
        TestRegister<TestClass>(lifeTime => sut.Register<TestClass>(lifeTime).Build());
    }

    [Fact]
    public void RegisterAsInstance()
    {
        TestRegisterAsInstance(instance => sut.Register(instance).Build(), new TestClass());
    }

    [Fact]
    public void RegisterFactory()
    {
        TestRegister<TestClass>(lifeTime => sut.RegisterFactory(_ => new TestClass(), lifeTime).Build());
    }

    [Fact]
    public void TryRegisterAsType()
    {
        TestRegister<TestClass>(lifeTime =>
        {
            var runner = sut
            .TryRegister<TestClass>(out var isRegistered, lifeTime)
            .TryRegister<TestClass>(out var isRegisteredTwice, lifeTime)
            .Build();

            isRegistered.Should().BeTrue();
            isRegisteredTwice.Should().BeFalse();
            return runner;
        });
    }

    [Fact]
    public void TryRegisterAsInstance()
    {
        TestRegisterAsInstance(instance =>
        {
            var runner = sut
                .TryRegister(instance, out var isRegistered)
                .TryRegister(instance, out var isRegisteredTwice)
                .Build();

            isRegistered.Should().BeTrue();
            isRegisteredTwice.Should().BeFalse();
            return runner;
        }
        , new TestClass());
    }

    [Fact]
    public void TryRegisterFactory()
    {
        TestRegister<TestClass>(lifeTime =>
        {
            var runner = sut
            .TryRegisterFactory(_ => new TestClass(), out var isRegistered, lifeTime)
            .TryRegisterFactory(_ => new TestClass(), out var isRegisteredTwice, lifeTime)
            .Build();

            isRegistered.Should().BeTrue();
            isRegisteredTwice.Should().BeFalse();
            return runner;
        });
    }

    [Fact]
    public void WithParam()
    {
        new Action(() => sut.WithParam("", 0)).Should().Throw<InvalidOperationException>();
        new Action(() => sut.Register<TestClassWithParam>()
                            .WithParam("unkownValue", 0))
            .Should().Throw<ArgumentException>();
        new Action(() => sut.Register<TestClassWithParam>()
                            .WithParam("intValue", 1)
                            .WithParam("intValue", 2))
            .Should().Throw<ArgumentException>();

        var intValue = 123;
        var runner = sut.Register<TestClassWithParam>()
                   .WithParam("intValue", intValue)
               .Build();

        runner.Resolver.Resolve<TestClassWithParam>().IntValue.Should().Be(intValue);
    }

    [Fact]
    public void WithParamFactory()
    {
        new Action(() => sut.WithParamFactory("", () => 0)).Should().Throw<InvalidOperationException>();
        new Action(() => sut.Register<TestClassWithParam>()
                            .WithParamFactory("unkownValue", () => 0))
            .Should().Throw<ArgumentException>();
        new Action(() => sut.Register<TestClassWithParam>()
                            .WithParamFactory("intValue", () => 1)
                            .WithParamFactory("intValue", () => 2))
            .Should().Throw<ArgumentException>();

        var intValue = 123;
        var runner = sut.Register<TestClassWithParam>()
                   .WithParamFactory("intValue", () => intValue)
               .Build();

        runner.Resolver.Resolve<TestClassWithParam>().IntValue.Should().Be(intValue);
    }

    void TestRegister<T>(Func<LifeTime, Runner> func)
    {
        TestRegisterInternal(LifeTime.Scoped);
        TestRegisterInternal(LifeTime.Transient);
        void TestRegisterInternal(LifeTime lifeTime)
        {
            var runner = func.Invoke(lifeTime);
            var testClass = runner.Resolver.Resolve<T>();
            testClass.Should().NotBeNull();
            switch (lifeTime)
            {
                case LifeTime.Scoped:
                    runner.Resolver.Resolve<T>().Should().Be(testClass);
                    break;
                case LifeTime.Transient:
                    runner.Resolver.Resolve<T>().Should().NotBe(testClass);
                    break;
            }
        }
    }

    void TestRegisterAsInstance<T>(Func<T, Runner> func, T instance)
    {
        var runner = func.Invoke(instance);
        var testClass = runner.Resolver.Resolve<T>();
        testClass.Should().Be(instance);
        runner.Resolver.Resolve<T>().Should().Be(instance);
    }

    class TestClass { }

    class TestClassWithParam 
    {
        public readonly int IntValue;

        public TestClassWithParam(int intValue)
        {
            IntValue = intValue;
        }
    }
}