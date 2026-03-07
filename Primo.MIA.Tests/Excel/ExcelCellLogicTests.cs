using System;
using FluentAssertions;
using Primo.MIA.Tests.Logic;
using Xunit;

namespace Primo.MIA.Tests.Excel
{
    public class ExcelCellLogicTests
    {
        private readonly ExcelCellLogic _logic;

        public ExcelCellLogicTests()
        {
            _logic = new ExcelCellLogic();
        }

        #region Basic Recalculation Tests

        [Fact]
        public void RecalculateCell_NoOffset_ReturnsSameCell()
        {
            // Arrange
            string startCell = "B5";

            // Act
            var result = _logic.RecalculateCell(startCell, 0, 0);

            // Assert
            result.Should().Be("B5");
        }

        [Fact]
        public void RecalculateCell_RowOffsetOnly_MovesDown()
        {
            // Arrange
            string startCell = "A1";

            // Act
            var result = _logic.RecalculateCell(startCell, 2, 0);

            // Assert
            result.Should().Be("A3");
        }

        [Fact]
        public void RecalculateCell_ColumnOffsetOnly_MovesRight()
        {
            // Arrange
            string startCell = "A1";

            // Act
            var result = _logic.RecalculateCell(startCell, 0, 3);

            // Assert
            result.Should().Be("D1");
        }

        [Fact]
        public void RecalculateCell_BothOffsets_MovesDiagonally()
        {
            // Arrange
            string startCell = "A1";

            // Act
            var result = _logic.RecalculateCell(startCell, 2, 3);

            // Assert
            result.Should().Be("D3");
        }

        #endregion

        #region Negative Offset Tests

        [Fact]
        public void RecalculateCell_NegativeRowOffset_MovesUp()
        {
            // Arrange
            string startCell = "E10";

            // Act
            var result = _logic.RecalculateCell(startCell, -3, 0);

            // Assert
            result.Should().Be("E7");
        }

        [Fact]
        public void RecalculateCell_NegativeColumnOffset_MovesLeft()
        {
            // Arrange
            string startCell = "E10";

            // Act
            var result = _logic.RecalculateCell(startCell, 0, -2);

            // Assert
            result.Should().Be("C10");
        }

        [Fact]
        public void RecalculateCell_BothNegativeOffsets_MovesUpLeft()
        {
            // Arrange
            string startCell = "E10";

            // Act
            var result = _logic.RecalculateCell(startCell, -3, -2);

            // Assert
            result.Should().Be("C7");
        }

        #endregion

        #region Multi-Letter Column Tests

        [Fact]
        public void RecalculateCell_ColumnZ_Plus5_ReturnsAE()
        {
            // Arrange
            string startCell = "Z1";

            // Act
            var result = _logic.RecalculateCell(startCell, 0, 5);

            // Assert
            result.Should().Be("AE1");
        }

        [Fact]
        public void RecalculateCell_ColumnAA_Plus1_ReturnsAB()
        {
            // Arrange
            string startCell = "AA1";

            // Act
            var result = _logic.RecalculateCell(startCell, 0, 1);

            // Assert
            result.Should().Be("AB1");
        }

        [Fact]
        public void RecalculateCell_ColumnAZ_Plus1_ReturnsBA()
        {
            // Arrange
            string startCell = "AZ1";

            // Act
            var result = _logic.RecalculateCell(startCell, 0, 1);

            // Assert
            result.Should().Be("BA1");
        }

        [Fact]
        public void RecalculateCell_ColumnAAA_Supported()
        {
            // Arrange
            string startCell = "AAA1";

            // Act
            var result = _logic.RecalculateCell(startCell, 0, 1);

            // Assert
            result.Should().Be("AAB1");
        }

        #endregion

        #region Boundary Protection Tests

        [Fact]
        public void RecalculateCell_NegativeOffsetBeyondA1_ReturnsA1()
        {
            // Arrange
            string startCell = "B2";

            // Act
            var result = _logic.RecalculateCell(startCell, -5, -3);

            // Assert
            result.Should().Be("A1");
        }

