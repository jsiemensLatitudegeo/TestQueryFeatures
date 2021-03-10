using Esri.ArcGISRuntime.ArcGISServices;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Xamarin.Forms;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace TestQueryFeatures
{
    internal class TileHighlighter
    {
        private readonly TileCacheTracker _tracker;
        private readonly MapView _mapView;
        private readonly Dictionary<LevelOfDetail, LevelHighlights> _highlights = new Dictionary<LevelOfDetail, LevelHighlights>();
        private bool _show;

        public TileHighlighter(TileCacheTracker tracker, MapView mapView)
        {
            _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
            _mapView = mapView ?? throw new ArgumentNullException(nameof(mapView));
            _mapView.ViewpointChanged += MapView_ViewpointChanged;
        }

        private void MapView_ViewpointChanged(object sender, EventArgs e)
        {
            if (_show)
            {
                UpdateOverlayVisibilities();
            }
        }

        private void UpdateOverlayVisibilities()
        {
            var scale = _mapView.GetCurrentViewpoint(ViewpointType.CenterAndScale).TargetScale;

            var nearestLevel = _tracker.GetNearestLevel(scale);

            foreach (var kvp in _highlights)
            {
                if (kvp.Key.Scale == nearestLevel.LevelOfDetail.Scale)
                {
                    kvp.Value.Overlay.IsVisible = true;
                }
                else
                {
                    kvp.Value.Overlay.IsVisible = false;
                }
            }
        }

        private SemaphoreSlim _highlightSemaphore = new SemaphoreSlim(1);

        public async void HighlightTile(Tile tile)
        {
            if (tile is null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            await _highlightSemaphore.WaitAsync();
            try
            {
                var highlights = GetHighlights(tile.LevelOfDetail);
                if (highlights.Tiles.Contains(tile.Position))
                {
                    return;
                }
                highlights.Tiles.Add(tile.Position);
                highlights.Overlay.Graphics.Add(new Graphic()
                {
                    Geometry = tile.Envelope.ToEnvelope(),
                    Symbol = new SimpleFillSymbol(
                        SimpleFillSymbolStyle.Solid,
                        System.Drawing.Color.FromArgb(11, System.Drawing.Color.Indigo),
                        new SimpleLineSymbol(
                            SimpleLineSymbolStyle.Solid,
                            System.Drawing.Color.Red,
                            2)),
                    ZIndex = 1
                });

                highlights.Overlay.Graphics.Add(new Graphic()
                {
                    Geometry = tile.Envelope.ToEnvelope().GetCenter(),
                    Symbol = new TextSymbol($"{tile.LevelOfDetail.Level}: ({tile.Position})", System.Drawing.Color.White, 32, HorizontalAlignment.Center, VerticalAlignment.Middle) { HaloColor = System.Drawing.Color.Black },
                    ZIndex = 2
                });
            }
            finally
            {
                _highlightSemaphore.Release();
            }
        }

        private LevelHighlights GetHighlights(LevelOfDetail lod)
        {
            if (_highlights.TryGetValue(lod, out var highlights))
            {
                return highlights;
            }

            highlights = new LevelHighlights();
            highlights.Overlay.IsVisible = _show;
            _highlights.Add(lod, highlights);
            _mapView.GraphicsOverlays.Add(highlights.Overlay);
            return highlights;
        }

        public void Reset()
        {
            foreach (var highlights in _highlights.Values)
            {
                highlights.Overlay.Graphics.Clear();
                highlights.Tiles.Clear();
            }
        }

        public void Show()
        {
            _show = true;
            UpdateOverlayVisibilities();
        }

        public void Hide()
        {
            _show = false;
            foreach (var highlights in _highlights.Values)
            {
                highlights.Overlay.IsVisible = false;
            }
        }

        private class LevelHighlights
        {
            public HashSet<TilePosition> Tiles { get; } = new HashSet<TilePosition>();

            public GraphicsOverlay Overlay { get; } = new GraphicsOverlay();
        }
    }
}
