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
        private LineSymbol _querySymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.FromArgb(128, Color.Orange), 4);

        public MainPage()
        {
            InitializeComponent();
            SetMap();
            InitUI();
            MainMapView.GeoViewTapped += MainMapView_GeoViewTapped;
        }

        private async void SetMap()
        {
            MainMapView.Map = await Map.LoadFromUriAsync(new Uri("https://latitudegeo.maps.arcgis.com/home/item.html?id=335c18b8e2344c44ab8e126412432dea"));
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

            var layer = (FeatureLayer)MainMapView.Map.OperationalLayers[0];
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
    }
}
