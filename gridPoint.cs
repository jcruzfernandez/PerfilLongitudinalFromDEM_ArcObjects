using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using System.Windows.Forms;
using System;

namespace perfilLongitudinal
{
    public class gridPoint : ESRI.ArcGIS.Desktop.AddIns.Tool
    {
        public gridPoint()
        {
        }

        protected override void OnActivate()
        {
            Cursor = Cursors.Cross;
        }

        protected override void OnMouseDown(MouseEventArgs arg)
        {
 
            IMxDocument pMxDoc = (IMxDocument)ArcMap.Application.Document;
            IGraphicsContainer graphicsContainer = (IGraphicsContainer)pMxDoc.FocusMap;
            graphicsContainer.DeleteAllElements();
            IActiveView activeView = pMxDoc.ActiveView;
            IScreenDisplay screenDisplay = activeView.ScreenDisplay;
            screenDisplay.StartDrawing(screenDisplay.hDC, (short)esriScreenCache.esriNoScreenCache);

            IRgbColor rgbColor = new RgbColorClass();
            rgbColor.Red = 255;
            rgbColor.Transparency = 0;

            IColor color = rgbColor; // Implicit Cast
            ISimpleFillSymbol simpleFillSymbol = new SimpleFillSymbolClass();
            simpleFillSymbol.Color = color;
            simpleFillSymbol.Style = esriSimpleFillStyle.esriSFSNull;

            ISymbol symbol = (ISymbol)simpleFillSymbol; // Dynamic Cast
            IRubberBand rubberBand = new RubberRectangularPolygonClass();
            ESRI.ArcGIS.Geometry.IGeometry geometry = rubberBand.TrackNew(screenDisplay, symbol);

            try
            {
                screenDisplay.SetSymbol(symbol);
                screenDisplay.DrawPolygon(geometry);

                settings.drawPolygon_x_min = geometry.Envelope.XMin;
                settings.drawPolygon_y_min = geometry.Envelope.YMin;

                IElement element = new PolygonElementClass();
                element.Geometry = geometry;
                IPolygonElement polElement = (IPolygonElement)element;
                IFillShapeElement fillShapeElement = (IFillShapeElement)polElement;
                fillShapeElement.Symbol = simpleFillSymbol;

                graphicsContainer.AddElement((IElement)fillShapeElement, 0);
                activeView.Refresh();
            }
            catch (Exception ex)
            {
                // Manejo de excepción
                System.Windows.Forms.MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                screenDisplay.FinishDrawing();
            }

            base.OnMouseDown(arg);
        }

        protected override void OnUpdate()
        {
            //Enabled = Cbx_selectRaster.selectedLayer != null;
            //Enabled = settings.drawPolygon_y_min != 0;
            Enabled = ArcMap.Application != null;
        }
    }
}
