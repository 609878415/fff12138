using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;

using System.Runtime.InteropServices;
using ESRI.ArcGIS.ADF.BaseClasses;
using ESRI.ArcGIS.ADF.CATIDs;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.ArcMapUI;
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

namespace MapEdit.Nitool
{
    /// <summary>
    /// Summary description for CutFeature.
    /// </summary>
    [Guid("fb03679b-fc1e-41e2-b481-d8945d5eb54b")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("MapEdit.Nitool.CutFeature")]
    public sealed class CutFeature : BaseTool
    {
        #region COM Registration Function(s)
        [ComRegisterFunction()]
        [ComVisible(false)]
        static void RegisterFunction(Type registerType)
        {
            // Required for ArcGIS Component Category Registrar support
            ArcGISCategoryRegistration(registerType);

            //
            // TODO: Add any COM registration code here
            //
        }

        [ComUnregisterFunction()]
        [ComVisible(false)]
        static void UnregisterFunction(Type registerType)
        {
            // Required for ArcGIS Component Category Registrar support
            ArcGISCategoryUnregistration(registerType);

            //
            // TODO: Add any COM unregistration code here
            //
        }

        #region ArcGIS Component Category Registrar generated code
        /// <summary>
        /// Required method for ArcGIS Component Category registration -
        /// Do not modify the contents of this method with the code editor.
        /// </summary>
        private static void ArcGISCategoryRegistration(Type registerType)
        {
            string regKey = string.Format("HKEY_CLASSES_ROOT\\CLSID\\{{{0}}}", registerType.GUID);
            MxCommands.Register(regKey);

        }
        /// <summary>
        /// Required method for ArcGIS Component Category unregistration -
        /// Do not modify the contents of this method with the code editor.
        /// </summary>
        private static void ArcGISCategoryUnregistration(Type registerType)
        {
            string regKey = string.Format("HKEY_CLASSES_ROOT\\CLSID\\{{{0}}}", registerType.GUID);
            MxCommands.Unregister(regKey);

        }

        #endregion
        #endregion

        private IApplication m_application;
        private ILayer mLayer;
        private IMap mMap;
        private IPolyline mline;
        private IHookHelper mhookHelper=new HookHelperClass();
       

        public CutFeature(ILayer player,IMap pmap,IPolyline CutLine)
        {
            mMap = pmap;
            mLayer = player;
            mline = CutLine;
            //
            // TODO: Define values for the public properties
            //
            base.m_category = "Generic"; //localizable text 
            base.m_caption = "CutFeature";  //localizable text 
            base.m_message = "Cut polyline or polygon";  //localizable text
            base.m_toolTip = "CutFeature";  //localizable text
            base.m_name = "Generic_CutFeature";   //unique id, non-localizable (e.g. "MyCategory_ArcMapTool")
            try
            {
                //
                // TODO: change resource name if necessary
                //
                string bitmapResourceName = GetType().Name + ".bmp";
                base.m_bitmap = new Bitmap(GetType(), bitmapResourceName);
                base.m_cursor = new System.Windows.Forms.Cursor(GetType(), GetType().Name + ".cur");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message, "Invalid Bitmap");
            }
        }

        #region Overridden Class Methods



        /// <summary>
        /// Occurs when this tool is created
        /// </summary>
        /// <param name="hook">Instance of the application</param>
        public override void OnCreate(object hook)
        {
            m_application = hook as IApplication;

            mhookHelper.Hook = hook;
            //Disable if it is not ArcMap
            if (hook is IMxApplication)
                base.m_enabled = true;
            else
                base.m_enabled = false;

            // TODO:  Add other initialization code
        }

        /// <summary>
        /// Occurs when this tool is clicked
        /// </summary>
        public override void OnClick()
        {
            // TODO: Add CutFeature.OnClick implementation
            
        }

