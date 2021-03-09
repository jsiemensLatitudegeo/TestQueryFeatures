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
        private readonly Dictionary<LevelOfDetail, GraphicsOverlay> _overlays = new Dictionary<LevelOfDetail, GraphicsOverlay>();
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
                var overlay = GetOverlay(tile.LevelOfDetail);
                overlay.Graphics.Add(new Graphic()
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

                overlay.Graphics.Add(new Graphic()
                {
                    Geometry = tile.Envelope.GetCenter(),
                    Symbol = new TextSymbol($"{tile.LevelOfDetail.Level}: ({tile.Position})", System.Drawing.Color.White, 32, HorizontalAlignment.Center, VerticalAlignment.Middle) { HaloColor = System.Drawing.Color.Black },
                    ZIndex = 2
                });
            }
            finally
            {
                _highlightSemaphore.Release();
            }
        }

        private GraphicsOverlay GetOverlay(LevelOfDetail lod)
        {
            if (_overlays.TryGetValue(lod, out var overlay))
            {
                return overlay;
            }

            overlay = new GraphicsOverlay()
            {
                IsVisible = _show
            };
            _overlays.Add(lod, overlay);
            _mapView.GraphicsOverlays.Add(overlay);
            return overlay;
        }

        public void Reset()
        {
            foreach (var overlay in _overlays.Values)
            {
                overlay.Graphics.Clear();
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
            foreach (var overlay in _overlays.Values)
            {
                overlay.IsVisible = false;
            }
        }
    }
}
