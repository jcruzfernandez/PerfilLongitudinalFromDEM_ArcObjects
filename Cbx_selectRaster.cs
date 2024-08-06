using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.Geodatabase;
using System;

namespace perfilLongitudinal
{
    public class Cbx_selectRaster : ESRI.ArcGIS.Desktop.AddIns.ComboBox
    {
        private IActiveViewEvents_Event activeViewEvents;
        public static string selectedLayerName;
        public static IFeatureLayer selectedLayer;
        //private Type factoryType;
        //public static string currentPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string pathTool = System.IO.Path.Combine(settings.currentPath, "scripts", "ToolboxMain.tbx");

        public Cbx_selectRaster()
        {
            // Obtener el documento de ArcMap y el mapa enfocado
            IMxDocument mxDoc = (IMxDocument)ArcMap.Application.Document;
            IMap map = mxDoc.FocusMap;
            
            // Obtener el evento ActiveViewEvents del mapa
            activeViewEvents = (IActiveViewEvents_Event)map;

            // Suscribirse al evento ItemAdded
            activeViewEvents.ItemAdded += OnLayerAdded;

            // Inicializar el ComboBox con las capas existentes
            UpdateComboBox();
        }

        private void OnLayerAdded(object item)
        {
            // Actualizar el ComboBox cuando se agrega una capa
            UpdateComboBox();
        }

        private void UpdateComboBox()
        {
            // Limpiar los ítems existentes en el ComboBox
            this.Clear();
            try
            {
                // Obtener el documento de ArcMap y el mapa enfocado
                IMxDocument mxDoc = (IMxDocument)ArcMap.Application.Document;
                IMap map = mxDoc.FocusMap;

                // Agregar todas las capas del mapa al ComboBox
                for (int i = 0; i < map.LayerCount; i++)
                {
                    ILayer layer = map.get_Layer(i);
                    if (layer is IRasterLayer)
                    {
                        // Obtener la ruta del archivo de la capa raster
                        IDataLayer dataLayer = layer as IDataLayer;
                        IGeoDataset geoDataset;
                        geoDataset = layer as IGeoDataset;
                        settings.spatialReference = geoDataset.SpatialReference;
                        settings.wkid = geoDataset.SpatialReference.FactoryCode;
                        IDatasetName datasetName = dataLayer.DataSourceName as IDatasetName;
                        IWorkspaceName workspaceName = datasetName.WorkspaceName;
                        string folder = workspaceName.PathName;
                        // Obtener el nombre del archivo raster automáticamente
                        string rasterName = datasetName.Name; //+ "." + datasetName.Extension;

                        // Comprobar el tipo de espacio de trabajo
                        IWorkspaceFactory workspaceFactory;
                        IWorkspace workspace;
                        IRasterBandCollection rasterBandCollection = null;

                        if (workspaceName.WorkspaceFactoryProgID.Contains("esriDataSourcesRaster.RasterWorkspaceFactory"))
                        {
                            // Caso de archivo raster (.tiff)
                            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesRaster.RasterWorkspaceFactory");
                            workspaceFactory = Activator.CreateInstance(factoryType) as IWorkspaceFactory;
                            IRasterWorkspace2 rasterWorkspace = (IRasterWorkspace2)workspaceFactory.OpenFromFile(folder, 0);
                            IRasterDataset2 rasterDataset = (IRasterDataset2)rasterWorkspace.OpenRasterDataset(rasterName);
                            rasterBandCollection = (IRasterBandCollection)rasterDataset;
                        }
                        else if (workspaceName.WorkspaceFactoryProgID.Contains("esriDataSourcesGDB.FileGDBWorkspaceFactory") ||
                                 workspaceName.WorkspaceFactoryProgID.Contains("esriDataSourcesGDB.AccessWorkspaceFactory") ||
                                 workspaceName.WorkspaceFactoryProgID.Contains("esriDataSourcesGDB.SdeWorkspaceFactory"))
                        {
                            // Caso de imagen raster dentro de una geodatabase
                            workspaceFactory = Activator.CreateInstance(Type.GetTypeFromProgID(workspaceName.WorkspaceFactoryProgID)) as IWorkspaceFactory;
                            workspace = workspaceFactory.Open(workspaceName.ConnectionProperties, 0);
                            IRasterWorkspaceEx rasterWorkspaceEx = workspace as IRasterWorkspaceEx;
                            IRasterDataset rasterDataset = rasterWorkspaceEx.OpenRasterDataset(rasterName);
                            rasterBandCollection = rasterDataset as IRasterBandCollection;
                        }
                        else
                        {
                            throw new Exception("Tipo de espacio de trabajo no soportado.");
                        }

                        // Añadir el rasterLayer al ComboBox si es un DEM
                        if (rasterBandCollection.Count == 1)
                        {
                            this.Add(layer.Name, layer);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Error: " + ex.Message);
            }
        }
        protected override void OnUpdate()
        {
            Enabled = ArcMap.Application != null;
        }

        protected override void OnSelChange(int index)
        {
            if (index != -1)
            {
                var selectedLayer = this.GetItem(index);
                IDataLayer dataLayer = selectedLayer.Tag as IDataLayer;// selectedLyr.Tag as IFeatureLayer;
                IDatasetName datasetName = dataLayer.DataSourceName as IDatasetName;
                IWorkspaceName workspaceName = datasetName.WorkspaceName;
                string folder = workspaceName.PathName;
                string rasterName = datasetName.Name;
                settings.rasterDEMPath = System.IO.Path.Combine(folder,rasterName);
            }
        }

        ~Cbx_selectRaster()
        {
            // Remover el manejador de eventos para evitar memory leaks
            if (activeViewEvents != null)
            {
                activeViewEvents.ItemAdded -= OnLayerAdded;
            }
        }

    }

}
