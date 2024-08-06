using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using System.Windows.Forms;
using ESRI.ArcGIS.esriSystem;
using System;
using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Geodatabase;
using System.Collections.Generic;
using ESRI.ArcGIS.DataSourcesGDB;

namespace perfilLongitudinal
{
    public class DrawLineLongitudinal : ESRI.ArcGIS.Desktop.AddIns.Tool
    {
        private INewLineFeedback newLineFeedBack;
        private IActiveView activeView;
        private IGraphicsContainer graphicsContainer;
        IMap mapa;
        //List<IPoint> puntosConIndice = new List<IPoint>();
        private IScreenDisplay screenDisplay;
        bool mousePresionado;
        string layerNameTemp = "Eje_Longitudinal";
        IPointCollection pointCollection;
        public DrawLineLongitudinal()
        {
        }

        protected override void OnActivate()
        {
            // Cambiar el cursor a una cruz cuando la herramienta se activa
            Cursor = Cursors.Cross;
        }

        private ISimpleLineSymbol CreateLineSymbol()
        {
            IRgbColor color = new RgbColorClass
            {
                Red = 0,
                Green = 255,
                Blue = 255
            };

            ISimpleLineSymbol lineSymbol = new SimpleLineSymbolClass
            {
                Color = color,
                Width = 1,
                Style = esriSimpleLineStyle.esriSLSDash,
            };

            return lineSymbol;
        }

        protected override void OnMouseDown(MouseEventArgs arg)
        {
            graphicsContainer = null;
            // Definimos vista y mapa
            activeView = ArcMap.Document.ActivatedView;
            mapa = ArcMap.Document.FocusMap;
            screenDisplay = activeView.ScreenDisplay;
            graphicsContainer = (IGraphicsContainer)mapa;
            
            // Eliminar la línea existente antes de comenzar una nueva
            graphicsContainer.DeleteAllElements();
            activeView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
            // Creamos un punto con las coordendas del clic en el mapa
            IPoint point = activeView.ScreenDisplay.DisplayTransformation.ToMapPoint(arg.X, arg.Y);
            if (newLineFeedBack == null)
            {
                newLineFeedBack = new NewLineFeedbackClass();
                newLineFeedBack.Display = screenDisplay;              
                newLineFeedBack.Start(point);
            }
            else
            {
                //return;
                newLineFeedBack.AddPoint(point);
            }
            mousePresionado = true;
            // Refresh view to draw the feedback
            //activeView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
        }

        protected override void OnDoubleClick()
        {
            settings.EliminarCapaTemp("Eje_Longitudinal");
            activeView = ArcMap.Document.ActivatedView;
            mapa = ArcMap.Document.FocusMap;

            if (newLineFeedBack != null)
            {
                IPolyline polyline = newLineFeedBack.Stop();
                pointCollection = (IPointCollection)polyline;

                if (polyline != null)
                {
                    // Agrega el gráfico de polilínea al mapa
                    IElement element = new LineElementClass
                    {
                        Geometry = polyline,
                        Symbol = (ILineSymbol)CreateLineSymbol()
                    };

                    graphicsContainer = activeView.GraphicsContainer;
                    graphicsContainer.AddElement(element, 0);
                    activeView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);

                    string[] aryTextFile = new string[pointCollection.PointCount];
                    for (int i = 0; i < pointCollection.PointCount; i++)
                    {
                        IPoint point = pointCollection.get_Point(i);
                        aryTextFile[i] = string.Format("{0} {1}", point.X, point.Y);
                    }
                    settings.drawLine_wkt = string.Format("LINESTRING ({0})", string.Join(",", aryTextFile));
                    CrearFeatureLayerEnMemoria(polyline);
                }
            }
            mousePresionado = false;
            newLineFeedBack = null;
        }

        protected override void OnMouseMove(MouseEventArgs arg)
        {
            if (mousePresionado)
            {
                // Efecto de movimiento con el mouse
                IPoint punto = activeView.ScreenDisplay.DisplayTransformation.ToMapPoint(arg.X, arg.Y);
                newLineFeedBack.MoveTo(punto);
            }
            else
            {
                return;
            }
        }

