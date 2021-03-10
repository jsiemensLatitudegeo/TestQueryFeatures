using Esri.ArcGISRuntime.ArcGISServices;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestQueryFeatures
{
    internal class TileCacheTracker
    {
        public TileInfo TileInfo { get; }

        public VectorTileSourceInfo VectorTileInfo { get; }

        public Envelope FullExtent { get; set; }

        public TileCacheTrackerLevel[] LevelsOfDetail { get; }

        public TileCacheTracker(TileInfo tileInfo)
        {
            TileInfo = tileInfo ?? throw new ArgumentNullException(nameof(tileInfo));
            LevelsOfDetail = tileInfo.LevelsOfDetail.Select(x => new TileCacheTrackerLevel(tileInfo, x)).OrderByDescending(x => x.LevelOfDetail.Scale).ToArray();
        }

        public TileCacheTracker(VectorTileSourceInfo vectorTileInfo)
        {
            VectorTileInfo = vectorTileInfo ?? throw new ArgumentNullException(nameof(vectorTileInfo));
            LevelsOfDetail = vectorTileInfo.LevelsOfDetail.Select(x => new TileCacheTrackerLevel(vectorTileInfo, x)).OrderByDescending(x => x.LevelOfDetail.Scale).ToArray();
        }

        public TileCacheTrackerLevel GetNearestLevel(double scale)
        {
            // Find the closest level that is greater than or equal to the scale
            TileCacheTrackerLevel level = null;
            foreach (var lod in LevelsOfDetail)
            {
                if (level == null || lod.LevelOfDetail.Scale >= scale)
                {
                    level = lod;
                }
            }

            return level;
        }

        public (TileCacheTrackerLevel Level, Tile[,] Tiles) GetTiles(double scale, Envelope envelope)
        {
            if (envelope is null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            var level = GetNearestLevel(scale);
            var tiles = level.GetTiles(envelope.ToSlimEnvelope());
            return (level, tiles);
        }

        public bool DoesCacheCover(Tile tile, Layer layer)
        {
            bool doesCover = false;
            Parallel.ForEach(LevelsOfDetail, level =>
            {
                if (level.DoesCacheCover(tile, layer))
                {
                    doesCover = true;
                }
            });
            return doesCover;
        }

        public void Reset()
        {
            foreach (var level in LevelsOfDetail)
            {
                level.Reset();
            }
        }
    }
}
