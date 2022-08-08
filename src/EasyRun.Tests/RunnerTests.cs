using EasyRun.Runners;

namespace EasyRun.Tests;

public class RunnerTests
{
    [Fact]
    public void Add()
    {
        var isAdded = false;
        var sut = CreateRunner(builder => builder
            .Register<Action>(() => isAdded = true));
        
        sut.Add<TestRunner>().Run();

        isAdded.Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync()
    {
        var isAdded = false;
        var sut = CreateRunner(builder => builder
            .Register<Func<CancellationToken, Task>>(_ =>
            {
                isAdded = true;
                return Task.CompletedTask;
            }));

        await sut.AddAsync<TestAsyncRunner>().RunAsync();

        isAdded.Should().BeTrue();
    }

    [Fact]
    public void TryAdd()
    {
        var addedCount = 0;
        var sut = CreateRunner(builder => builder
            .Register<Action>(() => addedCount += 1));

        sut.TryAdd<TestRunner>(out var isAdded1)
           .TryAdd<TestRunner>(out var isAdded2)
           .Run();

        isAdded1.Should().BeTrue();
        isAdded2.Should().BeFalse();
        addedCount.Should().Be(1);
    }

    [Fact]
    public async Task TryAddAsync()
    {
        var addedCount = 0;
        var sut = CreateRunner(builder => builder
            .Register<Func<CancellationToken, Task>>(_ =>
            {
                addedCount += 1;
                return Task.CompletedTask;
            }));

        await sut
            .TryAddAsync<TestAsyncRunner>(out var isAdded1)
            .TryAddAsync<TestAsyncRunner>(out var isAdded2)
            .RunAsync();

        isAdded1.Should().BeTrue();
        isAdded2.Should().BeFalse();
        addedCount.Should().Be(1);
    }

    [Fact]
    public void WithParam()
    {
        var addedValue = 0;
        new Action(() => CreateSUT().WithParam("", 0)).Should().Throw<InvalidOperationException>();
        new Action(() => CreateSUT().Add<TestRunnerWithParam>()
                                    .WithParam("unkownValue", 0))
            .Should().Throw<ArgumentException>();
        new Action(() => CreateSUT().Add<TestRunnerWithParam>()
                                    .WithParam("intValue", 1)
                                    .WithParam("intValue", 2))
            .Should().Throw<ArgumentException>();

        CreateSUT().Add<TestRunnerWithParam>().WithParam("intParam", 80).Run();

        addedValue.Should().Be(80);

        Runner CreateSUT()
        {
            return CreateRunner(builder => builder
                .Register<Action<int>>(value => addedValue = value));
        }
    }

    [Fact]
    public async Task WithParamFactory()
    {
        var addedValue = 0;
        new Action(() => CreateSUT().WithParamFactory("", () => 0)).Should().Throw<InvalidOperationException>();
        new Action(() => CreateSUT().AddAsync<TestAsyncRunnerWithParam>()
                                    .WithParamFactory("unkownValue", () => 0))
            .Should().Throw<ArgumentException>();
        new Action(() => CreateSUT().AddAsync<TestAsyncRunnerWithParam>()
                                    .WithParamFactory("intValue", () => 1)
                                    .WithParamFactory("intValue", () => 2))
            .Should().Throw<ArgumentException>();
        await CreateSUT().AddAsync<TestAsyncRunnerWithParam>()
            .WithParamFactory("intParam", () => 800)
            .RunAsync();

        addedValue.Should().Be(800);

        Runner CreateSUT()
        {
            return CreateRunner(builder => builder
                .Register<Func<int, CancellationToken, Task>>((value, _) =>
                {
                    addedValue = value;
                    return Task.CompletedTask;
                }));
        }
    }

    Runner CreateRunner(Action<RunnerBuilder> action = null)
    {
        var builder = Runner.CreateBuilder();
        action?.Invoke(builder);
        return builder.Build();
    }

    class TestRunner : IRunner
    {
        Action _action;

        public TestRunner(Action action) => _action = action;

        public void Run() => _action?.Invoke();
    }

    class TestRunnerWithParam : IRunner
    {
        int _intParam;
        Action<int> _action;

        public TestRunnerWithParam(Action<int> action, int intParam)
        {
            _action = action;
            _intParam = intParam;
        }
        

        public void Run() => _action?.Invoke(_intParam);
    }

    class TestAsyncRunner : IAsyncRunner
    {
        Func<CancellationToken, Task> _func;

        public TestAsyncRunner(Func<CancellationToken, Task> func) 
            => _func = func;

        public Task RunAsync(CancellationToken cancellationToken) 
            => _func?.Invoke(cancellationToken) ?? Task.CompletedTask;
    }

    class TestAsyncRunnerWithParam : IAsyncRunner
    {
        int _intParam;
        Func<int, CancellationToken, Task> _func;

        public TestAsyncRunnerWithParam(Func<int, CancellationToken, Task> func, int intParam)
        {
            _func = func;
            _intParam = intParam;
        }
               

        public Task RunAsync(CancellationToken cancellationToken)
            => _func?.Invoke(_intParam, cancellationToken) ?? Task.CompletedTask;
    }
}
