using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Output;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.ADF;

namespace MapEdit
{
   
    public static class MapClass
    {
        //IMapDocument对象，并实例化          
        public static IMapDocument pMapDocument = new MapDocumentClass();
        private static System.Windows.Forms.OpenFileDialog openFileDialog2;
        private static System.Windows.Forms.SaveFileDialog SaveFileDialog2;
        
        public static void NewMapDoc(AxMapControl axMapControl1)
        {
           
            SaveFileDialog2 = new SaveFileDialog();
            SaveFileDialog2.Title = "新建地图文档";
            SaveFileDialog2.Filter = "Mxd文档(*.mxd)|*.mxd";
            SaveFileDialog2.ShowDialog();
            String sFilePath = SaveFileDialog2.FileName;
            pMapDocument.New(sFilePath);
            pMapDocument.Open(sFilePath, ""); 
            axMapControl1.Map = pMapDocument.get_Map(0);
        }

        public static void OpenMapDoc(AxMapControl axMapControl1)
        {
            //System.Windows.Forms.OpenFileDialog openFileDialog;
            //openFileDialog2 = new OpenFileDialog();
            //openFileDialog2.Title = "打开地图文档";
            //openFileDialog2.Filter = "map documents(*.mxd)|*.mxd";
            //if (openFileDialog2.ShowDialog() != DialogResult.OK)
            //{
            //    MessageBox.Show("打开失败！");
            //    return;
            //}
            //string filePath = openFileDialog2.FileName;
            //if (axMapControl1.CheckMxFile(filePath))
            //{
            //    axMapControl1.MousePointer = esriControlsMousePointer.esriPointerHourglass;
            //    axMapControl1.LoadMxFile(filePath, 0, Type.Missing);
            //    axMapControl1.MousePointer = esriControlsMousePointer.esriPointerDefault;
            //}
            //else
            //{
            //    MessageBox.Show(filePath + "不是有效的地图文档");
            //}
        }

        public static void SaveMapDoc()
        {
            
             if (pMapDocument.get_IsReadOnly(pMapDocument.DocumentFilename) == true)
            {
                MessageBox.Show("地图文档只读!");
                return;
            }
            else
            {
                pMapDocument.Save(pMapDocument.UsesRelativePaths, true);
                MessageBox.Show("保存成功!");
                Application.Exit();
            }
        }
  
        public static void SaveAsMapDoc()
        {
            SaveFileDialog2 = new SaveFileDialog();
            SaveFileDialog2.Title = "Save Map As";
            SaveFileDialog2.Filter = "Mxd Document(*.mxd)|*.mxd";
            SaveFileDialog2.ShowDialog();
            String sFilePath = SaveFileDialog2.FileName;
            if (sFilePath == "")
            {
                return;
            }
            if (sFilePath == pMapDocument.DocumentFilename)
            {
                SaveMapDoc();
            }

            else
            {
                pMapDocument.SaveAs(sFilePath, true, true);
            }
            Application.Exit();
        }
    }
}
