using System;
using System.Collections.Generic;
using System.Text;

namespace TestQueryFeatures
{
    internal struct TilePosition
    {
        public int Row { get; set; }

        public int Column { get; set; }

        public TilePosition(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public override bool Equals(object obj)
        {
            return obj is TilePosition position &&
                   Row == position.Row &&
                   Column == position.Column;
        }

        public override int GetHashCode()
        {
            int hashCode = 240067226;
            hashCode = hashCode * -1521134295 + Row.GetHashCode();
            hashCode = hashCode * -1521134295 + Column.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return $"{Column},{Row}";
        }
    }
}
