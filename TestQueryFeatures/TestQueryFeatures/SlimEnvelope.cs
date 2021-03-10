using Esri.ArcGISRuntime.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestQueryFeatures
{
    internal class SlimEnvelope
    {
        public double XMin { get; }

        public double YMin { get; }

        public double XMax { get; }

        public double YMax { get; }

        public SpatialReference SpatialReference { get; }

        public SlimEnvelope(double xMin, double yMin, double xMax, double yMax, SpatialReference spatialReference)
        {
            XMin = xMin;
            YMin = yMin;
            XMax = xMax;
            YMax = yMax;
            SpatialReference = spatialReference;
        }

        private Envelope _envelope;

        public Envelope ToEnvelope()
        {
            _envelope = _envelope ?? new Envelope(XMin, YMin, XMax, YMax, SpatialReference);
            return _envelope;
        }
    }

    internal static class LeanEnvelopeExtensions
    {
        public static SlimEnvelope ToSlimEnvelope(this Envelope envelope)
        {
            return new SlimEnvelope(envelope.XMin, envelope.YMin, envelope.XMax, envelope.YMax, envelope.SpatialReference);
        }
    }
}
