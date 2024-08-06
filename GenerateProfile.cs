using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geoprocessing;
using System;
using System.Windows.Forms;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Carto;

namespace perfilLongitudinal
{
    public class GenerateProfile : ESRI.ArcGIS.Desktop.AddIns.Tool
    {
        private string tolerancia= "100";
        public GenerateProfile()
        {
        }

        protected override void OnUpdate()
        {
            Enabled = ArcMap.Application != null;
        }

        protected override void OnActivate()
        {
            // Mostrar ventana de confirmación
            DialogResult result = MessageBox.Show("¿Desea proceder a generar el perfil?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.No)
            {
                // Si el usuario elige "No", no se ejecuta el geoproceso y se deja el botón habilitado para presionar nuevamente
                return;
            }
            Cursor = Cursors.WaitCursor;

            // Crear y mostrar una ventana modal que bloquee ArcMap
            Form waitForm = new Form();
            waitForm.Text = "Espere mientras se ejecuta el geoproceso...";
            waitForm.ControlBox = false;
            waitForm.StartPosition = FormStartPosition.CenterScreen;
            waitForm.Width = 300;
            waitForm.Height = 100;
            Label label = new Label();
            label.Text = "Procesando...";
            label.Dock = DockStyle.Fill;
            label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            waitForm.Controls.Add(label);

            // Geoproceso de generacion de perfil
            IGeoProcessor2 geoprocessor = new GeoProcessorClass();
            // Opcional: Configura el geoprocesador para sobrescribir la salida
            geoprocessor.OverwriteOutput = true;
            // Agregar el geoproceso al historial de geoprocesamiento de ArcMap
            geoprocessor.AddToResults = true;
            try
            {
                waitForm.Show();
                waitForm.Refresh();
                
                geoprocessor.AddToolbox(settings._toolboxPath_);
                // Crea un objeto IVariantArray para almacenar los parámetros de la herramienta
                IVariantArray parameters = new VarArrayClass();
                parameters.Add(settings.rasterDEMPath);
                parameters.Add(settings.drawLine_wkt);
                parameters.Add(settings.wkid);
                parameters.Add(tolerancia);
                parameters.Add(settings.drawPolygon_x_min);
                parameters.Add(settings.drawPolygon_y_min);
                IGeoProcessorResult results = geoprocessor.Execute("generateProfile", parameters, null);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error al ejecutar la herramienta: " + e.Message);
                // Manejo adicional de errores aquí
            }
            finally
            {
                waitForm.Close();
                Cursor = Cursors.Default;
                IMxDocument pMxDoc = (IMxDocument)ArcMap.Application.Document;
                IGraphicsContainer graphicsContainer = (IGraphicsContainer)pMxDoc.FocusMap;
                graphicsContainer.DeleteAllElements();
            }
        }
    }
}
