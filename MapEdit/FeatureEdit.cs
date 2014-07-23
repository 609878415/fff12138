using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Display;
using System.IO;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Geodatabase;

namespace MapEdit
{
    class FeatureEdit
    {
        private AxMapControl MapCtr;
        public AxMapControl p_axMapControl
        {
            get { return MapCtr; }
        }
        public FeatureEdit(AxMapControl m_axMapControl)
        {
            MapCtr = m_axMapControl;
        }
        public FeatureEdit()
        {

        }
        public ILayer getFeatureLayerByName(IMap pMap, string str)
        {

            //ILayer aa = pMap.get_Layer(0);
            IFeatureLayer pFeatureLayer = null; //as IFeatureLayer;

            for (int i = 0; i < pMap.LayerCount; i++)
            {
                if (str == pMap.get_Layer(i).Name)
                {
                    pFeatureLayer = pMap.get_Layer(i) as IFeatureLayer;
                }
            }

            return pFeatureLayer;
        }
        
        public void AddFeatureByStore(IGeometry pGeometry, string strLyrName)
        {
            
            IFeatureLayer l = getFeatureLayerByName(MapCtr.Map, strLyrName) as IFeatureLayer;
            IFeatureClass fc = l.FeatureClass;
            IWorkspaceEdit w = (fc as IDataset).Workspace as IWorkspaceEdit;
            IFeature f;

            w.StartEditing(true);
            w.StartEditOperation();
            f = fc.CreateFeature();
            f.Shape = pGeometry;
            f.Store();
            w.StopEditOperation();
            w.StopEditing(true);
            MapCtr.Refresh();
        }
        public void AddFeatureByBuffer(IGeometry pGeometry, string strLyrName)
        {
            IFeatureLayer l = getFeatureLayerByName(MapCtr.Map, strLyrName) as IFeatureLayer;
            IFeatureClass fc = l.FeatureClass;
            IWorkspaceEdit w = (fc as IDataset).Workspace as IWorkspaceEdit;
            IFeatureBuffer f;

            w.StartEditing(true);
            w.StartEditOperation();
            IFeatureCursor cur = fc.Insert(true);
            f = fc.CreateFeatureBuffer();
            f.Shape = pGeometry;
            cur.InsertFeature(f);
            w.StopEditOperation();
            w.StopEditing(true);
            MapCtr.Refresh();
        }
        public void DelSelectedFeatures(IFeatureCursor p_FeatureCursor, string strLyrName)
        {
            IFeatureLayer pFeatLyr = getFeatureLayerByName(MapCtr.Map, strLyrName) as IFeatureLayer;
            IFeatureClass pFeatClass = pFeatLyr.FeatureClass;
            IWorkspaceEdit pWorkspaceEdit = (pFeatClass as IDataset).Workspace as IWorkspaceEdit;

            pWorkspaceEdit.StartEditing(true);
            pWorkspaceEdit.StartEditOperation();
            IFeature pFeat = p_FeatureCursor.NextFeature();
            while (pFeat != null)
            {
                pFeat.Delete();
                pFeat = p_FeatureCursor.NextFeature();
            }
            p_FeatureCursor.Flush();
            pWorkspaceEdit.StopEditOperation();
            pWorkspaceEdit.StopEditing(true);
            MapCtr.Refresh();

        }
       
    }
}

