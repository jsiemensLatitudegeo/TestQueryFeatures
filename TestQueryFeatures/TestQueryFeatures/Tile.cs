using Esri.ArcGISRuntime.ArcGISServices;
using Esri.ArcGISRuntime.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestQueryFeatures
{
    internal class Tile
    {
        public TilePosition Position { get; }

        public Envelope Envelope { get; }

        public bool IsCached { get; }

        public LevelOfDetail LevelOfDetail { get; }

        public Tile(TilePosition position, bool isCached, Envelope envelope, LevelOfDetail lod)
        {
            Position = position;
            Envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));
            IsCached = isCached;
            LevelOfDetail = lod ?? throw new ArgumentNullException(nameof(lod));
        }
    }
}
