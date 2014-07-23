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
    public class EditIt
    {
        private AxMapControl m_MapControl;
        private ILayer m_SelectedLayer;
        private bool m_IsEdited = false;
        private bool m_IsInUse = false;
        private List<IFeature> m_SelectedFeature;
        private IPoint m_CurrentMousePosition;
        private IDisplayFeedback m_FeedBack;
        private IPointCollection m_PointCollection;

        #region 属性
        public AxMapControl EditedMap
        {
            get { return m_MapControl; }
            set { m_MapControl = value; }
        }

        public ILayer CurrentLayer
        {
            get { return m_SelectedLayer; }
            set { m_SelectedLayer = value; }
        }

        /// <summary>
        /// 判断是否处以编辑状态
        /// </summary>
        public bool IsEditing
        {
            get { return m_IsEdited; }
        }

        public List<IFeature> SelectedFeature
        {
            get { return m_SelectedFeature; }
        }

        public EditIt()
        {
            m_MapControl = null;
            m_SelectedLayer = null;
            m_SelectedFeature = new List<IFeature>();
            m_CurrentMousePosition = null;
            m_FeedBack = null;
            m_PointCollection = null;
        }

        public EditIt(AxMapControl editedMap)
        {
            m_MapControl = editedMap;
            m_SelectedFeature = new List<IFeature>();
            m_CurrentMousePosition = null;
            m_FeedBack = null;
            m_PointCollection = null;
        }

        public IGeometry MouseClickGeometry
        {
            get
            {
                if (m_SelectedFeature.Count > 0)
                {
                    return m_SelectedFeature[0].Shape;
                }
                else return null;
            }
        }
        #endregion

        #region MapControl显示控制
        /// <summary>
        /// 设置鼠标样式
        /// </summary>
        /// <param name="pointer"></param>
        public void SetMapcontrolMousePointer(esriControlsMousePointer pointer)
        {
            m_MapControl.MousePointer = pointer;
        }

        /// <summary>
        /// 清除要素选择状态，恢复常态
        /// </summary>
        public void ClearSelection()
        {
            m_MapControl.Map.ClearSelection();
            m_MapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeography, m_SelectedLayer, null);
        }

        /// <summary>
        /// 在要素上面绘制一个可拖拽的符号
        /// </summary>
        /// <param name="geometry"></param>
        public void DrawEditSymbol(IGeometry geometry, IDisplay display)
        {
            IEngineEditProperties engineProperty = new EngineEditorClass();

            ISymbol pointSymbol = engineProperty.SketchVertexSymbol as ISymbol;
            ISymbol sketchSymbol = engineProperty.SketchSymbol as ISymbol;

            ITopologicalOperator pTopo = geometry as ITopologicalOperator;

            sketchSymbol.SetupDC(display.hDC, display.DisplayTransformation);
            sketchSymbol.Draw(pTopo.Boundary);

            IPointCollection pointCol = geometry as IPointCollection;
            for (int i = 0; i < pointCol.PointCount; i++)
            {
                IPoint point = pointCol.get_Point(i);
                pointSymbol.SetupDC(display.hDC, display.DisplayTransformation);
                pointSymbol.Draw(point);
                pointSymbol.ResetDC();
            }

            //m_MapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeography, m_SelectedLayer, null);
            //ESRI.ArcGIS.ADF.ComReleaser.ReleaseCOMObject(sketchSymbol);
            //ESRI.ArcGIS.ADF.ComReleaser.ReleaseCOMObject(pointSymbol);
            //ESRI.ArcGIS.ADF.ComReleaser.ReleaseCOMObject(engineProperty);
        }
        #endregion

        #region 开始、结束编辑
        /// <summary>
        /// 开始编辑
        /// </summary>
        /// <param name="bWithUndoRedo"></param>
        public void StartEditing(bool bWithUndoRedo)
        {
            if (m_SelectedLayer == null) return;
            IFeatureLayer featureLayer = m_SelectedLayer as IFeatureLayer;
            if (featureLayer == null) return;
            IFeatureClass featureClass = featureLayer.FeatureClass;
            if (featureClass == null) return;

            IDataset dataset = featureClass as IDataset;
            IWorkspaceEdit workspaceEdit = dataset.Workspace as IWorkspaceEdit;
            try
            {
                workspaceEdit.StartEditing(bWithUndoRedo);
                m_IsEdited = true;
            }
            catch
            {
                return;
            }
        }

        /// <summary>
        /// 结束编辑
        /// </summary>
        /// <param name="bSave"></param>
        public void StopEditing(bool bSave)
        {
            if (m_IsEdited)
            {
                m_IsEdited = false;

                if (m_SelectedLayer == null) return;
                IFeatureLayer featureLayer = m_SelectedLayer as IFeatureLayer;
                if (featureLayer == null) return;
                IFeatureClass featureClass = featureLayer.FeatureClass;
                if (featureClass == null) return;

                IDataset dataset = featureClass as IDataset;
                IWorkspaceEdit workspaceEdit = dataset.Workspace as IWorkspaceEdit;
                if (workspaceEdit.IsBeingEdited())
                {
                    try
                    {
                        workspaceEdit.StopEditing(bSave);
                    }
                    catch
                    {
                        workspaceEdit.AbortEditOperation();
                        return;
                    }
                }
            }
        }
        #endregion

        #region 选择要素，使其处于高亮状态
        public void GetFeatureOnMouseDown(int x, int y)
        {
            m_SelectedFeature.Clear();
            try
            {
                if (m_SelectedLayer == null) return;
                IFeatureLayer featureLayer = m_SelectedLayer as IFeatureLayer;
                if (featureLayer == null) return;
                IFeatureClass featureClass = featureLayer.FeatureClass;
                if (featureClass == null) return;

                IPoint point = m_MapControl.ActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(x, y);
                IGeometry geometry = point as IGeometry;

                double length = ConvertPixelsToMapUnits(4);
                ITopologicalOperator pTopo = geometry as ITopologicalOperator;
                IGeometry buffer = pTopo.Buffer(length);
                geometry = buffer.Envelope as IGeometry;

                ISpatialFilter spatialFilter = new SpatialFilterClass();
                spatialFilter.Geometry = geometry;
                switch (featureClass.ShapeType)
                {
                    case esriGeometryType.esriGeometryPoint:
                        spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelContains;
                        break;
                    case esriGeometryType.esriGeometryPolygon:
                        spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                        break;
                    case esriGeometryType.esriGeometryPolyline:
                        spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelCrosses;
                        break;
                }
                spatialFilter.GeometryField = featureClass.ShapeFieldName;
                IQueryFilter filter = spatialFilter as IQueryFilter;

                IFeatureCursor cursor = featureClass.Search(filter, false);
                IFeature pfeature = cursor.NextFeature();
                while (pfeature != null)
                {
                    m_SelectedFeature.Add(pfeature);
                    pfeature = cursor.NextFeature();
                }
            }
            catch
            {
                return;
            }
        }

        /// <summary>
        /// 根据鼠标点击位置使击中要素处于高亮显示状态
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void SelectOnMouseDown()
        {
            try
            {
                if (m_SelectedLayer == null) return;
                m_MapControl.Map.ClearSelection();
                m_MapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);
                foreach (IFeature feature in m_SelectedFeature.ToArray())
                {
                    m_MapControl.Map.SelectFeature(m_SelectedLayer, feature);
                }
                m_MapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);
            }
            catch
            { return; }
        }
        #endregion

        #region 绘制新图形

        public void SketchMouseDown(int x, int y)
        {
            if (m_SelectedLayer == null) return;
            if ((m_SelectedLayer as IGeoFeatureLayer) == null) return;

            IFeatureLayer featureLayer = m_SelectedLayer as IFeatureLayer;
            IFeatureClass featureClass = featureLayer.FeatureClass;
            if (featureClass == null) return;

            IPoint point = m_MapControl.ActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(x, y);
            INewLineFeedback lineFeedback = null;
            INewPolygonFeedback polygonFeedback = null;
            try
            {
                if (!m_IsInUse)
                {
                    switch (featureClass.ShapeType)
                    {
                        case esriGeometryType.esriGeometryPoint:
                            break;
                        case esriGeometryType.esriGeometryMultipoint:
                            m_IsInUse = true;
                            m_FeedBack = new NewMultiPointFeedbackClass();
                            INewMultiPointFeedback multiPointFeedback = m_FeedBack as INewMultiPointFeedback;
                            m_PointCollection = new MultipointClass();
                            multiPointFeedback.Start(m_PointCollection, point);
                            break;
                        case esriGeometryType.esriGeometryPolyline:
                            m_IsInUse = true;
                            m_FeedBack = new NewLineFeedbackClass();
                            lineFeedback = m_FeedBack as INewLineFeedback;
                            lineFeedback.Start(point);
                            break;
                        case esriGeometryType.esriGeometryPolygon:
                            m_IsInUse = true;
                            m_FeedBack = new NewPolygonFeedbackClass();
                            polygonFeedback = m_FeedBack as INewPolygonFeedback;
                            polygonFeedback.Start(point);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    if ((m_FeedBack as INewMultiPointFeedback) != null)
                    {
                        object missing = Type.Missing;
                        m_PointCollection.AddPoint(point, ref missing, ref missing);
                    }
                    else if ((m_FeedBack as INewLineFeedback) != null)
                    {
                        lineFeedback = m_FeedBack as INewLineFeedback;
                        lineFeedback.AddPoint(point);
                    }
                    else if ((m_FeedBack as INewPolygonFeedback) != null)
                    {
                        polygonFeedback = m_FeedBack as INewPolygonFeedback;
                        polygonFeedback.AddPoint(point);
                    }
                }
            }
            catch { return; }
        }

        public void SketchMouseMove(int x, int y)
        {
            if ((!m_IsInUse) || (m_FeedBack == null)) return;

            m_CurrentMousePosition = m_MapControl.ActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(x, y);
            m_FeedBack.MoveTo(m_CurrentMousePosition);
        }

        public void EndSketch()
        {
            IGeometry resGeometry = null;
            IPointCollection pointColl = null;

            try
            {
                if ((m_FeedBack as INewMultiPointFeedback) != null)
                {
                    INewMultiPointFeedback multiPointFeedback = m_FeedBack as INewMultiPointFeedback;
                    multiPointFeedback.Stop();
                }
                else if ((m_FeedBack as INewLineFeedback) != null)
                {
                    INewLineFeedback lineFeedback = m_FeedBack as INewLineFeedback;
                    lineFeedback.AddPoint(m_CurrentMousePosition);
                    IPolyline polyline = lineFeedback.Stop();
                    pointColl = polyline as IPointCollection;
                    if (pointColl.PointCount > 1) resGeometry = pointColl as IGeometry;
                }
                else if ((m_FeedBack as INewPolygonFeedback) != null)
                {
                    INewPolygonFeedback polygonFeedback = m_FeedBack as INewPolygonFeedback;
                    polygonFeedback.AddPoint(m_CurrentMousePosition);
                    IPolygon polygon = polygonFeedback.Stop();
                    if (polygon != null)
                    {
                        pointColl = polygon as IPointCollection;
                        if (pointColl.PointCount > 2)
                        {
                            resGeometry = pointColl as IGeometry;
                            ITopologicalOperator pTopo = resGeometry as ITopologicalOperator;
                            if (!pTopo.IsKnownSimple) pTopo.Simplify();
                        }
                    }
                }
                m_FeedBack = null;
                m_IsInUse = false;
            }
            catch { return; }
        }
        #endregion

        #region 编辑要素节点
        public void SnapVertex(int x, int y, IGeometry snapContainer, ref bool vertexSnaped, ref bool contained)
        {
            IPoint point = m_MapControl.ActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(x, y);
            IPoint pHitPoint = null;
            double hitDist = -1, tol = -1;
            int vertexIndex = -1, partIndex = -1;
            bool vertex = false;

            tol = ConvertPixelsToMapUnits(4);

            IHitTest pHitTest = snapContainer as IHitTest;
            bool bHit = pHitTest.HitTest(point, tol, esriGeometryHitPartType.esriGeometryPartVertex, pHitPoint, ref hitDist, ref partIndex, ref vertexIndex, ref vertex);
            vertexSnaped = false;
            contained = false;
            if (bHit)
            {
                m_MapControl.MousePointer = esriControlsMousePointer.esriPointerCrosshair;
                vertexSnaped = true;
                return;
            }
            else
            {
                IRelationalOperator pRelOperator = null;
                ITopologicalOperator pTopo = null;
                IGeometry buffer = null;
                IPolygon polygon = null;
                switch (snapContainer.GeometryType)
                {
                    case esriGeometryType.esriGeometryPolyline:
                        pTopo = snapContainer as ITopologicalOperator;
                        buffer = pTopo.Buffer(3);
                        polygon = buffer as IPolygon;
                        pRelOperator = polygon as IRelationalOperator;
                        break;
                    case esriGeometryType.esriGeometryPolygon:
                        polygon = snapContainer as IPolygon;
                        pRelOperator = polygon as IRelationalOperator;
                        break;
                    case esriGeometryType.esriGeometryPoint:
                        pTopo = snapContainer as ITopologicalOperator;
                        buffer = pTopo.Buffer(3);
                        polygon = buffer as IPolygon;
                        pRelOperator = polygon as IRelationalOperator;
                        break;
                    default:
                        break;
                }

                if (pRelOperator == null) return;
                if (pRelOperator.Contains(point))
                {
                    m_MapControl.MousePointer = esriControlsMousePointer.esriPointerSizeAll;
                    contained = true;
                }
                else m_MapControl.MousePointer = esriControlsMousePointer.esriPointerArrow;
                return;
            }
        }

        public bool EditFeature(int x, int y, IGeometry geometry)
        {
            GetFeatureOnMouseDown(x, y);
            SelectOnMouseDown();
            if (m_SelectedFeature.Count < 1) return false;
            if (geometry == null) return false;

            IPoint pHitPoint = null;
            double hitDist = 0, tol = 0;
            int vertexIndex = 0, vertexOffset = 0, numVertices = 0, partIndex = 0;
            bool vertex = false;

            IFeature editedFeature = m_SelectedFeature[0];
            IPoint point = m_MapControl.ActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(x, y);
            tol = ConvertPixelsToMapUnits(4);
            //IGeometry pGeo = editedFeature.Shape;
            //m_EditingFeature = editedFeature;

            try
            {
                switch (geometry.GeometryType)
                {
                    case esriGeometryType.esriGeometryPoint:
                        m_FeedBack = new MovePointFeedbackClass();
                        m_FeedBack.Display = m_MapControl.ActiveView.ScreenDisplay;
                        IMovePointFeedback pointMove = m_FeedBack as IMovePointFeedback;
                        pointMove.Start(geometry as IPoint, point);
                        break;
                    case esriGeometryType.esriGeometryPolyline:
                        if (TestGeometryHit(tol, point, geometry, ref pHitPoint, ref hitDist, ref partIndex, ref vertexOffset, ref vertexIndex, ref vertex))
                        {
                            if (!vertex)
                            {
                                IGeometryCollection geometryColl = geometry as IGeometryCollection;
                                IPath path = geometryColl.get_Geometry(partIndex) as IPath;
                                IPointCollection pointColl = path as IPointCollection;
                                numVertices = pointColl.PointCount;
                                object missing = Type.Missing;

                                if (vertexIndex == 0)
                                {
                                    object start = 1 as object;
                                    pointColl.AddPoint(point, ref start, ref missing);
                                }
                                else
                                {
                                    object objVertexIndex = vertexIndex as object;
                                    pointColl.AddPoint(point, ref missing, ref objVertexIndex);
                                }
                                TestGeometryHit(tol, point, geometry, ref pHitPoint, ref hitDist, ref partIndex, ref vertexOffset, ref vertexIndex, ref vertex);
                            }
                            m_FeedBack = new LineMovePointFeedbackClass();
                            m_FeedBack.Display = m_MapControl.ActiveView.ScreenDisplay;
                            ILineMovePointFeedback lineMove = m_FeedBack as ILineMovePointFeedback;
                            lineMove.Start(geometry as IPolyline, vertexIndex, point);
                        }
                        else return false;
                        break;
                    case esriGeometryType.esriGeometryPolygon:
                        if (TestGeometryHit(tol, point, geometry, ref pHitPoint, ref hitDist, ref partIndex, ref vertexOffset, ref vertexIndex, ref vertex))
                        {
                            if (!vertex)
                            {
                                IGeometryCollection geometryColl = geometry as IGeometryCollection;
                                IPath path = geometryColl.get_Geometry(partIndex) as IPath;
                                IPointCollection pointColl = path as IPointCollection;
                                numVertices = pointColl.PointCount;
                                object missing = Type.Missing;
                                if (vertexIndex == 0)
                                {
                                    object start = 1 as object;
                                    pointColl.AddPoint(point, ref start, ref missing);
                                }
                                else
                                {
                                    object objVertexIndex = vertexIndex as object;
                                    pointColl.AddPoint(point, ref missing, ref objVertexIndex);
                                }
                                TestGeometryHit(tol, point, geometry, ref pHitPoint, ref hitDist, ref partIndex, ref vertexOffset, ref vertexIndex, ref vertex);
                            }
                            m_FeedBack = new PolygonMovePointFeedbackClass();
                            m_FeedBack.Display = m_MapControl.ActiveView.ScreenDisplay;
                            IPolygonMovePointFeedback polyMove = m_FeedBack as IPolygonMovePointFeedback;
                            polyMove.Start(geometry as IPolygon, vertexIndex + vertexOffset, point);
                        }
                        else return false;
                        break;
                    default:
                        break;
                }
            }
            catch { return false; }
            return true;
        }

        public void FeatureEditMouseMove(int x, int y)
        {
            if (m_FeedBack == null) return;
            IPoint point = m_MapControl.ActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(x, y);
            m_FeedBack.MoveTo(point);
        }

        public IGeometry EndFeatureEdit(int x, int y)
        {
            if (m_FeedBack == null) return null;

            IGeometry geometry = null;
            IPoint point = m_MapControl.ActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(x, y);
            if ((m_FeedBack as IMovePointFeedback) != null)
            {
                IMovePointFeedback pointMove = m_FeedBack as IMovePointFeedback;
                geometry = pointMove.Stop() as IGeometry;
            }
            else if ((m_FeedBack as ILineMovePointFeedback) != null)
            {
                ILineMovePointFeedback lineMove = m_FeedBack as ILineMovePointFeedback;
                geometry = lineMove.Stop() as IGeometry;
            }
            else if ((m_FeedBack as IPolygonMovePointFeedback) != null)
            {
                IPolygonMovePointFeedback polyMove = m_FeedBack as IPolygonMovePointFeedback;
                geometry = polyMove.Stop() as IGeometry;
            }
            m_FeedBack = null;
            m_MapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeography, m_SelectedLayer, null);
            return geometry;
        }
        #endregion

        #region 移动要素

        public void FeatureMoveMouseDown(int x, int y)
        {
            if (m_SelectedLayer == null) return;
            if ((m_SelectedLayer as IGeoFeatureLayer) == null) return;

            IFeatureLayer featureLayer = m_SelectedLayer as IFeatureLayer;
            IFeatureClass featureClass = featureLayer.FeatureClass;
            if (featureClass == null) return;

            if (m_SelectedFeature.Count < 1) return;
            IFeature feature = m_SelectedFeature[0];
            IGeometry startGeom = feature.Shape;

            IPoint point = m_MapControl.ActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(x, y);
            try
            {

                switch (featureClass.ShapeType)
                {
                    case esriGeometryType.esriGeometryPoint:
                        m_FeedBack = new MovePointFeedbackClass();
                        m_FeedBack.Display = m_MapControl.ActiveView.ScreenDisplay;
                        IMovePointFeedback pointMoveFeedback = m_FeedBack as IMovePointFeedback;
                        pointMoveFeedback.Start(startGeom as IPoint, point);
                        break;
                    case esriGeometryType.esriGeometryPolyline:
                        m_FeedBack = new MoveLineFeedbackClass();
                        m_FeedBack.Display = m_MapControl.ActiveView.ScreenDisplay;
                        IMoveLineFeedback lineMoveFeedback = m_FeedBack as IMoveLineFeedback;
                        lineMoveFeedback.Start(startGeom as IPolyline, point);
                        break;
                    case esriGeometryType.esriGeometryPolygon:
                        m_FeedBack = new MovePolygonFeedbackClass();
                        m_FeedBack.Display = m_MapControl.ActiveView.ScreenDisplay;
                        IMovePolygonFeedback polygonMoveFeedback = m_FeedBack as IMovePolygonFeedback;
                        polygonMoveFeedback.Start(startGeom as IPolygon, point);
                        break;
                    default:
                        break;
                }
            }
            catch { return; }
        }

        public void FeatureMoveMouseMove(int x, int y)
        {
            if (m_FeedBack == null) return;
            IPoint point = m_MapControl.ActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(x, y);
            m_FeedBack.MoveTo(point);
        }

        public IGeometry EndFeatureMove(int x, int y)
        {
            if (m_FeedBack == null) return null;
            IGeometry geometry = null;
            try
            {
                if ((m_FeedBack as IMovePointFeedback) != null)
                {
                    IMovePointFeedback pointMoveFeedback = m_FeedBack as IMovePointFeedback;
                    geometry = pointMoveFeedback.Stop();
                }
                else if ((m_FeedBack as IMoveLineFeedback) != null)
                {
                    IMoveLineFeedback lineMoveFeedback = m_FeedBack as IMoveLineFeedback;
                    geometry = lineMoveFeedback.Stop();
                }
                else if ((m_FeedBack as IMovePolygonFeedback) != null)
                {
                    IMovePolygonFeedback polygonMoveFeedback = m_FeedBack as IMovePolygonFeedback;
                    geometry = polygonMoveFeedback.Stop();
                }
            }
            catch { return null; }
            m_FeedBack = null;
            return geometry;
        }

        #endregion

        #region 更新要素(编辑、移动后)

        public bool UpdateEdit(IGeometry newGeom)
        {
            if (m_SelectedFeature.Count < 1) return false;
            if (newGeom == null) return false;
            if (newGeom.IsEmpty) return false;

            IFeature feature = m_SelectedFeature[0];
            IDataset dataset = feature.Class as IDataset;
            IWorkspaceEdit workspaceEdit = dataset.Workspace as IWorkspaceEdit;
            if (!workspaceEdit.IsBeingEdited()) return false;

            workspaceEdit.StartEditOperation();
            feature.Shape = newGeom;
            feature.Store();
            workspaceEdit.StopEditOperation();
            m_SelectedFeature.Clear();
            m_SelectedFeature.Add(feature);
            ClearSelection();
            //m_MapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGraphicSelection, null, null);
            //m_MapControl.Map.SelectFeature(m_SelectedLayer, feature);
            //m_MapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGraphicSelection, null, null);
            return true;
        }

        public void UndoEdit()
        {
            if (m_SelectedLayer == null) return;

            IFeatureLayer featLayer = m_SelectedLayer as IFeatureLayer;
            IDataset dataset = featLayer.FeatureClass as IDataset;
            IWorkspaceEdit workspaceEdit = dataset.Workspace as IWorkspaceEdit;
            bool bHasUndos = false;
            workspaceEdit.HasUndos(ref bHasUndos);
            if (bHasUndos)
            {
                workspaceEdit.UndoEditOperation();
            }
            ClearSelection();
        }

        public void RedoEdit()
        {
            if (m_SelectedLayer == null) return;

            IFeatureLayer featLayer = m_SelectedLayer as IFeatureLayer;
            IDataset dataset = featLayer.FeatureClass as IDataset;
            IWorkspaceEdit workspaceEdit = dataset.Workspace as IWorkspaceEdit;
            bool bHasUndos = false;
            workspaceEdit.HasRedos(ref bHasUndos);
            if (bHasUndos)
            {
                workspaceEdit.RedoEditOperation();
            }
            ClearSelection();
        }

        #endregion


        #region 私有函数区
        private double ConvertPixelsToMapUnits(double pixelUnits)
        {
            int pixelExtent = m_MapControl.ActiveView.ScreenDisplay.DisplayTransformation.get_DeviceFrame().right
                           - m_MapControl.ActiveView.ScreenDisplay.DisplayTransformation.get_DeviceFrame().left;

            double realWorldDisplayExtent = m_MapControl.ActiveView.ScreenDisplay.DisplayTransformation.VisibleBounds.Width;
            double sizeOfOnePixel = realWorldDisplayExtent / pixelExtent;

            return pixelUnits * sizeOfOnePixel;
        }

        private bool TestGeometryHit(double tol, IPoint pPoint, IGeometry geometry, ref IPoint pHitPoint,
                                   ref double hitDist, ref int partIndex, ref int vertexOffset,
                                   ref int vertexIndex, ref bool vertexHit)
        {
            IHitTest pHitTest = geometry as IHitTest;
            pHitPoint = new PointClass();
            bool last = true;
            bool res = false;
            if (pHitTest.HitTest(pPoint, tol, esriGeometryHitPartType.esriGeometryPartVertex, pHitPoint, ref hitDist, ref partIndex, ref vertexIndex, ref last))
            {
                vertexHit = true;
                res = true;
            }
            else
            {
                if (pHitTest.HitTest(pPoint, tol, esriGeometryHitPartType.esriGeometryPartBoundary, pHitPoint, ref hitDist, ref partIndex, ref vertexIndex, ref last))
                {
                    vertexHit = false;
                    res = true;
                }
            }

            if (partIndex > 0)
            {
                IGeometryCollection pGeoColl = geometry as IGeometryCollection;
                vertexOffset = 0;
                for (int i = 0; i < partIndex; i = 2 * i + 1)
                {
                    IPointCollection pointColl = pGeoColl.get_Geometry(i) as IPointCollection;
                    vertexOffset = vertexOffset + pointColl.PointCount;
                }
            }
            return res;
        }

        #endregion

        #region 合并要素
        private void MergeFeatures(List<int> OneFeatureArr, ILayer tSelectLayer, IFeatureWorkspace pCommonFeaureWorkspace)
        {
            IFeatureLayer tFeatureLayer = tSelectLayer as IFeatureLayer;
            IFeatureClass pFeatureClass = tFeatureLayer.FeatureClass;
            var pDataset = pFeatureClass as IDataset;
            IWorkspace pWorkspace = pDataset.Workspace;
            IFeatureWorkspace pFWs = pWorkspace as IFeatureWorkspace;
            pCommonFeaureWorkspace = pFWs;

            IWorkspaceEdit pWorkspaceEdit = (IWorkspaceEdit)pCommonFeaureWorkspace;
            pWorkspaceEdit.StartEditing(true);
            pWorkspaceEdit.StartEditOperation();

            IFeatureLayer pFeatureLayer = tSelectLayer as IFeatureLayer;
            IFeatureCursor pEF = pFeatureLayer.Search(null, false);
            IFeature pFeatureFirst = pEF.NextFeature();
            while (pFeatureFirst != null)
            {//找到在数组里的第一个要素
                if (pFeatureFirst.OID == OneFeatureArr[0])
                {
                    //OneFeatureArr.RemoveAt(0);//除掉第一个要素
                    break;
                }
                else
                    pFeatureFirst = pEF.NextFeature();
            }
            pEF = pFeatureLayer.Search(null, false);

            IGeometry pGeometryFirst = pFeatureFirst.Shape;
            ITopologicalOperator2 pTopOperatorFirst = (ITopologicalOperator2)pGeometryFirst;
            IRelationalOperator pRelOperatorFirst = (IRelationalOperator)pGeometryFirst;
            pTopOperatorFirst.IsKnownSimple_2 = false;
            pTopOperatorFirst.Simplify();
            pGeometryFirst.SnapToSpatialReference();

            IGeometry pGeometrySecond = null;
            IFeature pFeatureSecond = pEF.NextFeature();

            IGeometryCollection Geometrybag = new GeometryBagClass();//装geometry的袋子
            object oMissing = Type.Missing;
            while (pFeatureSecond != null)
            {
                if (OneFeatureArr.IndexOf(pFeatureSecond.OID) == -1)
                {
                    pFeatureSecond = pEF.NextFeature();
                    continue;
                }
                pGeometrySecond = pFeatureSecond.ShapeCopy;
                Geometrybag.AddGeometry(pGeometrySecond, ref oMissing, ref oMissing);//将geometry装进袋子
                pFeatureSecond = pEF.NextFeature();
            }
            IEnumGeometry tEnumGeometry = (IEnumGeometry)Geometrybag;
            pTopOperatorFirst.ConstructUnion(tEnumGeometry);

            pTopOperatorFirst.IsKnownSimple_2 = false;
            pTopOperatorFirst.Simplify();
            pFeatureFirst.Shape = pGeometryFirst;
            pFeatureFirst.Store();

            pWorkspaceEdit.StopEditOperation();
            pWorkspaceEdit.StopEditing(true);
        }

    /*
            IFeatureCursor pFeatCur = pZoning.Update(null, false);
            IFeature pFeat = pFeatCur.NextFeature();
            ISpatialFilter pSpatialFilter = new SpatialFilterClass();
            pSpatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelContains;
            pSpatialFilter.GeometryField = pZoning.ShapeFieldName;
            while (pFeat != null)
            {
                pTopo = pFeat.ShapeCopy as ITopologicalOperator;
                pConvexHull = pTopo.ConvexHull();
                pSpatialFilter.Geometry = pConvexHull;
                pSpatialFilter.WhereClause = pZoning.OIDFieldName + " <> " + pFeat.OID;
                pSubCur = pZoning.Update(pSpatialFilter, false);
                pSubFeat = pSubCur.NextFeature();
                if (pSubFeat == null)//如果没有其他多边形包含在该多边形的凸包内
                {
                    pFeat.set_Value(pZoning.FindField("cluster"), i);
                    pFeatCur.UpdateFeature(pFeat);
                }
                else   //否则将其合并到该多边形
                {
                    pGeo = pFeat.ShapeCopy;
                    while (pSubFeat != null)
                    {
                        if (DeleteList.Contains(pSubFeat.OID)==true)//DeleteList记录已删除的FeatureOID
                        { pSubFeat = pSubCur.NextFeature(); continue; }
                        else
                        {
                            DeleteList.Add(pSubFeat.OID);
                            pTopo = pGeo as ITopologicalOperator;
                            pGeo = pTopo.Union(pSubFeat.ShapeCopy);//union返回的结果应存到pGeo，pTopo也必须更新
                            pSubCur.DeleteFeature();
                        }
                        pSubFeat = pSubCur.NextFeature();
                    }
                    pFeat.Shape = pGeo;//结果表明，pFeat的图形是变了的
                    pFeat.Store();
                    pFeatCur.UpdateFeature(pFeat);
                }
                i = i + 1;
                pFeat = pFeatCur.NextFeature();
            }
        }
     * 
     * */
        //public void UnionFeatures()
        //{
        //    IMap pMap = MapControl.Map;
        //    IActiveView pActiveView = pMap as IActiveView;
        //    IGeometryCollection Geometrybag = new GeometryBagClass();//装geometry的袋子
        //    IEnumFeature pEnumFeat = (IEnumFeature)pMap.FeatureSelection;
        //    IFeature pFeat = pEnumFeat.Next();
        //    object oMissing = Type.Missing;
        //    while (pFeat != null)
        //    {
        //        IGeometry pGeometry = pFeat.Shape as IGeometry;
        //        Geometrybag.AddGeometry(pGeometry, ref oMissing, ref oMissing);//将geometry装进袋子
        //        pFeat.Delete();
        //        pFeat = pEnumFeat.Next();
        //    }
        //    ITopologicalOperator unionedpolygon = new PolygonClass();
        //    unionedpolygon.ConstructUnion(Geometrybag as IEnumGeometry);//
        //    IGeometry pGeo = unionedpolygon as IGeometry;
        //    CreateUnionFeature(pGeo);
        //    pActiveView.Refresh();
        //}
        #endregion
    }
}
