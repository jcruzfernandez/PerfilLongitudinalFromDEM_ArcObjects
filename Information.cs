using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace perfilLongitudinal
{
    public class Information : ESRI.ArcGIS.Desktop.AddIns.Button
    {
        public Information()
        {
        }

        protected override void OnClick()
        {
            SettingsForm settingsForm = new SettingsForm();
            settingsForm.ShowDialog();
        }

        protected override void OnUpdate()
        {
            Enabled = ArcMap.Application != null;
        }
    }
}
