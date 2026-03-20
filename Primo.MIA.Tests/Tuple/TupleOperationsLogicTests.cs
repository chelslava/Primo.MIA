using System;
using System.Collections.Generic;
using FluentAssertions;
using Primo.MIA;
using Primo.MIA.Tests.Logic;
using Xunit;

namespace Primo.MIA.Tests.Tuple
{
    public class TupleOperationsLogicTests
    {
        private readonly TupleOperationsLogic _logic;

        public TupleOperationsLogicTests()
        {
            _logic = new TupleOperationsLogic();
        }

        #region CreateTuple Tests

        [Fact]
        public void CreateTuple_TwoElements_CreatesCorrectly()
        {
            // Arrange
            var values = new List<object> { "apple", 42 };

            // Act
            var result = _logic.CreateTuple(values, TupleKind.ClassicTuple);

            // Assert
            result.Should().NotBeNull();
            var tuple = result as System.Tuple<object, object>;
            tuple.Should().NotBeNull();
            tuple.Item1.Should().Be("apple");
            tuple.Item2.Should().Be(42);
        }

        [Fact]
        public void CreateTuple_ThreeElements_CreatesCorrectly()
        {
            // Arrange
            var values = new List<object> { "a", "b", "c" };

            // Act
            var result = _logic.CreateTuple(values, TupleKind.ClassicTuple);

            // Assert
            result.Should().NotBeNull();
            var tuple = result as System.Tuple<object, object, object>;
            tuple.Should().NotBeNull();
            tuple.Item1.Should().Be("a");
            tuple.Item2.Should().Be("b");
            tuple.Item3.Should().Be("c");
        }

