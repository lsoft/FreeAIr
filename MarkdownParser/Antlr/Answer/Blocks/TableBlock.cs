using MarkdownParser.Antlr.Answer.Parts;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace MarkdownParser.Antlr.Answer.Blocks
{
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
                    new Run(
                        cellText
                        )
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
