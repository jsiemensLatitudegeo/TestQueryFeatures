using Esri.ArcGISRuntime.ArcGISServices;
using Esri.ArcGISRuntime.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestQueryFeatures
{
    internal class TileCacheTrackerLevel
    {
        private HashSet<TilePosition> _cachedTiles = new HashSet<TilePosition>();

        public double TileWidthMapUnits { get; }

        public double TileHeightMapUnits { get; }

        public TileInfo TileInfo { get; }

        public VectorTileSourceInfo VectorTileInfo { get; }

        public MapPoint Origin => TileInfo?.Origin ?? VectorTileInfo?.Origin;

        public SpatialReference SpatialReference => TileInfo?.SpatialReference ?? VectorTileInfo?.SpatialReference;

        public LevelOfDetail LevelOfDetail { get; }

        public int FeatureCount { get; set; }

        public TileCacheTrackerLevel(TileInfo tileInfo, LevelOfDetail level)
        {
            TileInfo = tileInfo ?? throw new ArgumentNullException(nameof(tileInfo));
            LevelOfDetail = level ?? throw new ArgumentNullException(nameof(level));
            TileWidthMapUnits = level.Resolution * tileInfo.TileWidth;
            TileHeightMapUnits = level.Resolution * tileInfo.TileHeight;
        }

        public TileCacheTrackerLevel(VectorTileSourceInfo vectorTileInfo, LevelOfDetail level)
        {
            VectorTileInfo = vectorTileInfo ?? throw new ArgumentNullException(nameof(vectorTileInfo));
            LevelOfDetail = level ?? throw new ArgumentNullException(nameof(level));
            TileWidthMapUnits = level.Resolution * 256;
            TileHeightMapUnits = level.Resolution * 256;
        }

        public void MarkTileAsCached(TilePosition position)
        {
            _cachedTiles.Add(position);
        }

        public bool IsTileCached(TilePosition position)
        {
            return _cachedTiles.Contains(position);
        }

        public Tile[,] GetTiles(Envelope envelope)
        {
            if (envelope is null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            var startRow = (int)Math.Floor((Origin.Y - envelope.YMax) / TileHeightMapUnits) - 1;
            var startColumn = (int)Math.Floor((envelope.XMin - Origin.X) / TileWidthMapUnits);
            var endRow = (int)Math.Ceiling((Origin.Y - envelope.YMin) / TileHeightMapUnits) - 1;
            var endColumn = (int)Math.Ceiling((envelope.XMax - Origin.X) / TileWidthMapUnits);

            var numRows = endRow - startRow + 1;
            var numColumns = endColumn - startColumn + 1;
            var tiles = new Tile[numColumns, numRows];
            for (int x = 0; x < numColumns; x++)
            {
                int column = x + startColumn;
                for (int y = 0; y < numRows; y++)
                {
                    int row = y + startRow;
                    var tilePosition = new TilePosition(row, column);
                    var xmin = Origin.X + (column * TileWidthMapUnits);
                    var ymin = Origin.Y - ((row + 1) * TileHeightMapUnits);
                    var xmax = xmin + TileWidthMapUnits;
                    var ymax = ymin + TileHeightMapUnits;
                    tiles[x, y] = new Tile(tilePosition, new Envelope(xmin, ymin, xmax, ymax, SpatialReference), LevelOfDetail);
                }
            }

            return tiles;
        }

        public void Reset()
        {
            _cachedTiles.Clear();
            FeatureCount = 0;
        }
    }
}
