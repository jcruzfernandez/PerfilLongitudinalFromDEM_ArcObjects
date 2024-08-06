using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geometry;
using System;
using System.Reflection;
using System.Windows.Forms;

namespace perfilLongitudinal
{
    class settings
    {
        // Variables que obtienen informacion sobre desarrollo, fecha, etc.
        public static string __title__ = "PerfilLongitudinalFromDEM";
        public static string __author__ = "Julio C. Cruz Fernandez";
        public static string __copyright__ = "Julio Cruz 2024";
        public static string __credits__ = "Julio C. Cruz Fernandez";
        public static string __version__ = "1.0";
        public static string __maintainer__ = __credits__;
        public static string __mail__ = "juliocruz552@gmail.com";
        public static string __status__ = "Production";
        public static string __tempdir__ = System.IO.Path.GetTempPath();

        // Obtiene la ruta actual en donde se almacena la instalacion del addin
        public static string currentPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string _scripts_path = System.IO.Path.Combine(currentPath, "scripts");

        // Varibles globales dinamicas
        public static string drawLine_wkt;
        public static double drawPolygon_x_min = 0;
        public static double drawPolygon_y_min = 0;
        public static ISpatialReference spatialReference;
        public static int wkid;
        public static string rasterDEMPath;

        // Construye la ruta donde se encuentra el archivo *.tbx
        public static string _toolboxPath_ = System.IO.Path.Combine(_scripts_path, "ToolboxMain.tbx");

        // Nombre de herramientas del tbx _toolboxPath
        public static string tool_getPythonPath = "getPythonPath";
        public static string tool_generateProfile = "generateProfile";

        public static void EliminarCapaTemp(string layerNameTemp)
        {
            // Obtener el documento de ArcMap y el mapa enfocado
            IMxDocument mxDoc = (IMxDocument)ArcMap.Application.Document;
            IMap map = mxDoc.FocusMap;
            // IFeatureClass featureClass;
            // Agregar todas las capas del mapa al ComboBox
            for (int i = 0; i < map.LayerCount; i++)
            {
                ILayer layer = map.get_Layer(i);
                if (layer is IFeatureLayer)
                {
                    IFeatureLayer featureLayer = (IFeatureLayer)layer;
                    if (layer.Name.Equals(layerNameTemp, StringComparison.OrdinalIgnoreCase))
                    {
                        map.DeleteLayer(layer);
                        break; // Salir del bucle después de eliminar la capa
                    }
                }
            }
        }
    }

    public class SettingsForm : Form
    {
        public SettingsForm()
        {
            this.Text = "Settings Information";
            this.Width = 300;
            this.Height = 150;
            this.ControlBox = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            //this.Width = 300;
            //this.Height = 300;
            string info = $"Title: {settings.__title__}\n" +
                          $"Author: {settings.__author__}\n" +
                          $"Copyright: {settings.__copyright__}\n" +
                          $"Credits: {settings.__credits__}\n" +
                          $"Version: {settings.__version__}\n" +
                          $"Maintainer: {settings.__maintainer__}\n" +
                          $"Email: {settings.__mail__}\n" +
                          $"Status: {settings.__status__}";

            Label label = new Label()
            {
                Text = info,
                Dock = DockStyle.Fill,
                AutoSize = true,
                TextAlign= System.Drawing.ContentAlignment.MiddleLeft
            };

            this.Controls.Add(label);
        }
    }
}