        public IFeatureLayer CrearFeatureLayerEnMemoria(IPolyline polyline)
        {
            // Obtener el documento actual de ArcMap
            IMxDocument mxDocument = ArcMap.Application.Document as IMxDocument;
            IMap map = mxDocument.FocusMap;
            IFeatureLayer featureLayer = null;

            // Crear un workspace en memoria
            IWorkspaceFactory workspaceFactory = new InMemoryWorkspaceFactoryClass();
            IWorkspaceName workspaceName = workspaceFactory.Create("", "GPInMemoryWorkspace", null, 0);
            IName name = (IName)workspaceName;
            IWorkspace inMemoryWorkspace = (IWorkspace)name.Open();

            // Crear un FeatureClass en memoria
            IFields fields = new FieldsClass();
            IFieldsEdit fieldsEdit = (IFieldsEdit)fields;

            // Crear un campo para el OID
            IField idField = new FieldClass();
            IFieldEdit idFieldEdit = (IFieldEdit)idField;
            idFieldEdit.Name_2 = "OBJECTID";
            idFieldEdit.Type_2 = esriFieldType.esriFieldTypeOID;
            idFieldEdit.IsNullable_2 = false;
            idFieldEdit.Required_2 = false;
            fieldsEdit.AddField(idField);

            //Crear campo ORDEN
            IField ordField = new FieldClass();
            IFieldEdit ordFieldEdit = (IFieldEdit)ordField;
            ordFieldEdit.Name_2 = "LABEL";
            ordFieldEdit.Type_2 = esriFieldType.esriFieldTypeString;
            ordFieldEdit.AliasName_2 = "ETIQUETA";
            fieldsEdit.AddField(ordField);

            // Crear un campo de geometría para los puntos
            IField geometryField = new FieldClass();
            IFieldEdit geometryFieldEdit = (IFieldEdit)geometryField;
            //geometryFieldEdit.Name_2 = "SHAPE";
            //geometryFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;

            IGeometryDef geometryDef = new GeometryDefClass();
            IGeometryDefEdit geometryDefEdit = (IGeometryDefEdit)geometryDef;
            geometryDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolyline;
            geometryDefEdit.SpatialReference_2 = settings.spatialReference;
            geometryFieldEdit.Name_2 = "SHAPE";
            geometryFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;
            geometryFieldEdit.GeometryDef_2 = geometryDef;
            fieldsEdit.AddField(geometryField);

            // Crear el FeatureClass
            IFeatureClass featureClassmain = ((IFeatureWorkspace)inMemoryWorkspace).CreateFeatureClass("Eje_Longitudinal", fields, null, null, esriFeatureType.esriFTSimple, "SHAPE", "");

            // Añadir la polilínea al FeatureClass
            IFeature feature = featureClassmain.CreateFeature();
            feature.set_Value(feature.Fields.FindField("LABEL"), "Eje_Longitudinal");
            feature.Shape = polyline;
            feature.Store();

            // Crear un FeatureLayer a partir del FeatureClass
            IFeatureLayer featureLayermain = new FeatureLayerClass();
            featureLayermain.FeatureClass = featureClassmain;
            featureLayermain.Name = layerNameTemp;

            // Crear una simbologia
            IGeoFeatureLayer geoFeatureLayer = featureLayermain as IGeoFeatureLayer;
            ISimpleRenderer simpleRenderer = new SimpleRendererClass();
            simpleRenderer.Symbol = (ISymbol)CreateLineSymbol();
            geoFeatureLayer.Renderer = simpleRenderer as IFeatureRenderer;

            // Agregar el feature layer creado al mapa
            if (map != null && featureLayermain != null)
            {
                map.AddLayer(featureLayermain);
            }

            // Refrescar la vista de ArcMap para mostrar el nuevo layer
            mxDocument.UpdateContents();
            mxDocument.ActiveView.Refresh();

            return featureLayer;
        }

        protected override void OnUpdate()
        {
            //Enabled = Cbx_selectRaster.selectedLayer != null;
            Enabled = ArcMap.Application != null;
        }
    }

}
