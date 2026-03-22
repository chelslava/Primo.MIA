using System;
using System.Text.RegularExpressions;

namespace Primo.MIA
{
    public class ExcelCellLogic
    {
        public string RecalculateCell(string startCell, int rowOffset, int columnOffset)
        {
            if (string.IsNullOrWhiteSpace(startCell))
                throw new ArgumentException("Начальная ячейка не может быть пустой");

            var cellInfo = ParseExcelCell(startCell);
            if (!cellInfo.IsValid)
                throw new ArgumentException($"Неверный формат ячейки: {startCell}");

            int newRow = Math.Max(1, cellInfo.Row + rowOffset);
            int newCol = Math.Max(1, cellInfo.Column + columnOffset);

            return ConvertToExcelColumn(newCol) + newRow.ToString();
        }

        private CellCoordinates ParseExcelCell(string cellName)
        {
            if (string.IsNullOrWhiteSpace(cellName))
                return new CellCoordinates(false, 0, 0);

            Match match = Regex.Match(cellName.Trim(), @"^([A-Z]+)(\d+)$", RegexOptions.IgnoreCase);
            if (!match.Success)
                return new CellCoordinates(false, 0, 0);

            string colPart = match.Groups[1].Value.ToUpperInvariant();
            string rowPart = match.Groups[2].Value;

            if (!int.TryParse(rowPart, out int row) || row < 1)
                return new CellCoordinates(false, 0, 0);

            int column = ConvertFromExcelColumn(colPart);
            return new CellCoordinates(true, row, column);
        }

        private int ConvertFromExcelColumn(string columnName)
        {
            int result = 0;
            foreach (char c in columnName)
            {
                result *= 26;
                result += c - 'A' + 1;
            }

            return result;
        }

        private string ConvertToExcelColumn(int columnNumber)
        {
            if (columnNumber <= 0)
                return string.Empty;

            string result = string.Empty;
            while (columnNumber > 0)
            {
                columnNumber--;
                result = (char)('A' + (columnNumber % 26)) + result;
                columnNumber /= 26;
            }

            return result;
        }

        private struct CellCoordinates
        {
            public CellCoordinates(bool isValid, int row, int column)
            {
                IsValid = isValid;
                Row = row;
                Column = column;
            }

            public bool IsValid { get; }

            public int Row { get; }

            public int Column { get; }
        }
    }
}
