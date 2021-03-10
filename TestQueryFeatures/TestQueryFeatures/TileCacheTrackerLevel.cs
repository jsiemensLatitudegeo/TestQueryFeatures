using Esri.ArcGISRuntime.ArcGISServices;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestQueryFeatures
{
    internal class TileCacheTrackerLevel
    {
        private Dictionary<Layer, HashSet<TilePosition>> _cachedTiles = new Dictionary<Layer, HashSet<TilePosition>>();

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

        public void MarkTileAsCached(TilePosition position, Layer layer)
        {
            if (!_cachedTiles.TryGetValue(layer, out var lookup))
            {
                lookup = new HashSet<TilePosition>();
                _cachedTiles.Add(layer, lookup);
            }

            lookup.Add(position);
        }

        public void MarkTileAsNotCached(TilePosition position, Layer layer)
        {
            if (_cachedTiles.TryGetValue(layer, out var lookup) && lookup.Contains(position))
            {
                lookup.Remove(position);
            }
        }

        public bool IsTileCached(TilePosition position, Layer layer)
        {
            return _cachedTiles.TryGetValue(layer, out var lookup) && lookup.Contains(position);
        }

        public bool DoesCacheCover(Tile tile, Layer layer)
        {
            var tiles = GetTiles(tile.Envelope);
            bool[] isCached = tiles.OfType<Tile>().Select(x => IsTileCached(x.Position, layer)).ToArray();
            return isCached.All(x => x);
        }

        public Tile[,] GetTiles(SlimEnvelope envelope)
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
            var numColumns = endColumn - startColumn;
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
                    tiles[x, y] = new Tile(tilePosition, new SlimEnvelope(xmin, ymin, xmax, ymax, SpatialReference), LevelOfDetail);
                }
            }

            return tiles;
        }

        public void Reset()
        {
            _cachedTiles.Clear();
            FeatureCount = 0;
        }

        private struct TileCacheKey
        {
            public TilePosition Position { get; set; }

            public Layer Layer { get; set; }

            public TileCacheKey(TilePosition position, Layer layer)
            {
                Position = position;
                Layer = layer;
            }

            public override bool Equals(object obj)
            {
                return obj is TileCacheKey key &&
                       EqualityComparer<TilePosition>.Default.Equals(Position, key.Position) &&
                       EqualityComparer<Layer>.Default.Equals(Layer, key.Layer);
            }

            public override int GetHashCode()
            {
                int hashCode = -1770035120;
                hashCode = hashCode * -1521134295 + Position.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<Layer>.Default.GetHashCode(Layer);
                return hashCode;
            }
        }
    }
}
