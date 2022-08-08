using EasyRun.Runners;

namespace EasyRun.Tests;
public class RunTests
{
    [Fact]
    public void Run()
    {
        var sut = CreateRunner();

        sut.Add<TestRunner>().Run();
        sut.TryAdd<TestRunner>(out var isAdded);

        CheckRun(sut);
        isAdded.Should().BeFalse();
    }

    [Fact]
    public void RunDirect()
    {
        var sut = CreateRunner();

        sut.RunDirect<TestRunner>();
        sut.TryAdd<TestRunner>(out var isAdded);

        CheckRun(sut);
        isAdded.Should().BeTrue();
    }

    [Fact]
    public void RunDirectWithParam()
    {
        var sut = CreateRunner();

        sut.RunDirect<TestRunner>(option => option.AddParam("runCount", 2));

        CheckRun(sut, 2);
    }

    [Fact]
    public void RunDirectWithParamFactory()
    {
        var sut = CreateRunner();

        sut.RunDirect<TestRunner>(option => option.AddParamFactory("runCount", () => 3));

        CheckRun(sut, 3);
    }

    [Fact]
    public void RunTemp()
    {
        var sut = CreateRunner();

        sut.RunTemp(runner => runner.Add<TestRunner>().WithParam("runCount", 4));
        sut.TryAdd<TestRunner>(out var isAdded);

        CheckRun(sut, 4);
        isAdded.Should().BeTrue();
    }

    [Fact]
    public void RunSub()
    {
        var sut = CreateRunner();

        sut.RunSub(builder => builder.Register<TestClassBonus>(),
                   runner => runner.Add<TestRunner>().WithParam("runCount", 5));
        sut.TryAdd<TestRunner>(out var isAdded);

        CheckRun(sut, 6);
        isAdded.Should().BeTrue();
        sut.Resolver.TryResolve<TestClassBonus>(out _).Should().BeFalse();
    }

    [Fact]
    public void StaticRun()
    {
        bool hasRun = false;
        Runner.StaticRun(builder => builder.Register<TestClass>(),
                         runner => runner.Add<TestRunner>()
                                         .WithParam<Action>("customAction", () => hasRun = true));

        hasRun.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync()
    {
        var sut = CreateRunner();

        await sut.AddAsync<TestRunner>().RunAsync();
        sut.TryAddAsync<TestRunner>(out var isAdded);

        CheckRun(sut);
        isAdded.Should().BeFalse();
    }

    [Fact]
    public async Task RunDirectAsync()
    {
        var sut = CreateRunner();

        await sut.RunDirectAsync<TestRunner>();
        sut.TryAddAsync<TestRunner>(out var isAdded);

        CheckRun(sut);
        isAdded.Should().BeTrue();
    }

    [Fact]
    public async Task RunDirectAsyncWithParam()
    {
        var sut = CreateRunner();

        await sut.RunDirectAsync<TestRunner>(option => option.AddParam("runCount", 2));

        CheckRun(sut, 2);
    }

    [Fact]
    public async Task RunDirectAsyncWithParamFactory()
    {
        var sut = CreateRunner();

        await sut.RunDirectAsync<TestRunner>(option => option.AddParamFactory("runCount", () => 3));

        CheckRun(sut, 3);
    }

    [Fact]
    public async Task RunTempAsync()
    {
        var sut = CreateRunner();

        await sut.RunTempAsync(runner => runner.AddAsync<TestRunner>().WithParam("runCount", 4));
        sut.TryAddAsync<TestRunner>(out var isAdded);

        CheckRun(sut, 4);
        isAdded.Should().BeTrue();
    }

    [Fact]
    public async Task RunSubAsync()
    {
        var sut = CreateRunner();

        await sut.RunSubAsync(builder => builder.Register<TestClassBonus>(),
                              runner => runner.AddAsync<TestRunner>().WithParam("runCount", 5));
        sut.TryAddAsync<TestRunner>(out var isAdded);

        CheckRun(sut, 6);
        isAdded.Should().BeTrue();
        sut.Resolver.TryResolve<TestClassBonus>(out _).Should().BeFalse();
    }

    [Fact]
    public async Task StaticRunAsync()
    {
        bool hasRun = false;
        await Runner.StaticRunAsync(builder => builder.Register<TestClass>(),
                                    runner => runner.AddAsync<TestRunner>()
                                                    .WithParam<Action>("customAction", () => hasRun = true));

        hasRun.Should().BeTrue();
    }

    Runner CreateRunner()
    {
        return Runner.CreateBuilder()
            .Register<TestClass>()
            .Build();
    }

    void CheckRun(Runner runner, int runCount = 1)
    {
        runner.Resolver.Resolve<TestClass>().RunCount.Should().Be(runCount);
    }

    class TestClass 
    {
        public int RunCount { get; private set; }
        public void OnRun(int runCount) => RunCount += runCount;
    }

    class TestClassBonus
    {
        public readonly int Bonus = 1;
    }

    class TestRunner : IRunner, IAsyncRunner
    {
        readonly TestClass _testClass;
        readonly int _runCount;
        readonly Action? _customAction;
        readonly TestClassBonus? _bonus;

        public TestRunner(TestClass testClass, int runCount = 1, Action? customAction = null, TestClassBonus? bonus = null)
        {
            _testClass = testClass;
            _runCount = runCount;
            _customAction = customAction;
            _bonus = bonus;
        }

        public void Run()
        {
            _customAction?.Invoke();
            _testClass.OnRun(_runCount + (_bonus?.Bonus ?? 0));
        }

        public Task RunAsync(CancellationToken cancellationToken = default)
        {
            Run();
            return Task.CompletedTask;
        }
    }
}
