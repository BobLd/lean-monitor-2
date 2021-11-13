using Dock.Model;
using System;
using System.IO;

namespace Panoptes.ViewModels
{
    public class LayoutManager : ILayoutManager
    {
        public void LoadLayout(DockManager manager)
        {
            if (File.Exists(LayoutFileName))
            {
                LoadLayout(manager, LayoutFileName);
            }
            else
            {
                ResetLayout(manager);
            }
        }

        public void ResetLayout(DockManager manager)
        {
            if (!File.Exists(DefaultLayoutFileName)) return;

            LoadLayout(manager, DefaultLayoutFileName);
        }

        public void SaveLayout(DockManager manager)
        {
            // Craete the folder if it does not exist yet
            if (!Directory.Exists(DataFolder)) Directory.CreateDirectory(DataFolder);
            if (File.Exists(LayoutFileName)) File.Delete(LayoutFileName);

            // Serialize the layout
            //var serializer = new XmlLayoutSerializer(manager);
            //serializer.Serialize(LayoutFileName);
        }

        private static void LoadLayout(DockManager manager, string fileName)
        {
            //var serializer = new XmlLayoutSerializer(manager);
            //serializer.Deserialize(fileName);
        }

        private static string DataFolder => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar + "Lean" + Path.DirectorySeparatorChar + "Monitor";

        private static string LayoutFileName => DataFolder + Path.DirectorySeparatorChar + "layout.xml";

        private static string DefaultLayoutFileName => "layout.default.xml";
    }
}
