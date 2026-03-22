using System;
using FluentAssertions;
using Primo.MIA.Common;
using Xunit;

namespace Primo.MIA.Tests.Common
{
    public class TupleHelperTests
    {
        [Fact(DisplayName = "IsValueTuple: null and classic tuple are handled correctly")]
        public void IsValueTuple_NullAndClassicTuple_ReturnsExpectedResult()
        {
            TupleHelper.IsValueTuple(null).Should().BeFalse();
            TupleHelper.IsValueTuple(System.Tuple.Create("a", "b")).Should().BeFalse();
            TupleHelper.IsValueTuple(System.ValueTuple.Create("a", "b")).Should().BeTrue();
        }

        [Fact(DisplayName = "IsClassicTuple: null and value tuple are handled correctly")]
        public void IsClassicTuple_NullAndValueTuple_ReturnsExpectedResult()
        {
            TupleHelper.IsClassicTuple(null).Should().BeFalse();
            TupleHelper.IsClassicTuple(System.Tuple.Create("a", "b")).Should().BeTrue();
            TupleHelper.IsClassicTuple(System.ValueTuple.Create("a", "b")).Should().BeFalse();
        }

        [Fact(DisplayName = "GetArity: returns tuple size and rejects non-tuples")]
        public void GetArity_ReturnsExpectedValue()
        {
            TupleHelper.GetArity(null).Should().Be(-1);
            TupleHelper.GetArity("not a tuple").Should().Be(-1);
            TupleHelper.GetArity(System.Tuple.Create("a")).Should().Be(1);
            TupleHelper.GetArity(System.Tuple.Create("a", "b", "c")).Should().Be(3);
            TupleHelper.GetArity(System.ValueTuple.Create("a", "b", "c", "d")).Should().Be(4);
        }

        [Fact(DisplayName = "GetItem: reads classic tuple items and returns null for missing ones")]
        public void GetItem_ClassicTuple_ReturnsExpectedValues()
        {
            var tuple = System.Tuple.Create("first", 2);

            TupleHelper.GetItem(tuple, 1).Should().Be("first");
            TupleHelper.GetItem(tuple, 2).Should().Be(2);
            TupleHelper.GetItem(tuple, 3).Should().BeNull();
        }

        [Fact(DisplayName = "GetItem: reads value tuple items")]
        public void GetItem_ValueTuple_ReturnsExpectedValues()
        {
            var tuple = System.ValueTuple.Create("first", 2);

            TupleHelper.GetItem(tuple, 1).Should().Be("first");
            TupleHelper.GetItem(tuple, 2).Should().Be(2);
        }

        [Fact(DisplayName = "CreateFromArray: creates classic tuple")]
        public void CreateFromArray_ClassicTuple_ReturnsExpectedTuple()
        {
            var tuple = TupleHelper.CreateFromArray(new object[] { "alpha", 10 }, 2);

            tuple.Should().BeOfType<System.Tuple<object, object>>();
            var typedTuple = tuple.Should().BeOfType<System.Tuple<object, object>>().Subject;
            typedTuple.Item1.Should().Be("alpha");
            typedTuple.Item2.Should().Be(10);
        }

        [Fact(DisplayName = "CreateFromArray: creates value tuple")]
        public void CreateFromArray_ValueTuple_ReturnsExpectedTuple()
        {
            var tuple = TupleHelper.CreateFromArray(new object[] { "alpha", 10 }, 2, useValueTuple: true);

            tuple.Should().BeOfType<System.ValueTuple<object, object>>();
            var typedTuple = tuple.Should().BeOfType<System.ValueTuple<object, object>>().Subject;
            typedTuple.Item1.Should().Be("alpha");
            typedTuple.Item2.Should().Be(10);
        }

        [Fact(DisplayName = "CreateFromArray: unsupported arity throws")]
        public void CreateFromArray_UnsupportedArity_Throws()
        {
            Action act = () => TupleHelper.CreateFromArray(new object[] { 1, 2, 3, 4, 5, 6, 7, 8 }, 8);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Арность 8 не поддерживается*");
        }

        [Fact(DisplayName = "WithItem: replaces classic tuple item without mutating original")]
        public void WithItem_ClassicTuple_ReturnsNewTuple()
        {
            var tuple = System.Tuple.Create("old", 42);

            var result = TupleHelper.WithItem(tuple, 1, "new");

            result.Should().BeOfType<System.Tuple<object, object>>();
            var typedTuple = result.Should().BeOfType<System.Tuple<object, object>>().Subject;
            typedTuple.Item1.Should().Be("new");
            typedTuple.Item2.Should().Be(42);
            tuple.Item1.Should().Be("old");
        }

        [Fact(DisplayName = "WithItem: replaces value tuple item without mutating original")]
        public void WithItem_ValueTuple_ReturnsNewTuple()
        {
            var tuple = System.ValueTuple.Create("old", 42);

            var result = TupleHelper.WithItem(tuple, 2, 99);

            result.Should().BeOfType<System.ValueTuple<object, object>>();
            var typedTuple = result.Should().BeOfType<System.ValueTuple<object, object>>().Subject;
            typedTuple.Item1.Should().Be("old");
            typedTuple.Item2.Should().Be(99);
            tuple.Item2.Should().Be(42);
        }

        [Fact(DisplayName = "WithItem: invalid item number throws")]
        public void WithItem_InvalidItemNumber_Throws()
        {
            var tuple = System.Tuple.Create("first", "second");

            Action act = () => TupleHelper.WithItem(tuple, 3, "third");

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Item3*арности 2*");
        }
    }
}
