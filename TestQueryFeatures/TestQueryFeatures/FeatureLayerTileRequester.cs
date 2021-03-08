using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Xamarin.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace TestQueryFeatures
{
    public class FeatureLayerTileRequester : INotifyPropertyChanged
    {
        private readonly TileCacheTracker _tileCacheTracker;
        private readonly TileHighlighter _tileHighlighter;
        private readonly MapView _mapView;
        private int _featureCount;
        private CancellationTokenSource _cancellation;

        public event PropertyChangedEventHandler PropertyChanged;

        public int FeatureCount
        {
            get => _featureCount;
            set
            {
                _featureCount = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FeatureCount)));
            }
        }

        public FeatureLayerTileRequester(MapView mapView)
        {
            _mapView = mapView ?? throw new ArgumentNullException(nameof(mapView));

            var tileLayer = mapView.Map.Basemap.BaseLayers.Union(mapView.Map.Basemap.ReferenceLayers).OfType<ArcGISTiledLayer>().FirstOrDefault();
            if (tileLayer != null)
            {
                _tileCacheTracker = new TileCacheTracker(tileLayer.TileInfo);
            }
            else
            {
                var vectorTiledLayer = mapView.Map.Basemap.BaseLayers.Union(mapView.Map.Basemap.ReferenceLayers).OfType<ArcGISVectorTiledLayer>().FirstOrDefault();
                _tileCacheTracker = new TileCacheTracker(vectorTiledLayer.SourceInfo);
            }

            _tileHighlighter = new TileHighlighter(_tileCacheTracker, mapView);
        }

        public async void Start()
        {
            _cancellation = new CancellationTokenSource();
            foreach (var fl in _mapView.Map.OperationalLayers.OfType<FeatureLayer>())
            {
                if (fl.FeatureTable is ServiceFeatureTable serviceFeatureTable)
                {
                    serviceFeatureTable.FeatureRequestMode = FeatureRequestMode.ManualCache;
                }
            }

            _mapView.NavigationCompleted += MainMapView_NavigationCompleted;
            await UpdateFeatures();
        }

        public void Stop()
        {
            _cancellation.Cancel();
            foreach (var fl in _mapView.Map.OperationalLayers.OfType<FeatureLayer>())
            {
                if (fl.FeatureTable is ServiceFeatureTable serviceFeatureTable)
                {
                    serviceFeatureTable.FeatureRequestMode = FeatureRequestMode.OnInteractionCache;
                }
            }

            _mapView.NavigationCompleted -= MainMapView_NavigationCompleted;

            foreach (var fl in _mapView.Map.OperationalLayers.OfType<FeatureLayer>())
            {
                if (fl.FeatureTable is ServiceFeatureTable serviceTable)
                {
                    //serviceTable.ClearCache(true);
                }
            }

            var currentViewpoint = _mapView.GetCurrentViewpoint(ViewpointType.CenterAndScale);
            var locationCenter = (MapPoint)currentViewpoint.TargetGeometry;
            var screenCenter = _mapView.LocationToScreen(locationCenter);
            var shiftedLocationCenter = _mapView.ScreenToLocation(new Point(screenCenter.X + 1, screenCenter.Y));
            _mapView.SetViewpoint(new Viewpoint(shiftedLocationCenter, currentViewpoint.TargetScale));
        }

        public void ShowTiles()
        {
            _tileHighlighter.Show();
        }

        public void HideTiles()
        {
            _tileHighlighter.Hide();
        }

        public void Reset()
        {
            _tileHighlighter.Reset();
            _tileCacheTracker.Reset();
            FeatureCount = 0;
        }

        private async void MainMapView_NavigationCompleted(object sender, EventArgs e)
        {
            await UpdateFeatures();
        }

        private async Task UpdateFeatures()
        {
            var cancellation = _cancellation;
            await Task.Run(async () =>
            {
                var scale = _mapView.GetCurrentViewpoint(ViewpointType.CenterAndScale).TargetScale;
                var extent = (Envelope)_mapView.GetCurrentViewpoint(ViewpointType.BoundingGeometry).TargetGeometry;
                (var level, var tiles) = _tileCacheTracker.GetTiles(scale, extent);

                var allRequests = new List<Task>();
                foreach (var tile in tiles)
                {
                    if (level.IsTileCached(tile.Position))
                    {
                        //Debug.WriteLine($"*** Skipping cached tile {tile.LevelOfDetail.Level}: {tile.Position}; {tile.Envelope} ***");
                        continue;
                    }

                    _tileHighlighter.HighlightTile(tile);
                    //Debug.WriteLine($"--- Begining request for tile {tile.LevelOfDetail.Level}: {tile.Position}; {tile.Envelope} ---");

                    foreach (var fl in _mapView.Map.OperationalLayers.OfType<FeatureLayer>())
                    {
                        allRequests.Add(RequestForLayer(fl, tile).ContinueWith((count) =>
                        {
                            if (!cancellation.IsCancellationRequested)
                            {
                                level.FeatureCount += count.Result;
                                Device.BeginInvokeOnMainThread(() =>
                                {
                                    FeatureCount = level.FeatureCount;
                                });
                            }
                        }));
                    }

                    level.MarkTileAsCached(tile.Position);
                }

                await Task.WhenAll(allRequests);
            });
        }

        private async Task<int> RequestForLayer(FeatureLayer fl, Tile tile)
        {
            if (fl.FeatureTable is ServiceFeatureTable serviceTable)
            {
                //Debug.WriteLine($"- Begining request for layer {fl.Name} -");
                int offset = 0;

                while (true)
                {
                    var result = await serviceTable.PopulateFromServiceAsync(new QueryParameters()
                    {
                        Geometry = tile.Envelope,
                        ResultOffset = offset
                    }, false, Array.Empty<string>(), _cancellation.Token);
                    var count = result.Count();
                    //Debug.WriteLine($"{count} features returned.");

                    offset += count;

                    if (!result.IsTransferLimitExceeded)
                    {
                        break;
                    }
                }

                return offset;
            }

            return 0;
        }

    }
}