        public override void OnMouseDown(int Button, int Shift, int X, int Y)
        {
            // TODO:  Add CutFeature.OnMouseDown implementation
            IFeature SelectFeature = ClickSelectFeature(mMap, mLayer, X, Y);
            IPolyline selectPolyline = SelectFeature.Shape as IPolyline;

            IActiveView maprefr = (IActiveView)mMap;
            IFeatureLayer pFeatureLayer = (IFeatureLayer)mLayer;
            IDataset pDataset = (IDataset)pFeatureLayer.FeatureClass;
            IWorkspaceEdit pWorkspaceEdit = (IWorkspaceEdit)pDataset.Workspace;
            

            IFeatureClass pfeatureclass = pFeatureLayer.FeatureClass;

            pWorkspaceEdit.StartEditOperation();


            //IFeatureCursor pEF = pFeatureLayer.Search(null, false);
            //IFeature pFeature = pEF.NextFeature();
            IFeature pFeature = pfeatureclass.CreateFeature();
            IGeometry pGeometry = pFeature.Shape;


            //IFeatureClass fClass = pFeatureLayer.FeatureClass;
            //int fCount = fClass.FeatureCount(null);
            

            IGeometry left = new PolygonClass();
            IGeometry right = new PolygonClass();

            ITopologicalOperator5 pTopOperator = selectPolyline as ITopologicalOperator5;
            pTopOperator.IsKnownSimple_2 = false;
            pTopOperator.Simplify();
            try
            {

                pTopOperator.Cut(mline, out left, out right);


            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            IPoint fPoint = mline.FromPoint;
            IPoint tPoint = GetIntersectPoint(mline, selectPolyline);
            
            IPoint t_Point = mhookHelper.ActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(X, Y);
           
            //double a = (double)X;
            //double b = (double)Y;

            //t_Point.PutCoords(a, b);
            #region 判断左右,返回geometry
            double minus_x = t_Point.X - fPoint.X;
            double minus_y = t_Point.Y - fPoint.Y;

            double base_x = tPoint.X - fPoint.X;
            double base_y = tPoint.Y - fPoint.Y;

            double judege_y = minus_y - base_y;

            if (base_x < 0)
            {
                if (judege_y > 0) pGeometry = left;
                else pGeometry = right;
            }
            else if (base_x>0)
            {
                if (judege_y < 0) pGeometry = left;
                else pGeometry = right;
            }
            else return;
            #endregion


            SelectFeature.Delete();
            //pGeometry = left;
            pFeature.Shape = pGeometry;
            pFeature.Store();

            pWorkspaceEdit.StopEditOperation();
            maprefr.Refresh();
        }

        public override void OnMouseMove(int Button, int Shift, int X, int Y)
        {
            // TODO:  Add CutFeature.OnMouseMove implementation
        }

        public override void OnMouseUp(int Button, int Shift, int X, int Y)
        {
            // TODO:  Add CutFeature.OnMouseUp implementation
        }
        
        #endregion

        #region 点选要素
        private IFeature ClickSelectFeature(IMap m_pMap,ILayer m_Layer, int x, int y)
        {
            //IPolyline selectpolyline; selectpolyline = pFeature.Shape as IPolyline;
            // get the layer
            IFeatureLayer pFeatureLayer = m_Layer as IFeatureLayer;
            //if (pFeatureLayer == null) { return; } 
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;//get the feature
            //if (pFeatureClass == null) { return; }
            //get mouse position
            IActiveView pActiveView = m_pMap as IActiveView;
            IPoint pPoint = pActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(x, y);
            //Use a 4 pixel buffer around the cursor for feature search
            double length;
            length = ConvertPixelsToMapUnits(pActiveView, 4);
            ITopologicalOperator pTopo = pPoint as ITopologicalOperator;
            IGeometry pBuffer = pTopo.Buffer(length);//建立4个地图单位的缓冲区
            IGeometry pGeometry = pBuffer.Envelope;//确定鼠标周围隐藏的选择框

            //新建一个空间约束器
            ISpatialFilter pSpatialFilter;
            IQueryFilter pFilter;
            //设置查询约束条件
            pSpatialFilter = new SpatialFilter();
            pSpatialFilter.Geometry = pGeometry;

            //switch (pFeatureClass.ShapeType)
            //{
            //    case esriGeometryType.esriGeometryPoint:
            //        pSpatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelContains;
            //        break;
            //    case esriGeometryType.esriGeometryPolyline:
            //        pSpatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelCrosses;
            //        break;
            //    case esriGeometryType.esriGeometryPolygon:
            //        pSpatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
            //        break;
            //    default:
            //        break;
            //}
            pSpatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelCrosses;
            pSpatialFilter.GeometryField = pFeatureClass.ShapeFieldName;
            pFilter = pSpatialFilter;
            //Do the Search 从图层中查询出满足约束条件的元素
            IFeatureCursor pCursor = pFeatureLayer.Search(pFilter, false);

            //select
            IFeature pFeature = pCursor.NextFeature();
            //m_pMap.SelectFeature(pFeatureLayer, pFeature);
            //while (pFeature != null)
            //{
            //    //m_pMap.SelectFeature(pFeatureLayer, pFeature);
            //    selectpolyline = pFeature.Shape as IPolyline;
            //    pFeature = pCursor.NextFeature();
            //}
            
            return pFeature;
            //pActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);
        }
        
        private double ConvertPixelsToMapUnits(IActiveView pActiveView, double pixelUnits)
        {
            // Uses the ratio of the size of the map in pixels to map units to do the conversion
            IPoint p1 = pActiveView.ScreenDisplay.DisplayTransformation.VisibleBounds.UpperLeft;
            IPoint p2 = pActiveView.ScreenDisplay.DisplayTransformation.VisibleBounds.UpperRight;
            int x1, x2, y1, y2;
            pActiveView.ScreenDisplay.DisplayTransformation.FromMapPoint(p1, out x1, out y1);
            pActiveView.ScreenDisplay.DisplayTransformation.FromMapPoint(p2, out x2, out y2);
            double pixelExtent = x2 - x1;
            double realWorldDisplayExtent = pActiveView.ScreenDisplay.DisplayTransformation.VisibleBounds.Width;
            double sizeOfOnePixel = realWorldDisplayExtent / pixelExtent;
            return pixelUnits * sizeOfOnePixel;
        }
        #endregion
        #region 交点获取
        private IPoint GetIntersectPoint(IPolyline line1,IPolyline line2)
        {
            ITopologicalOperator5 topoOperator = line1 as ITopologicalOperator5;
            IGeometry geo = topoOperator.Intersect(line2, esriGeometryDimension.esriGeometry0Dimension);
            IPointCollection5 pc=geo as IPointCollection5;
            IPoint pt = pc.get_Point(0);
            return pt;
        }
        #endregion
    }
}
