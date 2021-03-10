using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Xamarin.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace TestQueryFeatures
{
    public partial class MainPage : ContentPage
    {
        private GraphicsOverlay _overlay;
        private LineSymbol _querySymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.FromArgb(128, Color.Purple), 4);
        private Stopwatch _drawTimer = new Stopwatch();
        private bool _runTimer;

        public FeatureLayerTileRequester Tiler { get; private set; }

        public MainPage()
        {
            InitializeComponent();
            SetMap();
            MainMapView.GeoViewTapped += MainMapView_GeoViewTapped;

            var drawStatusChanged = Observable.FromEventPattern<DrawStatusChangedEventArgs>((handler) =>
            {
                MainMapView.DrawStatusChanged += handler;
            }, (handler) =>
            {
                MainMapView.DrawStatusChanged -= handler;
            });

            drawStatusChanged.Where(x => x.EventArgs.Status == DrawStatus.InProgress).ObserveOn(SynchronizationContext.Current).Subscribe(DrawStart);
            drawStatusChanged.Where(x => x.EventArgs.Status == DrawStatus.Completed).Throttle(TimeSpan.FromMilliseconds(2000)).ObserveOn(SynchronizationContext.Current).Subscribe(DrawStop);
        }

        private void DrawStop(EventPattern<DrawStatusChangedEventArgs> e)
        {
            _runTimer = false;
            _drawTimer.Stop();
            DrawStatusLabel.Text = "Draw Complete";
            UpdateDrawTime(TimeSpan.FromSeconds(2));
            _drawTimer.Reset();
        }

        private async void DrawStart(EventPattern<DrawStatusChangedEventArgs> e)
        {
            Debug.WriteLine("START");
            if (!_runTimer)
            {
                _runTimer = true;
                _drawTimer.Start();
                DrawStatusLabel.Text = "Drawing...";
                while (_runTimer)
                {
                    UpdateDrawTime();
                    await Task.Delay(1);
                }
            }
        }

        private async void MainMapView_DrawStatusChanged(object sender, DrawStatusChangedEventArgs e)
        {
            if (e.Status == DrawStatus.Completed)
            {
                _drawTimer.Stop();
                DrawStatusLabel.Text = "Draw Complete";
                UpdateDrawTime();
                _drawTimer.Reset();
            }
            else
            {
                _drawTimer.Start();
                DrawStatusLabel.Text = "Drawing...";
                while (MainMapView.DrawStatus == DrawStatus.InProgress)
                {
                    UpdateDrawTime();
                    await Task.Delay(1);
                }
            }
        }

        private void UpdateDrawTime(TimeSpan? subtract = null)
        {
            var ellapsed = subtract.HasValue ? _drawTimer.Elapsed.Subtract(subtract.Value) : _drawTimer.Elapsed;
            DrawTimeLabel.Text = $"{ellapsed.Minutes:00}:{ellapsed.Seconds:00}:{ellapsed.Milliseconds:000}";
        }

        private async void SetMap()
        {
            MainMapView.Map = await Map.LoadFromUriAsync(new Uri("https://latitudegeo.maps.arcgis.com/home/item.html?id=ae66491bf2c0422ca3b89c5277fcf04d"));
            Tiler = new FeatureLayerTileRequester(MainMapView);
            OnPropertyChanged(nameof(Tiler));
        }

        private async void MainMapView_GeoViewTapped(object sender, Esri.ArcGISRuntime.Xamarin.Forms.GeoViewInputEventArgs e)
        {
            Reset();

            var layer = MainMapView.Map.OperationalLayers.OfType<FeatureLayer>().FirstOrDefault(X => X.Id == "Victoria_Buildings_4805");
            if (layer == null)
            {
                return;
            }

            var queryResults = await layer.FeatureTable.QueryFeaturesAsync(new Esri.ArcGISRuntime.Data.QueryParameters()
            {
                Geometry = GeometryEngine.Buffer(e.Location, 20 * MainMapView.UnitsPerPixel)
            });
            foreach (var result in queryResults.ToArray())
            {
                _overlay.Graphics.Add(new Graphic(result.Geometry, _querySymbol));
            }
        }

        private void Reset()
        {
            EnsureOverlay();

            _overlay.Graphics.Clear();
        }

        private void EnsureOverlay()
        {
            if (_overlay == null)
            {
                _overlay = new GraphicsOverlay();
                MainMapView.GraphicsOverlays.Add(_overlay);
            }
        }

        private void FeatuerLayerTilingSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            if (e.Value)
            {
                Tiler.Start();
            }
            else
            {
                Tiler.Stop();
                Tiler.Reset();
            }
        }

        private void ShowTilesSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            if (e.Value)
            {
                Tiler.ShowTiles();
            }
            else
            {
                Tiler.HideTiles();
            }
        }
    }
}
