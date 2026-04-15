// Example grid-traverse demonstrates walking the terminal grid
// cell-by-cell using the GridRef API to inspect content and style.
using Ghostty.Vt;
using Ghostty.Vt.Types;

using var term = new Terminal(10, 3);

// Write some content: two plain lines and one bold line.
term.VTWrite("Hello!\r\n"u8);
term.VTWrite("World\r\n"u8);
term.VTWrite("\x1b[1mBold"u8);

int cols = term.Cols;
int rows = term.Rows;

for (int row = 0; row < rows; row++)
{
    Console.Write($"Row {row}: ");
    for (int col = 0; col < cols; col++)
    {
        var gridRef = term.GetGridRef(Point.Active(col, row));
        var cell = gridRef.GetCell();
        if (cell.HasText && cell.Grapheme != null)
        {
            Console.Write(cell.Grapheme);
        }
        else
        {
            Console.Write('.');
        }
    }

    // Print wrap and bold state for the first cell in the row.
    var rowRef = term.GetGridRef(Point.Active(0, row));
    var rowData = rowRef.GetRow();
    var style = rowRef.GetStyle();
    Console.WriteLine($" (wrap={rowData.Wrap}, bold={style.Bold})");
}
