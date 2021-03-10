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

        public SlimEnvelope Envelope { get; }

        public LevelOfDetail LevelOfDetail { get; }

        public Tile(TilePosition position, SlimEnvelope envelope, LevelOfDetail lod)
        {
            Position = position;
            Envelope = envelope;
            LevelOfDetail = lod ?? throw new ArgumentNullException(nameof(lod));
        }
    }
}
