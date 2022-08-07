using EasyRun.Registrations;
using EasyRun.Resolvers;

namespace EasyRun.Tests
{
    public class ObjectResolverTests
    {
        [Fact]
        public void Resolve()
        {
            var sut = CreateResolver(builder => builder
                .Register<TestClass>()
                .Register<TestClassDepended>());

            sut.Resolve<TestClass>().Should().NotBeNull();
            sut.Resolve<TestClassDepended>().Depended
                .Should().Be(sut.Resolve<TestClass>());
        }

        [Fact]
        public void TryResolve()
        {
            var sut = CreateResolver(builder => builder
                .Register<TestClass>()
                .Register<TestClassDepended>());

            sut.TryResolve<TestNoClass>(out _).Should().BeFalse();
            sut.TryResolve<TestClass>(out var testClass).Should().BeTrue();
            sut.TryResolve<TestClassDepended>(out var testClassDepended).Should().BeTrue();
            testClassDepended.Depended.Should().Be(testClass);
        }

        [Fact]
        public void InjectField()
        {
            var sut = CreateResolver(builder => builder.Register<TestClass>());

            var asField = new TestClassDependedAsField();
            sut.Inject(asField);
            asField.Depended.Should().Be(sut.Resolve<TestClass>());
        }

        [Fact]
        public void InjectProperty()
        {
            var sut = CreateResolver(builder => builder.Register<TestClass>());

            var asProperty = new TestClassDependedAsProperty();
            sut.Inject(asProperty);
            asProperty.Depended.Should().Be(sut.Resolve<TestClass>());
        }

        [Fact]
        public void InjectPropertyInherited()
        {
            var sut = CreateResolver(builder => builder
                .Register<TestClass>()
                .Register<TestClassDepended>(LifeTime.Transient));

            var asProperty = new TestClassDependedAsProperty();
            var asPropertyDerived = new TestClassDependedAsPropertyDerived();
            sut.Inject(asProperty);
            sut.Inject(asPropertyDerived);
            asProperty.Depended2.IntValue.Should().Be(1);
            asPropertyDerived.Depended2.IntValue.Should().Be(3);
        }

        [Fact]
        public void InjectMethod()
        {
            var sut = CreateResolver(builder => builder
                .Register<TestClass>()
                .Register<TestClassDepended>());

            var asMethod = new TestClassDependedAsMethod();
            sut.Inject(asMethod);
            asMethod.Depended.Should().Be(sut.Resolve<TestClass>());
        }

        [Fact]
        public void InjectMethodInherited()
        {
            var sut = CreateResolver(builder => builder
                .Register<TestClass>()
                .Register<TestClassDepended>(LifeTime.Transient));

            var asMethod = new TestClassDependedAsMethod();
            var asMethodDerived = new TestClassDependedAsMethodDerived();
            sut.Inject(asMethod);
            sut.Inject(asMethodDerived);
            asMethod.Depended2.IntValue.Should().Be(10);
            asMethodDerived.Depended2.IntValue.Should().Be(30);
        }

        IObjectResolver CreateResolver(Action<IDependencyRegistration> action)
        {
            var builder = Runner.CreateBuilder();
            action(builder);
            return builder.Build().Resolver;
        }

        class TestNoClass { }
        class TestClass { }
        class TestClassDepended
        {
            public readonly TestClass Depended;
            public int IntValue;

            public TestClassDepended(TestClass depended)
            {
                Depended = depended;
            }
        }

        class TestClassDependedAsField
        {
            [Injected]
            private TestClass _depended;
            public TestClass Depended => _depended;
        }

        class TestClassDependedAsProperty
        {
            TestClassDepended _testClassDepended;

            [Injected]
            public TestClass Depended { get; private set; }

            [Injected]
            public virtual TestClassDepended Depended2 
            {
                get => _testClassDepended;
                protected set
                {
                    _testClassDepended = value;
                    _testClassDepended.IntValue += 1;
                }
                
            }
        }

        class TestClassDependedAsPropertyDerived : TestClassDependedAsProperty
        {
            public override TestClassDepended Depended2
            {
                get => base.Depended2;
                protected set
                {
                    base.Depended2 = value;
                    base.Depended2.IntValue += 2;
                }
            }
        }

        class TestClassDependedAsMethod
        {
            TestClassDepended _depended2;

            public TestClass Depended { get; private set; }

            public TestClassDepended Depended2 => _depended2;

            [Injected]
            void OnInject(TestClass depended)
            {
                Depended = depended;
            }

            [Injected]
            protected virtual void OnInjected2(TestClassDepended depended2)
            {
                _depended2 = depended2;
                _depended2.IntValue += 10;
            }
        }

        class TestClassDependedAsMethodDerived : TestClassDependedAsMethod
        {
            protected override void OnInjected2(TestClassDepended depended2)
            {
                base.OnInjected2(depended2);
                Depended2.IntValue += 20;
            }
        }
    }
}
