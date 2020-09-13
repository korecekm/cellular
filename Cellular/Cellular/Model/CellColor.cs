using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cellular.Model
{
    public class CellColor
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public Color Color { get; set; }
        public List<Location> RelevantCells { get; set; }
        public List<Rule> Rules { get; set; }
    }

    /// <summary>
    /// Struct representing the relative position of a cell.
    /// As an example a rule may check the color of the cell to its left, that
    /// cell is represented by Location with diffX -1 and diffY 0.
    /// (which would be represented as -1:0 in the config file)
    /// </summary>
    public readonly struct Location : IEquatable<Location>
    {
        public readonly int diffX, diffY;

        public Location(int diffX, int diffY)
        {
            this.diffX = diffX;
            this.diffY = diffY;
        }

        public bool Equals(Location other)
        {
            return this.diffX == other.diffX
                && this.diffY == other.diffY;
        }
    }
}
