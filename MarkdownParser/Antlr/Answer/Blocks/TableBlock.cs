using MarkdownParser.Antlr.Answer.Parts;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace MarkdownParser.Antlr.Answer.Blocks
{
    /// <summary>
    /// A markdown pipe table, accumulated row by row as <see cref="AnswerMarkdownListener.EnterTable_row"/>
    /// fires and rendered as a bordered WPF <see cref="Table"/>. The row whose cells are all dashes
    /// (the header/body separator) is detected in <see cref="AddRow"/> and consumed instead of stored,
    /// marking the row before it as the header.
    /// </summary>
    public sealed class TableBlock : IBlock
    {
        private static readonly Brush _semiTransparentGray = new SolidColorBrush(Color.FromArgb(0x40, 0x80, 0x80, 0x80));
        
        private readonly IFontSizeProvider _fontSizeProvider;
        
        private bool _headerRowAdded = false;
        private List<List<string>> _rows;

        public BlockTypeEnum Type => BlockTypeEnum.Table;

        public TableBlock(
            IFontSizeProvider fontSizeProvider
            )
        {
            if (fontSizeProvider is null)
            {
                throw new ArgumentNullException(nameof(fontSizeProvider));
            }

            _rows = new List<List<string>>();
            _fontSizeProvider = fontSizeProvider;
        }

        /// <summary>Parses one `|`-delimited row; the dash-only separator row is consumed to mark the preceding row as the header instead of being stored as data.</summary>
        public void AddRow(
            string row
            )
        {
            var columns = row
                .Trim('|')
                .Split(new[] { "|" }, StringSplitOptions.None)
                .Select(r => r.Trim())
                .ToList()
                ;

            if (!_headerRowAdded && columns.All(c => string.IsNullOrEmpty(c.Trim('-'))))
            {
                _headerRowAdded = true;
                return;
            }

            _rows.Add(columns);
        }

        /// <summary>Builds the WPF <see cref="Table"/> from the accumulated rows, or null if none were added.</summary>
        public Block? CreateBlock(
            AdditionalCommandContainer? acc,
            bool isInProgress
            )
        {
            if (_rows.Count == 0)
            {
                return null;
            }

            var columnCount = _rows[0].Count;

            var table1 = new Table
            {
                Margin = new Thickness(10, 0, 0, 0),
                CellSpacing = 0,
                TextAlignment = TextAlignment.Center
            };

            // Define columns
            for (var ci = 0; ci < columnCount; ci++)
            {
                table1.Columns.Add(new TableColumn() { Width = GridLength.Auto });
            }

            var rowIndex = 0;

            var trg = new TableRowGroup();
            table1.RowGroups.Add(trg);

            if (_headerRowAdded)
            {
                // Create a header row
                var row0 = _rows[rowIndex];
                var headerRow = new TableRow();
                trg.Rows.Add(headerRow);
                headerRow.FontWeight = FontWeights.Bold;

                for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    headerRow.Cells.Add(
                        CreateTableCell(
                            row0[columnIndex],
                            _fontSizeProvider.TableHeaderSize,
                            GetBorderThickness(_rows.Count, row0.Count, rowIndex, columnIndex),
                            _semiTransparentGray
                            )
                        );
                }

                rowIndex++;
            }

            // Create a data rows
            for (; rowIndex < _rows.Count; rowIndex++)
            {
                var row = _rows[rowIndex];
                if (row.Count != columnCount)
                {
                    continue;
                }

                var dataRow = new TableRow();
                trg.Rows.Add(dataRow);

                for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    dataRow.Cells.Add(
                        CreateTableCell(
                            row[columnIndex],
                            _fontSizeProvider.TableBodySize,
                            GetBorderThickness(_rows.Count, row.Count, rowIndex, columnIndex),
                            null
                            )
                        );
                }
            }

            return table1;
        }


        /// <summary>Border thickness for a cell, drawing the outer table edge only on the first column and last row (interior cells share one border on each other side).</summary>
        public Thickness GetBorderThickness(
            int rowCount,
            int columnCount,
            int rowIndex,
            int columnIndex
            )
        {
            var left = 0.0;
            var top = 1.0;
            var right = 1.0;
            var bottom = 0.0;

            if (rowIndex == rowCount - 1)
            {
                bottom = 1.0;
            }
            if (columnIndex == 0)
            {
                left = 1.0;
            }

            return new Thickness(left, top, right, bottom);
        }

        private TableCell CreateTableCell(
            string cellText,
            double fontSize,
            Thickness border,
            Brush? background
            )
        {
            var cell = new TableCell(
                new Paragraph(
                    new Run
                    {
                        FontSize = fontSize,
                        Text = cellText
                    }
                    )
                )
            {
                Padding = new Thickness(0),
                FontSize = fontSize,
                BorderBrush = System.Windows.Media.Brushes.Black,
                BorderThickness = border,
            };


            if (background is not null)
            {
                cell.Background =  background;
            }

            return cell;
        }
    }
}
