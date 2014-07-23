using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.DataSourcesGDB;

namespace arcgisapp
{
    /// <summary>
    /// 使用本类可以新建点、线、面
    /// 移动点、线、面
    /// 编辑线、面的节点
    /// 使用时需设置Map和CurrentLayer
    /// </summary>
    public class AoEditor
    {
        private ILayer m_pCurrentLayer;                   //当前图层
        private IMap m_pMap;                             //当前操作的地图
        private IFeature m_pEditFeature;                 //编辑的元素
        private IPoint m_pPoint;                          //点
        private IDisplayFeedback m_pFeedback;             //撤销对象
        //private ISelectionTracker m_pSelectionTracker;
        private bool m_bInUse;                           //是否正在使用
        private IPointCollection m_pPointCollection;     //点集合
        public static IEngineSketchOperation sketchOp = new EngineSketchOperationClass();
        private IEngineSketchOperation m_sketchOp;      //节点编辑对象
        /// <summary>
        /// 当前图层,只写
        /// </summary>
        public ILayer CurrentLayer
        {
            set
            {
                m_pCurrentLayer = (ILayer)value;
            }
        }
        /// <summary>
        /// 地图对象,只写
        /// </summary>
        public IMap Map
        {
            set
            {
                m_pMap = (IMap)value;
            }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        public AoEditor()
        {

        }
        /// <summary>
        /// 开始编辑,使工作空间处于可编辑状态
        /// 在进行图层编辑前必须调用本方法
        /// </summary>
        public void StartEditing()
        {
            try
            {
                //当前选中图层为空时直接返回
                if (m_pCurrentLayer == null) return;
                //当前图层不是可编辑的直接返回
                if (!(m_pCurrentLayer is IGeoFeatureLayer)) return;

                IFeatureLayer pFeatureLayer = (IFeatureLayer)m_pCurrentLayer;
                IDataset pDataset = (IDataset)pFeatureLayer.FeatureClass;
                if (pDataset == null) return;

                // 开始编辑,并设置Undo/Redo 为可用
                IWorkspaceEdit pWorkspaceEdit = (IWorkspaceEdit)pDataset.Workspace;
                if (!pWorkspaceEdit.IsBeingEdited())
                {
                    pWorkspaceEdit.StartEditing(true);
                    pWorkspaceEdit.EnableUndoRedo();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
            }
        }

        /// <summary>
        /// 停止编辑，并将之前的编辑结果保存到数据文件中。
        /// </summary>
        public void StopEditing()
        {
            bool bHasEdits = false;
            bool bSave = false;

            try
            {
                if (m_pCurrentLayer == null) return;
                IFeatureLayer pFeatureLayer = (IFeatureLayer)m_pCurrentLayer;
                if (pFeatureLayer.FeatureClass == null) return;
                IDataset pDataset = (IDataset)pFeatureLayer.FeatureClass;
                if (pDataset == null) return;

                //如果数据已被修改，则提示用户是否保存
                IWorkspaceEdit pWorkspaceEdit = (IWorkspaceEdit)pDataset.Workspace;
                if (pWorkspaceEdit.IsBeingEdited())
                {
                    pWorkspaceEdit.HasEdits(ref bHasEdits);
                    if (bHasEdits)
                    {
                        DialogResult result;
                        result = MessageBox.Show("是否保存已做的修改?", "提示", MessageBoxButtons.YesNo);
                        if (result == DialogResult.Yes)
                        {
                            bSave = true;
                        }
                    }
                    pWorkspaceEdit.StopEditing(bSave);
                }

                m_pMap.ClearSelection();
                IActiveView pActiveView = (IActiveView)m_pMap;
                pActiveView.Refresh();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
            }
        }
        /// <summary>
        /// 检查工作空间中是否有数据处于编辑状态
        /// </summary>
        /// <returns>是否正在编辑</returns>
        public bool InEdit()
        {
            try
            {
                if (m_pCurrentLayer == null) return false;
                IFeatureLayer pFeatureLayer = (IFeatureLayer)m_pCurrentLayer;
                if (pFeatureLayer.FeatureClass == null) return false;
                IDataset pDataset = (IDataset)pFeatureLayer.FeatureClass;
                if (pDataset == null) return false;
                IWorkspaceEdit pWorkspaceEdit = (IWorkspaceEdit)pDataset.Workspace;
                if (pWorkspaceEdit.IsBeingEdited()) return true;

                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
                return false;
            }
        }
        /// <summary>
        /// 新建对象方法
        /// 当前图层为点图层时，每调用一次就新点一个点对象
        /// 当前图层为线图层或面图层时，第一次调用开始新建对象，并添加当前点，
        /// 以后每调用一次，即向新对象中添加一个点,调用NewFeatureEnd方法完成对象创建
        /// 在Map.MouseDown事件中调用本方法
        /// </summary>
        /// <param name="x">鼠标X坐标，屏幕坐标</param>
        /// <param name="y">鼠标Y坐标，屏幕坐标</param>
        public void NewFeatureMouseDown(int x, int y)
        {
            INewPolygonFeedback pPolyFeed;
            INewLineFeedback pLineFeed;

            try
            {
                if (m_pCurrentLayer == null) return;
                if (!(m_pCurrentLayer is IGeoFeatureLayer)) return;
                IFeatureLayer pFeatureLayer = (IFeatureLayer)m_pCurrentLayer;
                if (pFeatureLayer.FeatureClass == null) return;
                IActiveView pActiveView = (IActiveView)m_pMap;
                IPoint pPoint = pActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(x, y);

                // 如果是新开始创建的对象，则相应的创建一个新的Feedback对象；
                // 否则，向已存在的Feedback对象中加点
                if (!m_bInUse)
                {
                    m_pMap.ClearSelection();  //清除地图选中对象
                    switch (pFeatureLayer.FeatureClass.ShapeType)
                    {
                        case esriGeometryType.esriGeometryPoint:
                            CreateFeature(pPoint);
                            break;
                        case esriGeometryType.esriGeometryMultipoint:
                            m_bInUse = true;
                            m_pFeedback = new NewMultiPointFeedbackClass();
                            INewMultiPointFeedback pMPFeed = (INewMultiPointFeedback)m_pFeedback;
                            m_pPointCollection = new MultipointClass();
                            pMPFeed.Start(m_pPointCollection, pPoint);
                            break;
                        case esriGeometryType.esriGeometryPolyline:
                            m_bInUse = true;
                            m_pFeedback = new NewLineFeedbackClass();
                            pLineFeed = (INewLineFeedback)m_pFeedback;
                            pLineFeed.Start(pPoint);
                            break;
                        case esriGeometryType.esriGeometryPolygon:
                            m_bInUse = true;
                            m_pFeedback = new NewPolygonFeedbackClass();
                            pPolyFeed = (INewPolygonFeedback)m_pFeedback;
                            pPolyFeed.Start(pPoint);
                            break;
                    }

                    if (m_pFeedback != null)
                        m_pFeedback.Display = pActiveView.ScreenDisplay;
                }
                else
                {
                    if (m_pFeedback is INewMultiPointFeedback)
                    {
                        object obj = Missing.Value;
                        m_pPointCollection.AddPoint(pPoint, ref obj, ref obj);
                    }
                    else if (m_pFeedback is INewLineFeedback)
                    {
                        pLineFeed = (INewLineFeedback)m_pFeedback;
                        pLineFeed.AddPoint(pPoint);
                    }
                    else if (m_pFeedback is INewPolygonFeedback)
                    {
                        pPolyFeed = (INewPolygonFeedback)m_pFeedback;
                        pPolyFeed.AddPoint(pPoint);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
            }
        }
        /// <summary>
        /// 新建对象过程中鼠标移动方法,产生Track效果
        /// 在Map.MouseMove事件中调用本方法
        /// </summary>
        /// <param name="x">鼠标X坐标，屏幕坐标</param>
        /// <param name="y">鼠标Y坐标，屏幕坐标</param>
        public void NewFeatureMouseMove(int x, int y)
        {
            if ((!m_bInUse) || (m_pFeedback == null)) return;

            IActiveView pActiveView = (IActiveView)m_pMap;
            //获取屏幕的鼠标位置
            m_pPoint = pActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(x, y);
            m_pFeedback.MoveTo(m_pPoint);
        }
        /// <summary>
        /// 完成新建对象，取得绘制的对象，并添加到图层中
        /// 建议在Map.DblClick或Map.MouseDown(Button = 2)事件中调用本方法
        /// </summary>
        public void NewFeatureEnd()
        {
            IGeometry pGeom = null;
            IPointCollection pPointCollection;

            try
            {
                if (m_pFeedback is INewMultiPointFeedback)
                {
                    INewMultiPointFeedback pMPFeed = (INewMultiPointFeedback)m_pFeedback;
                    pMPFeed.Stop();
                    pGeom = (IGeometry)m_pPointCollection;
                }
                else if (m_pFeedback is INewLineFeedback)
                {
                    INewLineFeedback pLineFeed = (INewLineFeedback)m_pFeedback;
                    pLineFeed.AddPoint(m_pPoint);
                    IPolyline pPolyLine = pLineFeed.Stop();

                    pPointCollection = (IPointCollection)pPolyLine;
                    if (pPointCollection.PointCount < 2)
                        MessageBox.Show("至少输入两个节点");
                    else
                        pGeom = (IGeometry)pPointCollection;
                }
                else if (m_pFeedback is INewPolygonFeedback)
                {
                    INewPolygonFeedback pPolyFeed = (INewPolygonFeedback)m_pFeedback;
                    pPolyFeed.AddPoint(m_pPoint);

                    IPolygon pPolygon;
                    pPolygon = pPolyFeed.Stop();
                    if (pPolygon != null)
                    {
                        pPointCollection = (IPointCollection)pPolygon;
                        if (pPointCollection.PointCount < 3)
                            MessageBox.Show("至少输入三个节点");
                        else
                            pGeom = (IGeometry)pPointCollection;
                    }
                }

                CreateFeature(pGeom);
                m_pFeedback = null;
                m_bInUse = false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
            }
        }
        /// <summary>
        /// 查询当前图层中鼠标位置处的地图对象
        /// 建议在Map.MouseDown事件中调用本方法
        /// </summary>
        /// <param name="x">鼠标X坐标，屏幕坐标</param>
        /// <param name="y">鼠标Y坐标，屏幕坐标</param>
        public void SelectMouseDown(int x, int y)
        {
            ISpatialFilter pSpatialFilter;
            IQueryFilter pFilter;

            try
            {
                if (m_pCurrentLayer == null) return;
                if (!(m_pCurrentLayer is IGeoFeatureLayer)) return;

                IFeatureLayer pFeatureLayer = (IFeatureLayer)m_pCurrentLayer;
                IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
                if (pFeatureClass == null) return;
                IActiveView pActiveView = (IActiveView)m_pMap;
                IPoint pPoint = pActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(x, y);
                IGeometry pGeometry = pPoint;

                // 设置查询缓冲区
                double length = ConvertPixelsToMapUnits(pActiveView, 4.0);
                ITopologicalOperator pTopo = (ITopologicalOperator)pGeometry;
                IGeometry pBuffer = pTopo.Buffer(length);
                pGeometry = pBuffer.Envelope;

                //设置过滤器对象
                pSpatialFilter = new SpatialFilterClass();
                pSpatialFilter.Geometry = pGeometry;
                //---------------//删除节点有点问题   -------------
                switch (pFeatureClass.ShapeType)
                {

                    case esriGeometryType.esriGeometryPoint:
                        pSpatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelContains;
                        break;
                    case esriGeometryType.esriGeometryPolyline:
                        pSpatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelCrosses;
                        break;
                    case esriGeometryType.esriGeometryPolygon:
                        pSpatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                        break;
                }
                pSpatialFilter.GeometryField = pFeatureClass.ShapeFieldName;
                pFilter = pSpatialFilter;

                // 查询
                IFeatureCursor pCursor = pFeatureLayer.Search(pFilter, false);
                // 在地图上高亮显示查询结果
                IFeature pFeature = pCursor.NextFeature();
                while (pFeature != null)
                {
                    m_pMap.SelectFeature(m_pCurrentLayer, pFeature);
                    pFeature = pCursor.NextFeature();
                }
                pActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
            }
        }

        /// <summary>
        /// 编辑当前图层中鼠标击中的地图对象(开始编辑),
        /// 如果为点对象，可进行位置移动，如果为线对象或面对象，可进行节点编辑
        /// 建议在Map.MouseDown事件中调用本方法
        /// </summary>
        /// <param name="x">鼠标X坐标，屏幕坐标</param>
        /// <param name="y">鼠标Y坐标，屏幕坐标</param>
        /// <returns></returns>
        public bool EditFeatureMouseDown(int x, int y)
        {

            IGeometryCollection pGeomColn;
            IPointCollection pPointColn;
            IObjectClass pObjectClass;
            IFeature pFeature;
            IGeometry pGeom;

            IPath pPath;
            IPoint pHitPoint = null;
            IPoint pPoint = null;
            Double hitDist = 0.0;
            double tol;
            int vertexIndex = 0;
            int numVertices;
            int partIndex = 0;
            bool vertex = false;
            try
            {

                m_pMap.ClearSelection();
                // 取得鼠标击中的第一个对象
                SelectMouseDown(x, y);
                IEnumFeature pSelected = (IEnumFeature)m_pMap.FeatureSelection;
                pFeature = pSelected.Next();
                if (pFeature == null) return false;

                IActiveView pActiveView = (IActiveView)m_pMap;
                pPoint = pActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(x, y);

                // 节点空间查询容差
                tol = ConvertPixelsToMapUnits(pActiveView, 4.0);

                pGeom = pFeature.Shape;
                pObjectClass = pFeature.Class;
                m_pEditFeature = pFeature;
                object objNull = Missing.Value;
                object objBefore, objAfter;
                switch (pGeom.GeometryType)
                {
                    //为点的时候
                    case esriGeometryType.esriGeometryPoint:
                        m_pFeedback = new MovePointFeedbackClass();
                        m_pFeedback.Display = pActiveView.ScreenDisplay;
                        IMovePointFeedback pPointMove = (IMovePointFeedback)m_pFeedback;
                        pPointMove.Start((IPoint)pGeom, pPoint);
                        break;
                    case esriGeometryType.esriGeometryPolyline:
                        if (TestGeometryHit(tol, pPoint, pFeature, pHitPoint, hitDist, out partIndex, out vertexIndex, out vertex))
                        {
                            if (!vertex)
                            {
                                pGeomColn = (IGeometryCollection)pGeom;
                                pPath = (IPath)pGeomColn.get_Geometry(partIndex);
                                pPointColn = (IPointCollection)pPath;
                                numVertices = pPointColn.PointCount;

                                if (vertexIndex == 0)
                                {
                                    objBefore = (object)(vertexIndex + 1);
                                    pPointColn.AddPoint(pPoint, ref objBefore, ref objNull);
                                    //pPointColn.RemovePoints(vertexIndex, 1);
                                }
                                else
                                {
                                    objAfter = (object)vertexIndex;
                                    pPointColn.AddPoint(pPoint, ref objNull, ref objAfter);
                                }
                                TestGeometryHit(tol, pPoint, pFeature, pHitPoint, hitDist, out partIndex, out vertexIndex, out vertex);

                            }
                            m_pFeedback = new LineMovePointFeedbackClass();
                            m_pFeedback.Display = pActiveView.ScreenDisplay;
                            ILineMovePointFeedback pLineMove = (ILineMovePointFeedback)m_pFeedback;
                            pLineMove.Start((IPolyline)pGeom, vertexIndex, pPoint);
                            //---------------------------------------------------删除节点的方法--------------------------------------------------------------------
                            //deleteFeature(vertexIndex);

                            //       m_pSelectionTracker = new LineTrackerClass();
                            //       m_pSelectionTracker.Display = pActiveView.ScreenDisplay ;
                            //       m_pSelectionTracker.Geometry = pGeom;
                            //       m_pSelectionTracker.ShowHandles = true;
                            //       m_pSelectionTracker.QueryResizeFeedback(ref m_pFeedback); 
                            //       m_pSelectionTracker.OnMouseDown(1,0,x,y);        
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    case esriGeometryType.esriGeometryPolygon:
                        if (TestGeometryHit(tol, pPoint, pFeature, pHitPoint, hitDist, out partIndex, out vertexIndex, out vertex))
                        {
                            if (!vertex)
                            {
                                pGeomColn = (IGeometryCollection)pGeom;
                                pPath = (IPath)pGeomColn.get_Geometry(partIndex);
                                pPointColn = (IPointCollection)pPath;
                                numVertices = pPointColn.PointCount;
                                if (vertexIndex == 0)
                                {
                                    objBefore = (object)(vertexIndex + 1);
                                    pPointColn.AddPoint(pPoint, ref objBefore, ref objNull);
                                }
                                else
                                {
                                    objAfter = (object)vertexIndex;
                                    pPointColn.AddPoint(pPoint, ref objNull, ref objAfter);
                                }
                                TestGeometryHit(tol, pPoint, pFeature, pHitPoint, hitDist, out partIndex, out vertexIndex, out vertex);
                            }
                            m_pFeedback = new PolygonMovePointFeedbackClass();
                            m_pFeedback.Display = pActiveView.ScreenDisplay;
                            IPolygonMovePointFeedback pPolyMove = (IPolygonMovePointFeedback)m_pFeedback;
                            pPolyMove.Start((IPolygon)pGeom, vertexIndex, pPoint);
                        }
                        else
                        {
                            return false;
                        }
                        break;
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
                return false;
            }
        }
        /// <summary>
        /// 编辑地图对象过程中的鼠标移动事件,
        /// 如果为点对象，进行位置移动
        /// 如果为线对象或面对象，进行节点移动
        /// 建议在Map.MouseMove事件中调用本方法
        /// </summary>
        /// <param name="x">鼠标X坐标，屏幕坐标</param>
        /// <param name="y">鼠标Y坐标，屏幕坐标</param>
        public void EditFeatureMouseMove(int x, int y)
        {
            try
            {
                if (m_pFeedback == null) return;

                IActiveView pActiveView = (IActiveView)m_pMap;
                IPoint pPoint = pActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(x, y);
                m_pFeedback.MoveTo(pPoint);

                //    if (m_pSelectionTracker !=null) m_pSelectionTracker.OnMouseMove(1,0,x,y); 
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
            }
        }
        /// <summary>
        /// 完成地图对象编辑，取得编辑后的对象，并将其更新到图层中
        /// 建议在Map.MouseUp事件中调用本方法
        /// </summary>
        public void EditFeatureEnd()
        {
            IGeometry pGeometry;

            try
            {
                if (m_pFeedback == null) return;

                if (m_pFeedback is IMovePointFeedback)
                {
                    IMovePointFeedback pPointMove = (IMovePointFeedback)m_pFeedback;
                    pGeometry = pPointMove.Stop();
                    UpdateFeature(m_pEditFeature, pGeometry);
                }
                else if (m_pFeedback is ILineMovePointFeedback)
                {
                    ILineMovePointFeedback pLineMove = (ILineMovePointFeedback)m_pFeedback;
                    pGeometry = pLineMove.Stop();
                    UpdateFeature(m_pEditFeature, pGeometry);
                }
                else if (m_pFeedback is IPolygonMovePointFeedback)
                {
                    IPolygonMovePointFeedback pPolyMove = (IPolygonMovePointFeedback)m_pFeedback;
                    pGeometry = pPolyMove.Stop();
                    UpdateFeature(m_pEditFeature, pGeometry);
                }

                m_pFeedback = null;
                //    m_pSelectionTracker = null;
                IActiveView pActiveView = (IActiveView)m_pMap;
                pActiveView.Refresh();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
            }
        }
        /// <summary>
        /// 移动当前图层中鼠标击中地图对象的位置（开始移动）
        /// 建议在Map.MouseDown事件中调用本方法
        /// </summary>
        /// <param name="x">鼠标X坐标，屏幕坐标</param>
        /// <param name="y">鼠标Y坐标，屏幕坐标</param>
        /// <returns></returns>
        public bool MoveFeatureMouseDown(int x, int y)
        {
            try
            {
                m_pMap.ClearSelection();

                SelectMouseDown(x, y);
                IEnumFeature pSelected = (IEnumFeature)m_pMap.FeatureSelection;
                IFeature pFeature = pSelected.Next();
                if (pFeature == null) return false;

                IActiveView pActiveView = (IActiveView)m_pMap;
                IPoint pPoint = pActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(x, y);

                IGeometry pGeom = pFeature.Shape;
                m_pEditFeature = pFeature;

                switch (pGeom.GeometryType)
                {
                    case esriGeometryType.esriGeometryPoint:
                        m_pFeedback = new MovePointFeedbackClass();
                        m_pFeedback.Display = pActiveView.ScreenDisplay;
                        IMovePointFeedback pPointMove = (IMovePointFeedback)m_pFeedback;
                        pPointMove.Start((IPoint)pGeom, pPoint);
                        break;
                    case esriGeometryType.esriGeometryPolyline:

                        m_pFeedback = new MoveLineFeedbackClass();
                        m_pFeedback.Display = pActiveView.ScreenDisplay;
                        IMoveLineFeedback pLineMove = (IMoveLineFeedback)m_pFeedback;
                        pLineMove.Start((IPolyline)pGeom, pPoint);
                        break;
                    case esriGeometryType.esriGeometryPolygon:
                        m_pFeedback = new MovePolygonFeedbackClass();
                        m_pFeedback.Display = pActiveView.ScreenDisplay;
                        IMovePolygonFeedback pPolyMove = (IMovePolygonFeedback)m_pFeedback;
                        pPolyMove.Start((IPolygon)pGeom, pPoint);
                        break;
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
                return false;
            }
        }
        /// <summary>
        /// 移动地图对象过程中的鼠标移动事件
        /// 建议在Map.MouseMove事件中调用本方法
        /// </summary>
        /// <param name="x">鼠标X坐标，屏幕坐标</param>
        /// <param name="y">鼠标Y坐标，屏幕坐标</param>
        public void MoveFeatureMouseMove(int x, int y)
        {
            try
            {
                if (m_pFeedback == null) return;

                IActiveView pActiveView = (IActiveView)m_pMap;
                IPoint pPoint = pActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(x, y);
                m_pFeedback.MoveTo(pPoint);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
            }
        }
        /// <summary>
        /// 完成地图对象移动，取得移动后的对象，并将其更新到图层中
        /// 建议在Map.MouseUp事件中调用本方法
        /// </summary>
        public void MoveFeatureEnd()
        {
            IGeometry pGeometry;

            try
            {
                if (m_pFeedback == null) return;

                if (m_pFeedback is IMovePointFeedback)
                {
                    IMovePointFeedback pPointMove = (IMovePointFeedback)m_pFeedback;
                    pGeometry = pPointMove.Stop();
                    UpdateFeature(m_pEditFeature, pGeometry);
                }
                else if (m_pFeedback is IMoveLineFeedback)
                {
                    IMoveLineFeedback pLineMove = (IMoveLineFeedback)m_pFeedback;
                    pGeometry = pLineMove.Stop();
                    UpdateFeature(m_pEditFeature, pGeometry);
                }
                else if (m_pFeedback is IMovePolygonFeedback)
                {
                    IMovePolygonFeedback pPolyMove = (IMovePolygonFeedback)m_pFeedback;
                    pGeometry = pPolyMove.Stop();
                    UpdateFeature(m_pEditFeature, pGeometry);
                }

                m_pFeedback = null;
                IActiveView pActiveView = (IActiveView)m_pMap;
                pActiveView.Refresh();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
            }
        }
        /// <summary>
        /// 删除当前图层中选中的地图对象
        /// </summary>
        public void DeleteSelectedFeature()
        {
            try
            {
                if (m_pCurrentLayer == null) return;
                IFeatureCursor pFeatureCursor = GetSelectedFeatures();
                if (pFeatureCursor == null) return;
                m_pMap.ClearSelection();
                IWorkspaceEdit pWorkspaceEdit = GetWorkspaceEdit();
                pWorkspaceEdit.StartEditOperation();
                IFeature pFeature = pFeatureCursor.NextFeature();
                while (pFeature != null)
                {
                    pFeature.Delete();
                    pFeature = pFeatureCursor.NextFeature();
                }
                pWorkspaceEdit.StopEditOperation();

                IActiveView pActiveView = (IActiveView)m_pMap;
                pActiveView.Refresh();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
            }
        }
        /// <summary>
        /// 撤消以前所做的编辑
        /// </summary>
        public void UndoEdit()
        {
            bool bHasUndos = false;

            try
            {
                if (m_pCurrentLayer == null) return;
                IFeatureLayer pFeatureLayer = (IFeatureLayer)m_pCurrentLayer;
                IDataset pDataset = (IDataset)pFeatureLayer.FeatureClass;
                if (pDataset == null) return;
                IWorkspaceEdit pWorkspaceEdit = (IWorkspaceEdit)pDataset.Workspace;
                pWorkspaceEdit.HasUndos(ref bHasUndos);
                if (bHasUndos) pWorkspaceEdit.UndoEditOperation();
                IActiveView pActiveView = (IActiveView)m_pMap;
                pActiveView.Refresh();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
            }
        }
        /// <summary>
        /// 重做已撤消的编辑
        /// </summary>
        public void RedoEdit()
        {
            bool bHasRedos = false;

            try
            {
                if (m_pCurrentLayer == null) return;
                IFeatureLayer pFeatureLayer = (IFeatureLayer)m_pCurrentLayer;
                IDataset pDataset = (IDataset)pFeatureLayer.FeatureClass;
                if (pDataset == null) return;
                IWorkspaceEdit pWorkspaceEdit = (IWorkspaceEdit)pDataset.Workspace;
                pWorkspaceEdit.HasRedos(ref bHasRedos);
                if (bHasRedos) pWorkspaceEdit.RedoEditOperation();
                IActiveView pActiveView = (IActiveView)m_pMap;
                pActiveView.Refresh();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
            }
        }
        /// <summary>
        /// 向图层中添加新的地图对象，并使之处于选中状态
        /// </summary>
        /// <param name="pGeom">图形对象</param>
        private void CreateFeature(IGeometry pGeom)
        {
            try
            {
                if (pGeom == null) return;
                if (m_pCurrentLayer == null) return;
                IWorkspaceEdit pWorkspaceEdit = GetWorkspaceEdit();
                IFeatureLayer pFeatureLayer = (IFeatureLayer)m_pCurrentLayer;
                IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
                pWorkspaceEdit.StartEditOperation();
                IFeature pFeature = pFeatureClass.CreateFeature();
                pFeature.Shape = pGeom;
                pFeature.Store();
                pWorkspaceEdit.StopEditOperation();

                m_pMap.SelectFeature(m_pCurrentLayer, pFeature);
                IActiveView pActiveView = (IActiveView)m_pMap;
                pActiveView.Refresh();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
            }
        }
        /// <summary>
        /// 瘵屏幕坐标转换为地图坐标
        /// </summary>
        /// <param name="pActiveView">地图</param>
        /// <param name="pixelUnits">屏幕坐标</param>
        /// <returns>地图坐标</returns>
        private double ConvertPixelsToMapUnits(IActiveView pActiveView, double pixelUnits)
        {
            tagRECT pRect = pActiveView.ScreenDisplay.DisplayTransformation.get_DeviceFrame();
            int pixelExtent = pRect.right - pRect.left;
            double realWorldDisplayExtent = pActiveView.ScreenDisplay.DisplayTransformation.VisibleBounds.Width;
            double sizeOfOnePixel = realWorldDisplayExtent / pixelExtent;
            return pixelUnits * sizeOfOnePixel;
        }
        /// <summary>
        /// 取得当前图层所在的工作空间
        /// </summary>
        /// <returns>工作空间</returns>
        private IWorkspaceEdit GetWorkspaceEdit()
        {
            if (m_pCurrentLayer == null) return null;

            IFeatureLayer pFeatureLayer = (IFeatureLayer)m_pCurrentLayer;
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            IDataset pDataset = (IDataset)pFeatureClass;
            if (pDataset == null)
                return null;
            else
                return (IWorkspaceEdit)pDataset.Workspace;
        }
        /// <summary>
        /// 取得选中的地图对象集合
        /// </summary>
        /// <returns>地图对象游标</returns>
        private IFeatureCursor GetSelectedFeatures()
        {
            if (m_pCurrentLayer == null) return null;
            IFeatureSelection pFeatSel = (IFeatureSelection)m_pCurrentLayer;
            ISelectionSet pSelectionSet = pFeatSel.SelectionSet;

            if (pSelectionSet.Count == 0)
            {
                return null;
            }
            ICursor pCursor;
            pSelectionSet.Search(null, false, out pCursor);
            return (IFeatureCursor)pCursor;
        }
        /// <summary>
        /// 测试是否击中地图对象或地图对象上的节点
        /// </summary>
        /// <param name="tolerance">查询容差</param>
        /// <param name="pPoint">点击位置</param>
        /// <param name="pFeature">测试对象</param>
        /// <param name="pHitPoint">查询目标点</param>
        /// <param name="hitDist">目标点与点击点距离</param>
        /// <param name="partIndex">节索引</param>
        /// <param name="vertexIndex">点索引</param>
        /// <param name="vertexHit">是否击中点</param>
        /// <returns>是否击中测试对象</returns>
        private bool TestGeometryHit(double tolerance, IPoint pPoint, IFeature pFeature, IPoint pHitPoint,
         double hitDist, out int partIndex, out int vertexIndex, out bool vertexHit)
        {
            try
            {
                IGeometry pGeom = pFeature.Shape;
                IHitTest pHitTest = (IHitTest)pGeom;
                pHitPoint = new PointClass();
                bool bRes = true;
                partIndex = 0;
                vertexIndex = 0;
                vertexHit = false;
                // 检查节点是否被击中
                if (pHitTest.HitTest(pPoint, tolerance, esriGeometryHitPartType.esriGeometryPartVertex, pHitPoint,
                 ref hitDist, ref partIndex, ref vertexIndex, ref bRes))
                {
                    vertexHit = true;
                    return true;
                }
                // 检边界是否被击中
                else
                {
                    if (pHitTest.HitTest(pPoint, tolerance, esriGeometryHitPartType.esriGeometryPartBoundary, pHitPoint,
                     ref hitDist, ref partIndex, ref vertexIndex, ref bRes))
                    {
                        vertexHit = false;
                        return true;
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
                partIndex = 0;
                vertexIndex = 0;
                vertexHit = false;
                return false;
            }
        }
        /// <summary>
        /// 向图层中更新新的地图对象，并使之处于选中状态
        /// </summary>
        /// <param name="pFeature"></param>
        /// <param name="pGeometry"></param>
        private void UpdateFeature(IFeature pFeature, IGeometry pGeometry)
        {
            try
            {
                IDataset pDataset = (IDataset)pFeature.Class;
                IWorkspaceEdit pWorkspaceEdit = (IWorkspaceEdit)pDataset.Workspace;
                if (!pWorkspaceEdit.IsBeingEdited())
                {
                    MessageBox.Show("当前图层不可编辑");
                    return;
                }
                pWorkspaceEdit.StartEditOperation();
                pFeature.Shape = pGeometry;
                pFeature.Store();
                pWorkspaceEdit.StopEditOperation();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message.ToString());
            }
        }
        /// <summary>
        /// 删除节点
        /// </summary>
        /// <param name="m_Feature"></param>
        public bool deleteFeature(int x, int y)//IFeatureLayer m_FeatureLayer
        {
            //IGeometryCollection pGeomColn ;
            //IPointCollection pPointColn;
            IObjectClass pObjectClass;
            IFeature pFeature;
            IGeometry pGeom;

            //IPath pPath ;
            IPoint pHitPoint = null;
            IPoint pPoint = null;
            Double hitDist = 0.0;
            double tol;
            int vertexIndex = 0;
            //int numVertices;
            int partIndex = 0;
            bool vertex = false;
            try
            {
                m_pMap.ClearSelection();
                // 取得鼠标击中的第一个对象
                SelectMouseDown(x, y);
                IEnumFeature pSelected = (IEnumFeature)m_pMap.FeatureSelection;
                pFeature = pSelected.Next();
                if (pFeature == null) return false;
                IActiveView pActiveView = (IActiveView)m_pMap;
                pPoint = pActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(x, y);
                // 节点空间查询容差
                tol = ConvertPixelsToMapUnits(pActiveView, 4.0);
                pGeom = pFeature.Shape;
                pObjectClass = pFeature.Class;
                m_pEditFeature = pFeature;
                object objNull = Missing.Value;
                //object objBefore, objAfter;


                IPointCollection pointCollection;
                if (m_pEditFeature.Shape.GeometryType == esriGeometryType.esriGeometryPolyline & TestGeometryHit(tol, pPoint, pFeature, pHitPoint, hitDist, out partIndex, out vertexIndex, out vertex))
                {
                    pointCollection = new PolylineClass();
                    IPolyline polyline = m_pEditFeature.Shape as IPolyline;
                    pointCollection = polyline as IPointCollection;
                    //如果点个数少于两个就无法构成线
                    if (pointCollection.PointCount > 2)
                    {
                        //移除指定的节点
                        pointCollection.RemovePoints(vertexIndex, 1);
                    }
                    else
                    {
                        MessageBox.Show("此先已经少于两个点，不能在删除");
                    }
                }
                else if (m_pEditFeature.Shape.GeometryType == esriGeometryType.esriGeometryPolygon & TestGeometryHit(tol, pPoint, pFeature, pHitPoint, hitDist, out partIndex, out vertexIndex, out vertex))
                {
                    pointCollection = new PolygonClass();
                    IPolygon polygon = m_pEditFeature.Shape as IPolygon;
                    pointCollection = polygon as IPointCollection;
                    //点数少于三个就不能能构成面
                    if (pointCollection.PointCount > 3)
                    {
                        //移除指点的节点
                        pointCollection.RemovePoints(vertexIndex, 1);
                    }
                    else
                    {
                        MessageBox.Show("此先已经少于三个点，不能在删除");
                    }
                }
                IWorkspaceEdit workspaceEdit;
                IWorkspace workspace;
                IFeatureLayer pFeatureLayer = (IFeatureLayer)m_pCurrentLayer;
                IDataset dataset = pFeatureLayer.FeatureClass as IDataset;
                workspace = dataset.Workspace;
                workspaceEdit = workspace as IWorkspaceEdit;
                //开始编辑
                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();
                //保存数据
                m_pEditFeature.Store();
                //结束编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
                pActiveView = (IActiveView)m_pMap;
                pActiveView.Refresh();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
                return false;
            }
        }
        public void deleteVertx()
        {
            //m_sketchOp = new EngineSketchOperationClass();
            //m_sketchOp.Start(new EngineEditorClass());
            //esriEngineSketchOperationType opType = esriEngineSketchOperationType.esriEngineSketchOperationGeneral;
            //m_sketchOp.SetMenuString("InsertVertex");
            ////opType = esriEngineSketchOperationType.esriEngineSketchOperationVertexAdded;
            //opType = esriEngineSketchOperationType.esriEngineSketchOperationVertexDeleted;
            ////opType = esriEngineSketchOperationType.esriEngineSketchOperationVertexMoved;
            ////节点添加操作 多种方式可以自处理
            //ESRI.ArcGIS.Geometry.Point point = new ESRI.ArcGIS.Geometry.Point();
            //AoEditor.sketchOp.Finish(null, opType, point);
        }
    }
}
