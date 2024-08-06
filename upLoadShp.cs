using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Catalog;
using ESRI.ArcGIS.CatalogUI;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System.Windows.Forms;

namespace perfilLongitudinal
{
    public class upLoadShp : ESRI.ArcGIS.Desktop.AddIns.Tool
    {
        public upLoadShp()
        {
        }

        protected override void OnActivate()
        {
            GxDialog pGxDialog = new GxDialogClass();
            IGxObjectFilterCollection filters = pGxDialog as IGxObjectFilterCollection;
            filters.AddFilter(new GxFilterFGDBFeatureClasses(), true);
            filters.AddFilter(new GxFilterFeatureClassesClass(), true);
            filters.AddFilter(new GxFilterShapefilesClass(), true); // GxFilterFGDBFeatureClasses();
            //pGxDialog.ObjectFilter = mdbFilter;
            //pGxDialog.ObjectFilter = shpFilter;
            pGxDialog.Title = "Seleccione un Shapefile o FeatureClass";
            pGxDialog.RememberLocation = true;
            IEnumGxObject pEnumGx;
            if (pGxDialog.DoModalOpen(0, out pEnumGx) == true)
            {
                IGxObject gdbObj = pEnumGx.Next();
                IName fcName = gdbObj.InternalObjectName;
                string filePath = gdbObj.FullName;
                IFeatureLayer featureLayer = null;
                if (filePath.EndsWith(".shp"))
                {
                    featureLayer = LoadShapefile(filePath);
                }
                else// if (filePath.EndsWith(".gdb"))
                {
                    featureLayer = LoadFeatureClassFromGDB(filePath, gdbObj);
                }
                if (featureLayer != null && featureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                {
                    ApplyRedLineSymbol(featureLayer);
                    AddLayerToMap(featureLayer);
                }
                else
                {
                    MessageBox.Show("Por favor seleccione un Shapefile o FeatureClass de tipo polilínea.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

        }

        private IFeatureLayer LoadShapefile(string shapefilePath)
        {
            IWorkspaceFactory workspaceFactory = new ShapefileWorkspaceFactoryClass();
            string folderPath = System.IO.Path.GetDirectoryName(shapefilePath);
            string shapefileName = System.IO.Path.GetFileNameWithoutExtension(shapefilePath);
            IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspaceFactory.OpenFromFile(folderPath, 0);
            IFeatureClass featureClass = featureWorkspace.OpenFeatureClass(shapefileName);
            IFeatureLayer featureLayer = new FeatureLayerClass
            {
                FeatureClass = featureClass,
                Name = featureClass.AliasName
            };
            return featureLayer;
        }

        private IFeatureLayer LoadFeatureClassFromGDB(string gdbPath, IGxObject gxObject)
        {
            IWorkspaceFactory workspaceFactory = new FileGDBWorkspaceFactoryClass();
            //IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspaceFactory.OpenFromFile(gdbPath, 0);
            // Suponiendo que solo haya una feature class en el gdb para simplicidad
            //IEnumDataset enumDataset = featureWorkspace.OpenFeatureDataset(gdbPath);
            //enumDataset.Reset();
            //IDataset dataset = enumDataset.Next();
            //if (dataset == null)
            //{
            //    return null;
            //}

            //IFeatureClass featureClass = featureWorkspace.OpenFeatureClass(gdbPath);
            IFeatureLayer featureLayer = new FeatureLayerClass();
            if (gxObject is IGxDataset gxDataset)
            {
                IDatasetName datasetName = gxDataset.DatasetName;
                IName name = (IName)datasetName;
                IFeatureClass featureClass = (IFeatureClass)name.Open();
                featureLayer.FeatureClass = featureClass;
                featureLayer.Name = gxObject.Name;
            }
            return featureLayer;
        }

        private void ApplyRedLineSymbol(IFeatureLayer featureLayer)
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
                Width = 2,
                Style = esriSimpleLineStyle.esriSLSDash
            };

            IGeoFeatureLayer geoFeatureLayer = (IGeoFeatureLayer)featureLayer;
            ISimpleRenderer simpleRenderer = new SimpleRendererClass
            {
                Symbol = (ISymbol)lineSymbol
            };
            geoFeatureLayer.Renderer = (IFeatureRenderer)simpleRenderer;
        }

        private void AddLayerToMap(IFeatureLayer featureLayer)
        {
            IMap map = ArcMap.Document.FocusMap;
            map.AddLayer((ILayer)featureLayer);
            ArcMap.Document.ActiveView.Refresh();
        }

        protected override void OnUpdate()
        {
            Enabled = ArcMap.Application != null;
        }
    }
}
