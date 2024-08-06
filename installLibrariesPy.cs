using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using ESRI.ArcGIS.Desktop.AddIns;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.esriSystem;

namespace perfilLongitudinal
{
    public class installLibrariesPy : ESRI.ArcGIS.Desktop.AddIns.Extension
    {
        public installLibrariesPy()
        {
        }
        IGeoProcessor2 geoprocessor = new GeoProcessorClass();
        protected override void OnStartup()
        {
            //base.OnStartup();
            InitializeArcObjects();
            Task.Run(() => InstallLibrariesAsync());
        }

        private void InitializeArcObjects()
        {
            ESRI.ArcGIS.esriSystem.AoInitialize aoInit = null;
            aoInit = new AoInitializeClass();
            esriLicenseStatus licenseStatus = aoInit.Initialize(esriLicenseProductCode.esriLicenseProductCodeAdvanced);
            if (licenseStatus != esriLicenseStatus.esriLicenseCheckedOut)
            {
                throw new Exception("No se pudo inicializar la licencia de ArcObjects.");
            }
        }
        private async Task InstallLibrariesAsync()
        {
            string pythonPath = GetPythonPath(geoprocessor);// @"C:\Python27\ArcGIS10.8\python.exe"; // Ruta al ejecutable de Python
            string requirementsDir = Path.Combine(settings.currentPath, @"scripts\require");

            if (Directory.Exists(requirementsDir))
            {
                string[] files = Directory.GetFiles(requirementsDir, "*.whl");
                foreach (string file in files)
                {
                    await InstallLibraryAsync(pythonPath, file);
                }

                files = Directory.GetFiles(requirementsDir, "*.tar.gz");
                foreach (string file in files)
                {
                    await InstallLibraryAsync(pythonPath, file);
                }
            }
            else
            {
                MessageBox.Show("La carpeta de requisitos no existe.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Task InstallLibraryAsync(string pythonPath, string libraryPath)
        {
            return Task.Run(() =>
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = $"-m pip install \"{libraryPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.OutputDataReceived += (sender, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
                    process.ErrorDataReceived += (sender, e) => { if (e.Data != null) Console.WriteLine("ERROR: " + e.Data); };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                }
            });
        }

        //public async Task<string> GetPythonPath()
        public string GetPythonPath(IGeoProcessor2 gp)
        {
            //return await Task.Run(() =>
            //{
                try
                {
                    // Inicializar el geoprocesador
                    //IGeoProcessor2 gp = new GeoProcessorClass();
                    // Opcional: Configura el geoprocesador para sobrescribir la salida
                    gp.OverwriteOutput = true;
                    // Agregar el geoproceso al historial de geoprocesamiento de ArcMap
                    gp.AddToResults = true;
                    // Agrega el PythonPath toolbox.
                    gp.AddToolbox(settings._toolboxPath_);

                    // Necesita una matriz vacía aunque no tengamos parámetros de entrada
                    IVariantArray parameters = new VarArrayClass();

                    // Ejecutar la herramienta por nombre.
                    var result = gp.Execute(settings.tool_getPythonPath, parameters, null);
                    return result.GetOutput(0).GetAsText();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al obtener la ruta de Python: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }
            //});
        }

        protected override void OnShutdown()
        {
            base.OnShutdown();
        }
    }

}