        [Fact]
        public void CreateTuple_SevenElements_CreatesCorrectly()
        {
            // Arrange
            var values = new List<object> { 1, 2, 3, 4, 5, 6, 7 };

            // Act
            var result = _logic.CreateTuple(values, TupleKind.ClassicTuple);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public void CreateTuple_NullValues_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => _logic.CreateTuple(null, TupleKind.ClassicTuple);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void CreateTuple_TooManyElements_ThrowsArgumentException()
        {
            // Arrange
            var values = new List<object> { 1, 2, 3, 4, 5, 6, 7, 8 };

            // Act & Assert
            Action act = () => _logic.CreateTuple(values, TupleKind.ClassicTuple);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void CreateTuple_EmptyList_ThrowsArgumentException()
        {
            // Arrange
            var values = new List<object>();

            // Act & Assert
            Action act = () => _logic.CreateTuple(values, TupleKind.ClassicTuple);
            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region GetTupleItem Tests

        [Fact]
        public void GetTupleItem_Item1_ReturnsFirstElement()
        {
            // Arrange
            var tuple = System.Tuple.Create("apple", "banana", "cherry");

            // Act
            var result = _logic.GetTupleItem(tuple, TupleItemIndex.Item1);

            // Assert
            result.Should().Be("apple");
        }

        [Fact]
        public void GetTupleItem_Item2_ReturnsSecondElement()
        {
            // Arrange
            var tuple = System.Tuple.Create("apple", "banana", "cherry");

            // Act
            var result = _logic.GetTupleItem(tuple, TupleItemIndex.Item2);

            // Assert
            result.Should().Be("banana");
        }

        [Fact]
        public void GetTupleItem_Item3_ReturnsThirdElement()
        {
            // Arrange
            var tuple = System.Tuple.Create("apple", "banana", "cherry");

            // Act
            var result = _logic.GetTupleItem(tuple, TupleItemIndex.Item3);

            // Assert
            result.Should().Be("cherry");
        }

        [Fact]
        public void GetTupleItem_InvalidIndex_ThrowsArgumentException()
        {
            // Arrange
            var tuple = System.Tuple.Create("apple", "banana");

            // Act & Assert
            Action act = () => _logic.GetTupleItem(tuple, TupleItemIndex.Item5);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GetTupleItem_NullTuple_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => _logic.GetTupleItem(null, TupleItemIndex.Item1);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GetTupleItem_NotATuple_ThrowsArgumentException()
        {
            // Arrange
            var notATuple = "just a string";

            // Act & Assert
            Action act = () => _logic.GetTupleItem(notATuple, TupleItemIndex.Item1);
            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region DestructureTuple Tests

        [Fact]
        public void DestructureTuple_TwoElements_ReturnsListOfTwo()
        {
            // Arrange
            var tuple = System.Tuple.Create("apple", 42);

            // Act
            var result = _logic.DestructureTuple(tuple);

            // Assert
            result.Should().HaveCount(2);
            result[0].Should().Be("apple");
            result[1].Should().Be(42);
        }

        [Fact]
        public void DestructureTuple_FiveElements_ReturnsListOfFive()
        {
            // Arrange
            var tuple = System.Tuple.Create(1, 2, 3, 4, 5);

            // Act
            var result = _logic.DestructureTuple(tuple);

            // Assert
            result.Should().HaveCount(5);
            result.Should().Equal(new object[] { 1, 2, 3, 4, 5 });
        }

        [Fact]
        public void DestructureTuple_NullTuple_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => _logic.DestructureTuple(null);
            act.Should().Throw<ArgumentNullException>();
        }

        #endregion

        #region TupleToDictionary Tests

        [Fact]
        public void TupleToDictionary_CreatesCorrectDictionary()
        {
            // Arrange
            var tuple = System.Tuple.Create("apple", "banana", "cherry");

            // Act
            var result = _logic.TupleToDictionary(tuple);

            // Assert
            result.Should().HaveCount(3);
            result["Item1"].Should().Be("apple");
            result["Item2"].Should().Be("banana");
            result["Item3"].Should().Be("cherry");
        }

        [Fact]
        public void TupleToDictionary_SingleElement_CreatesOneKeyDictionary()
        {
            // Arrange
            var tuple = System.Tuple.Create("only");

            // Act
            var result = _logic.TupleToDictionary(tuple);

            // Assert
            result.Should().HaveCount(1);
            result["Item1"].Should().Be("only");
        }

        [Fact]
        public void TupleToDictionary_NullTuple_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => _logic.TupleToDictionary(null);
            act.Should().Throw<ArgumentNullException>();
        }

        #endregion

        #region SetTupleItem Tests

        [Fact(DisplayName = "SetTupleItem: замена первого элемента")]
        public void SetTupleItem_Item1_ReturnsNewTupleWithReplacedValue()
        {
            // Arrange
            var tuple = System.Tuple.Create("old", "banana", "cherry");

            // Act
            var result = _logic.SetTupleItem(tuple, TupleItemIndex.Item1, "apple");

            // Assert
            result.Should().NotBeNull();
            var newTuple = result as System.Tuple<object, object, object>;
            newTuple.Should().NotBeNull();
            newTuple.Item1.Should().Be("apple");
            newTuple.Item2.Should().Be("banana");
            newTuple.Item3.Should().Be("cherry");
        }

        [Fact(DisplayName = "SetTupleItem: замена второго элемента")]
        public void SetTupleItem_Item2_ReturnsNewTupleWithReplacedValue()
        {
            // Arrange
            var tuple = System.Tuple.Create("apple", "old", "cherry");

            // Act
            var result = _logic.SetTupleItem(tuple, TupleItemIndex.Item2, "banana");

            // Assert
            result.Should().NotBeNull();
            var newTuple = result as System.Tuple<object, object, object>;
            newTuple.Should().NotBeNull();
            newTuple.Item1.Should().Be("apple");
            newTuple.Item2.Should().Be("banana");
            newTuple.Item3.Should().Be("cherry");
        }

        [Fact(DisplayName = "SetTupleItem: замена последнего элемента")]
        public void SetTupleItem_LastItem_ReturnsNewTupleWithReplacedValue()
        {
            // Arrange
            var tuple = System.Tuple.Create("apple", "banana", "old");

            // Act
            var result = _logic.SetTupleItem(tuple, TupleItemIndex.Item3, "cherry");

            // Assert
            result.Should().NotBeNull();
            var newTuple = result as System.Tuple<object, object, object>;
            newTuple.Should().NotBeNull();
            newTuple.Item1.Should().Be("apple");
            newTuple.Item2.Should().Be("banana");
            newTuple.Item3.Should().Be("cherry");
        }

        [Fact(DisplayName = "SetTupleItem: замена с изменением типа значения")]
        public void SetTupleItem_DifferentType_ReturnsNewTupleWithNewType()
        {
            // Arrange
            var tuple = System.Tuple.Create("string", 42);

            // Act
            var result = _logic.SetTupleItem(tuple, TupleItemIndex.Item1, 123);

            // Assert
            result.Should().NotBeNull();
            var newTuple = result as System.Tuple<object, object>;
            newTuple.Item1.Should().Be(123);
            newTuple.Item2.Should().Be(42);
        }

        [Fact(DisplayName = "SetTupleItem: null кортеж — ошибка")]
        public void SetTupleItem_NullTuple_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => _logic.SetTupleItem(null, TupleItemIndex.Item1, "value");
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact(DisplayName = "SetTupleItem: индекс вне диапазона — ошибка")]
        public void SetTupleItem_InvalidIndex_ThrowsArgumentException()
        {
            // Arrange
            var tuple = System.Tuple.Create("apple", "banana");

            // Act & Assert
            Action act = () => _logic.SetTupleItem(tuple, TupleItemIndex.Item5, "cherry");
            act.Should().Throw<ArgumentException>();
        }

        [Fact(DisplayName = "SetTupleItem: не кортеж — ошибка")]
        public void SetTupleItem_NotATuple_ThrowsArgumentException()
        {
            // Arrange
            var notATuple = "just a string";

            // Act & Assert
            Action act = () => _logic.SetTupleItem(notATuple, TupleItemIndex.Item1, "value");
            act.Should().Throw<ArgumentException>();
        }

        [Fact(DisplayName = "SetTupleItem: оригинальный кортеж не изменяется")]
        public void SetTupleItem_OriginalTupleUnchanged()
        {
            // Arrange
            var tuple = System.Tuple.Create("original", "value");

            // Act
            _logic.SetTupleItem(tuple, TupleItemIndex.Item1, "new");

            // Assert — оригинал не изменился
            tuple.Item1.Should().Be("original");
        }

        #endregion

        #region GetArity Tests

        [Fact(DisplayName = "GetArity: кортеж из 1 элемента")]
        public void GetArity_OneElement_Returns1()
        {
            // Arrange
            var tuple = System.Tuple.Create("only");

            // Act
            var result = _logic.GetArity(tuple);

            // Assert
            result.Should().Be(1);
        }

        [Fact(DisplayName = "GetArity: кортеж из 2 элементов")]
        public void GetArity_TwoElements_Returns2()
        {
            // Arrange
            var tuple = System.Tuple.Create("a", "b");

            // Act
            var result = _logic.GetArity(tuple);

            // Assert
            result.Should().Be(2);
        }

        [Fact(DisplayName = "GetArity: кортеж из 7 элементов")]
        public void GetArity_SevenElements_Returns7()
        {
            // Arrange
            var tuple = System.Tuple.Create(1, 2, 3, 4, 5, 6, 7);

            // Act
            var result = _logic.GetArity(tuple);

            // Assert
            result.Should().Be(7);
        }

        [Fact(DisplayName = "GetArity: null — возвращает -1")]
        public void GetArity_Null_ReturnsMinus1()
        {
            // Act
            var result = _logic.GetArity(null);

            // Assert
            result.Should().Be(-1);
        }

        [Fact(DisplayName = "GetArity: не кортеж — возвращает -1")]
        public void GetArity_NotATuple_ReturnsMinus1()
        {
            // Arrange
            var notATuple = "just a string";

            // Act
            var result = _logic.GetArity(notATuple);

            // Assert
            result.Should().Be(-1);
        }

        #endregion
    }
}
