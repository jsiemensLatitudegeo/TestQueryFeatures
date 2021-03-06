using Esri.ArcGISRuntime.ArcGISServices;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Xamarin.Forms;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestQueryFeatures
{
    internal class TileHighlighter
    {
        private readonly TileCacheTracker _tracker;
        private readonly MapView _mapView;
        private readonly Dictionary<LevelOfDetail, GraphicsOverlay> _overlays = new Dictionary<LevelOfDetail, GraphicsOverlay>();

        public TileHighlighter(TileCacheTracker tracker, MapView mapView)
        {
            _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
            _mapView = mapView ?? throw new ArgumentNullException(nameof(mapView));
            _mapView.ViewpointChanged += MapView_ViewpointChanged;
        }

        private void MapView_ViewpointChanged(object sender, EventArgs e)
        {
            var scale = _mapView.GetCurrentViewpoint(ViewpointType.CenterAndScale).TargetScale;

            var nearestLevel = _tracker.GetNearestLevel(scale);

            foreach (var kvp in _overlays)
            {
                if (kvp.Key.Scale == nearestLevel.LevelOfDetail.Scale)
                {
                    kvp.Value.IsVisible = true;
                }
                else
                {
                    kvp.Value.IsVisible = false;
                }
            }
        }

        public void HighlightTile(Tile tile)
        {
            if (tile is null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            var _overlay = GetOverlay(tile.LevelOfDetail);
            _overlay.Graphics.Add(new Graphic()
            {
                Geometry = tile.Envelope,
                Symbol = new SimpleFillSymbol(
                    SimpleFillSymbolStyle.Solid, 
                    System.Drawing.Color.FromArgb(11, System.Drawing.Color.Indigo), 
                    new SimpleLineSymbol(
                        SimpleLineSymbolStyle.Solid, 
                        System.Drawing.Color.Red,
                        2)),
                ZIndex = 1
            });

            _overlay.Graphics.Add(new Graphic()
            {
                Geometry = tile.Envelope.GetCenter(),
                Symbol = new TextSymbol($"{tile.LevelOfDetail.Level}: ({tile.Position})", System.Drawing.Color.White, 32, HorizontalAlignment.Center, VerticalAlignment.Middle) { HaloColor = System.Drawing.Color.Black },
                ZIndex = 2
            });
        }

        private GraphicsOverlay GetOverlay(LevelOfDetail lod)
        {
            if (_overlays.TryGetValue(lod, out var overlay))
            {
                return overlay;
            }

            overlay = new GraphicsOverlay();
            _overlays.Add(lod, overlay);
            _mapView.GraphicsOverlays.Add(overlay);
            return overlay;
        }
    }
}
