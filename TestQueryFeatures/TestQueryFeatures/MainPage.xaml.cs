using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Xamarin.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace TestQueryFeatures
{
    public partial class MainPage : ContentPage
    {
        private GraphicsOverlay _overlay;
        private LineSymbol _identifySymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.FromArgb(128, Color.Cyan), 2);
        private LineSymbol _querySymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.FromArgb(128, Color.Purple), 4);

        public MainPage()
        {
            InitializeComponent();
            SetMap();
            InitUI();
            MainMapView.GeoViewTapped += MainMapView_GeoViewTapped;
        }

        private async void SetMap()
        {
            MainMapView.ViewpointChanged += MainMapView_ViewpointChanged;
            MainMapView.NavigationCompleted += MainMapView_NavigationCompleted;
            MainMapView.Map = await Map.LoadFromUriAsync(new Uri("https://latitudegeo.maps.arcgis.com/home/item.html?id=c6008288a95247428fc55d9aaa72b670"));

            foreach (var fl in MainMapView.Map.OperationalLayers.OfType<FeatureLayer>())
            {
                if (fl.FeatureTable is ServiceFeatureTable serviceTable)
                {
                    serviceTable.FeatureRequestMode = FeatureRequestMode.ManualCache;
                }
            }
        }

        private void MainMapView_ViewpointChanged(object sender, EventArgs e)
        {
            MainMapView.ViewpointChanged -= MainMapView_ViewpointChanged;
            UpdateFeatures();
        }

        private async void MainMapView_NavigationCompleted(object sender, EventArgs e)
        {
            UpdateFeatures();
        }

        private async void UpdateFeatures()
        {
            var extent = MainMapView.GetCurrentViewpoint(ViewpointType.BoundingGeometry).TargetGeometry;
            foreach (var fl in MainMapView.Map.OperationalLayers.OfType<FeatureLayer>())
            {
                if (fl.FeatureTable is ServiceFeatureTable serviceTable)
                {
                    int offset = 0;
                    while (true)
                    {
                        var result = await serviceTable.PopulateFromServiceAsync(new QueryParameters()
                        {
                            Geometry = extent,
                            ResultOffset = offset
                        }, false, serviceTable.Fields.Select(x => x.Name));

                        offset += result.Count();

                        if (!result.IsTransferLimitExceeded)
                        {
                            break;
                        }
                    }
                }
            }
        }

        private async void InitUI()
        {
            var identifySwatch = await _identifySymbol.CreateSwatchAsync();
            IdentifySwatch.Source = await identifySwatch.ToImageSourceAsync();
            var querySwatch = await _querySymbol.CreateSwatchAsync();
            QuerySwatch.Source = await querySwatch.ToImageSourceAsync();
        }

        private async void MainMapView_GeoViewTapped(object sender, Esri.ArcGISRuntime.Xamarin.Forms.GeoViewInputEventArgs e)
        {
            Reset();

            var layer = (FeatureLayer)MainMapView.Map.OperationalLayers.First(X => X.Id == "Victoria_Buildings_4805");
            var queryResults = await layer.FeatureTable.QueryFeaturesAsync(new Esri.ArcGISRuntime.Data.QueryParameters()
            {
                Geometry = GeometryEngine.Buffer(e.Location, 20 * MainMapView.UnitsPerPixel)
            });
            foreach (var result in queryResults.ToArray())
            {
                _overlay.Graphics.Add(new Graphic(result.Geometry, _querySymbol));
            }
            QueryGeometryLabel.Text = string.Join("\n", queryResults.Select(x => FormatGeometryLabel(x.Geometry)));

            if (DoIdentifySwitch.IsToggled)
            {
                var identifyResults = await MainMapView.IdentifyLayersAsync(e.Position, 20, false);
                foreach (var result in identifyResults)
                {
                    foreach (var element in result.GeoElements)
                    {
                        _overlay.Graphics.Add(new Graphic(element.Geometry, _identifySymbol));
                    }
                }
                IdentifyGeometryLabel.Text = string.Join("\n", identifyResults.SelectMany(x => x.GeoElements.Select(y => FormatGeometryLabel(y.Geometry))));
            }

            string FormatGeometryLabel(Geometry g)
            {
                if (g is Multipart parts)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var part in parts.Parts)
                    {
                        sb.Append("[");
                        sb.Append(string.Join(",\n", part.Points.Select(point => $"({point.X},{point.Y})")));
                        sb.Append("];");
                    }
                    return sb.ToString();
                }
                else
                {
                    return g.ToString();
                }
            }
        }

        private void Reset()
        {
            EnsureOverlay();

            _overlay.Graphics.Clear();
            IdentifyGeometryLabel.Text = string.Empty;
        }

        private void EnsureOverlay()
        {
            if (_overlay == null)
            {
                _overlay = new GraphicsOverlay();
                MainMapView.GraphicsOverlays.Add(_overlay);
            }
        }

        private async void FeatuerLayerTilingSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            foreach (var fl in MainMapView.Map.OperationalLayers.OfType<FeatureLayer>())
            {
                fl.TilingMode = e.Value ? FeatureTilingMode.EnabledWhenSupported : FeatureTilingMode.Disabled;
                if (fl.FeatureTable is ServiceFeatureTable serviceTable)
                {
                    serviceTable.ClearCache(true);
                }
            }

            var currentViewpoint = MainMapView.GetCurrentViewpoint(ViewpointType.CenterAndScale);
            var locationCenter = (MapPoint)currentViewpoint.TargetGeometry;
            var screenCenter = MainMapView.LocationToScreen(locationCenter);
            var shiftedLocationCenter = MainMapView.ScreenToLocation(new Point(screenCenter.X + 1, screenCenter.Y));
            MainMapView.SetViewpoint(new Viewpoint(shiftedLocationCenter, currentViewpoint.TargetScale));
        }
    }
}
