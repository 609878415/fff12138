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

namespace MapEdit
{
    public class EditClass
    {
        ///// <summary>
        ///// 开始编辑
        ///// </summary>
        ///// <param name="bWithUndoRedo"></param>
        //public void StartEditing(bool bWithUndoRedo)
        //{
        //    if (m_SelectedLayer == null) return;
        //    IFeatureLayer featureLayer = m_SelectedLayer as IFeatureLayer;
        //    if (featureLayer == null) return;
        //    IFeatureClass featureClass = featureLayer.FeatureClass;
        //    if (featureClass == null) return;

        //    IDataset dataset = featureClass as IDataset;
        //    IWorkspaceEdit workspaceEdit = dataset.Workspace as IWorkspaceEdit;
        //    try
        //    {
        //        workspaceEdit.StartEditing(bWithUndoRedo);
        //        m_IsEdited = true;
        //    }
        //    catch
        //    {
        //        return;
        //    }
        //}
        ///// <summary>
        ///// 结束编辑
        ///// </summary>
        ///// <param name="bSave"></param>
        //public void StopEditing(bool bSave)
        //{
        //    if (m_IsEdited)
        //    {
        //        m_IsEdited = false;

        //        if (m_SelectedLayer == null) return;
        //        IFeatureLayer featureLayer = m_SelectedLayer as IFeatureLayer;
        //        if (featureLayer == null) return;
        //        IFeatureClass featureClass = featureLayer.FeatureClass;
        //        if (featureClass == null) return;

        //        IDataset dataset = featureClass as IDataset;
        //        IWorkspaceEdit workspaceEdit = dataset.Workspace as IWorkspaceEdit;
        //        if (workspaceEdit.IsBeingEdited())
        //        {
        //            try
        //            {
        //                workspaceEdit.StopEditing(bSave);
        //            }
        //            catch
        //            {
        //                workspaceEdit.AbortEditOperation();
        //                return;
        //            }
        //        }
        //    }
        //}
    }
}