        [Fact]
        public void RecalculateCell_RowGoesNegative_ClampsTo1()
        {
            // Arrange
            string startCell = "E3";

            // Act
            var result = _logic.RecalculateCell(startCell, -10, 0);

            // Assert
            result.Should().Be("E1");
        }

        [Fact]
        public void RecalculateCell_ColumnGoesNegative_ClampsToA()
        {
            // Arrange
            string startCell = "C5";

            // Act
            var result = _logic.RecalculateCell(startCell, 0, -10);

            // Assert
            result.Should().Be("A5");
        }

        #endregion

        #region Format Validation Tests

        [Theory]
        [InlineData("A1")]
        [InlineData("B2")]
        [InlineData("Z10")]
        [InlineData("AA5")]
        [InlineData("ZZ999")]
        [InlineData("AAA1")]
        public void RecalculateCell_ValidFormats_Accepted(string cellName)
        {
            // Act
            var result = _logic.RecalculateCell(cellName, 0, 0);

            // Assert
            result.Should().Be(cellName);
        }

        [Theory]
        [InlineData("1A")]
        [InlineData("A")]
        [InlineData("1")]
        [InlineData("A1:B2")]
        [InlineData("$A$1")]
        [InlineData("Sheet1!A1")]
        [InlineData("")]
        [InlineData("  ")]
        public void RecalculateCell_InvalidFormats_ThrowsException(string cellName)
        {
            // Act & Assert
            Action act = () => _logic.RecalculateCell(cellName, 0, 0);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void RecalculateCell_NullCell_ThrowsException()
        {
            // Act & Assert
            Action act = () => _logic.RecalculateCell(null, 0, 0);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void RecalculateCell_CaseInsensitive_WorksWithLowercase()
        {
            // Arrange
            string startCell = "a1";

            // Act
            var result = _logic.RecalculateCell(startCell, 1, 1);

            // Assert
            result.Should().Be("B2");
        }

        #endregion

        #region Large Offset Tests

        [Fact]
        public void RecalculateCell_LargeRowOffset_Works()
        {
            // Arrange
            string startCell = "A1";

            // Act
            var result = _logic.RecalculateCell(startCell, 1000, 0);

            // Assert
            result.Should().Be("A1001");
        }

        [Fact]
        public void RecalculateCell_LargeColumnOffset_Works()
        {
            // Arrange
            string startCell = "A1";

            // Act
            var result = _logic.RecalculateCell(startCell, 0, 100);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().EndWith("1");
        }

        #endregion

        #region Real-World Scenarios

        [Fact]
        public void RecalculateCell_BuildRange_A1ToE10()
        {
            // Arrange - создание диапазона A1:E10
            string startCell = "A1";

            // Act - вычисляем конечную ячейку
            var endCell = _logic.RecalculateCell(startCell, 9, 4);

            // Assert
            endCell.Should().Be("E10");
        }

        [Fact]
        public void RecalculateCell_LoopIteration_IncrementRow()
        {
            // Arrange - симуляция цикла по строкам
            string startCell = "B2";

            // Act - итерации 0, 1, 2
            var row0 = _logic.RecalculateCell(startCell, 0, 0);
            var row1 = _logic.RecalculateCell(startCell, 1, 0);
            var row2 = _logic.RecalculateCell(startCell, 2, 0);

            // Assert
            row0.Should().Be("B2");
            row1.Should().Be("B3");
            row2.Should().Be("B4");
        }

        [Fact]
        public void RecalculateCell_MatrixNavigation_2x3()
        {
            // Arrange - навигация по матрице 2x3 начиная с B2
            string startCell = "B2";

            // Act - получаем все ячейки матрицы
            var cells = new string[2, 3];
            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    cells[row, col] = _logic.RecalculateCell(startCell, row, col);
                }
            }

            // Assert
            cells[0, 0].Should().Be("B2");
            cells[0, 1].Should().Be("C2");
            cells[0, 2].Should().Be("D2");
            cells[1, 0].Should().Be("B3");
            cells[1, 1].Should().Be("C3");
            cells[1, 2].Should().Be("D3");
        }

        #endregion
    }
}
