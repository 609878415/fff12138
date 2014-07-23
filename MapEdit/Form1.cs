using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;

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

using MapEdit.Nitool;
using MapEdit;




namespace MapEdit
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            
            InitializeComponent();
            
        }

        public ILayer flaglayer=null;
        public IWorkspace flagWorkspace;
        public IFeatureLayer mfeaturelayer;
        public IFeatureClass mfeatureclass;
        public IDataset dataset;
        public IWorkspace mworkspace;
        public IWorkspaceEdit CurWorkspaceEdit;

        private ITOCControl2 m_tocControl = null;
        private IMapControl3 m_mapControl = null;
        private IToolbarMenu m_menuMap = null;
        private IToolbarMenu m_menuLayer = null;
        public bool m_IsWorkspaceEdited = false;//编辑状态标签
        
        

        //string FlagEdit;
        public ILayer mlayer;
        public IMap m_map;
        
      
        private IEngineEditor mEngineEditor = new EngineEditorClass();
        private IEngineEditLayers mEngineEditLayer;    //开始编辑图层

        

        
        
        private void Form1_Load(object sender, EventArgs e)
        {
            
            axTOCControl1.SetBuddyControl(axMapControl1);

            
            //启动默认为pan
            ICommand command = new ControlsMapPanToolClass();
            command.OnCreate(axMapControl1.Object);
            axMapControl1.CurrentTool = command as ITool;

            m_tocControl = (ITOCControl2)axTOCControl1.Object;
            m_mapControl = (IMapControl3)axMapControl1.Object;

            m_menuMap = new ToolbarMenuClass();
            m_menuLayer = new ToolbarMenuClass();

            
            
            istoolenable(false);
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        #region 地图打开、保存、另存为、添加图层

        private void New_map_Click(object sender, EventArgs e)
        {

            MapClass.NewMapDoc(axMapControl1);
        }

        private void Open_Map_Click(object sender, EventArgs e)
        {
            ICommand command = new ControlsOpenDocCommandClass();
            command.OnCreate(axMapControl1.Object);
            command.OnClick();
            //MapClass.OpenMapDoc(axMapControl1);

            toolStripComboBox2.Items.Clear();

            for (int i = 0; i < axMapControl1.LayerCount; i++)
            {
                toolStripComboBox2.Items.Add(axMapControl1.get_Layer(i).Name);
            }
        }

        private void Save_Map_Click(object sender, EventArgs e)
        {
            if (this.axMapControl1.LayerCount == 0)
            {
                MessageBox.Show("没有地图");
                return;
            }
            else
            MapClass.SaveMapDoc();
        }

        private void SaveAs_Map_Click(object sender, EventArgs e)
        {
            if (this.axMapControl1.LayerCount == 0)
            {
                MessageBox.Show("没有地图");
                return;
            }
            else
            {
                ICommand command = new ControlsSaveAsDocCommandClass();
                command.OnCreate(axMapControl1.Object);
                command.OnClick(); 
            }
        }

        //打开图层
        private void Add_Data_Click(object sender, EventArgs e)
        {
            int currentLayerCount = this.axMapControl1.LayerCount;
            ICommand pCommand = new ControlsAddDataCommandClass();
            pCommand.OnCreate(this.axMapControl1.Object);
            pCommand.OnClick();
        }

        #endregion


        private void axMapControl1_OnMouseDown(object sender, IMapControlEvents2_OnMouseDownEvent e)
        {
                  
        }

        private void select_layer_Click(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            for (int i = 0; i < axMapControl1.LayerCount; i++)
            {
                comboBox1.Items.Add(axMapControl1.get_Layer(i).Name);
            }
        }

        private void toolStripDropDownButton1_Click(object sender, EventArgs e)
        {

        }
        #region  各种command工具
        /// <summary>
        /// 各种工具
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            //放大
            ICommand pCommand = new ControlsMapZoomInToolClass();
            
            switch (this.tabControl1.SelectedIndex)
            {
                case 0:
                    pCommand.OnCreate(this.axMapControl1.Object);
                    this.axMapControl1.CurrentTool = pCommand as ITool;
                    break;
                case 1:
                    pCommand.OnCreate(this.axPageLayoutControl1.Object);
                    this.axPageLayoutControl1.CurrentTool = pCommand as ITool;
                    break;
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            //缩小
            ICommand pCommand = new ControlsMapZoomOutTool();
            switch (this.tabControl1.SelectedIndex)
            {
                case 0:
                    pCommand.OnCreate(this.axMapControl1.Object);
                    this.axMapControl1.CurrentTool = pCommand as ITool;
                    break;
                case 1:
                    pCommand.OnCreate(this.axPageLayoutControl1.Object);
                    this.axPageLayoutControl1.CurrentTool = pCommand as ITool;
                    break;
            }
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            ICommand pCommand = new ControlsMapPanTool();
            switch (this.tabControl1.SelectedIndex)
            {
                case 0:
                    pCommand.OnCreate(this.axMapControl1.Object);
                    this.axMapControl1.CurrentTool = pCommand as ITool;
                    break;
                case 1:
                    pCommand.OnCreate(this.axPageLayoutControl1.Object);
                    this.axPageLayoutControl1.CurrentTool = pCommand as ITool;
                    break;
            }
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            ICommand pCommand = new ControlsMapZoomInFixedCommand();
            pCommand.OnCreate(this.axMapControl1.Object);
            pCommand.OnClick();
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            ICommand pCommand = new ControlsMapZoomOutFixedCommand();
            pCommand.OnCreate(this.axMapControl1.Object);
            pCommand.OnClick();
            
        }

        private void axMapControl1_OnAfterScreenDraw(object sender, IMapControlEvents2_OnAfterScreenDrawEvent e)
        {
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            ICommand pCommand = new ControlsMapZoomToLastExtentBackCommand();
            pCommand.OnCreate(this.axMapControl1.Object);
            pCommand.OnClick();
        }

        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            ICommand pCommand = new ControlsMapZoomToLastExtentForwardCommand();
            pCommand.OnCreate(this.axMapControl1.Object);
            pCommand.OnClick();
        }

        

        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            ICommand pCommand = new ControlsClearSelectionCommand();
            pCommand.OnCreate(this.axMapControl1.Object);
            this.axMapControl1.CurrentTool = pCommand as ITool;
        }

        
        private void toolStripDropDownButton2_Click(object sender, EventArgs e)
        {
            ICommand pCommand = new ControlsSelectFeaturesTool();
            pCommand.OnCreate(this.axMapControl1.Object);
            this.axMapControl1.CurrentTool = pCommand as ITool;
        }

        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            ICommand pCommand = new ControlsSelectTool();
            pCommand.OnCreate(this.axMapControl1.Object);
            this.axMapControl1.CurrentTool = pCommand as ITool;
        }
        private void toolStripButton11_Click(object sender, EventArgs e)
        {
            ICommand pCommand = new ControlsMapIdentifyTool();
            pCommand.OnCreate(this.axMapControl1.Object);
            this.axMapControl1.CurrentTool = pCommand as ITool;
        }

        

        #endregion


        #region 图片导出
        private void toolStripButton10_Click(object sender, EventArgs e)
        {
             try
            {
                SaveFileDialog exportJPGDialog = new SaveFileDialog();
                exportJPGDialog.Title = "导出JPEG图像";
                exportJPGDialog.Filter = "Jpeg Files(*.jpg,*.jpeg)|*.jpg,*.jpeg";
                exportJPGDialog.RestoreDirectory = true;
                exportJPGDialog.ValidateNames = true;
                exportJPGDialog.OverwritePrompt = true;
                exportJPGDialog.DefaultExt = "jpg";

                if (exportJPGDialog.ShowDialog() == DialogResult.OK)
                {
                    double lScreenResolution;
                    lScreenResolution = axMapControl1.ActiveView.ScreenDisplay.DisplayTransformation.Resolution;

                    IExport pExporter = new ExportJPEGClass() as IExport;
                    //IExport pExporter = new ExportPDFClass() as IExport;//直接可以用！！
                    pExporter.ExportFileName = exportJPGDialog.FileName;
                    pExporter.Resolution = lScreenResolution;

                    tagRECT deviceRECT;
                    //用这句的话执行到底下的ｏｕｔｐｕｔ（）时就会出现错误：Not enough memory to create requested bitmap
                    //axMapControl1.ActiveView.ScreenDisplay.DisplayTransformation.set_DeviceFrame(ref deviceRECT);
                    deviceRECT = axMapControl1.ActiveView.ExportFrame;

                    IEnvelope pDriverBounds = new EnvelopeClass();
                    //pDriverBounds = axMapControl1.ActiveView.FullExtent;

                    pDriverBounds.PutCoords(deviceRECT.left, deviceRECT.bottom, deviceRECT.right, deviceRECT.top);

                    pExporter.PixelBounds = pDriverBounds;

                    ITrackCancel pCancel = new CancelTrackerClass();
                    axMapControl1.ActiveView.Output(pExporter.StartExporting(), (int)lScreenResolution, ref deviceRECT, axMapControl1.ActiveView.Extent, pCancel);
                    pExporter.FinishExporting();
                    pExporter.Cleanup();
                    MessageBox.Show("图像保存在" + exportJPGDialog.FileName, "保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            { MessageBox.Show(ex.Message); return; }
        
        }
        #endregion


        #region 高亮显示
        private void axMapControl1_OnMouseDown_1(object sender, IMapControlEvents2_OnMouseDownEvent e)
        {
            //axMapControl1.MousePointer = esriControlsMousePointer.esriPointerDefault;
            //IMap pMap = axMapControl1.Map;
            //IGeometry pGeometry = axMapControl1.TrackRectangle();       //获取框选几何
            //ISelectionEnvironment pSelectionEnv = new SelectionEnvironment(); //新建选择环境
            //IRgbColor pColor = new RgbColor();
            //pColor.Red = 255;
            //pSelectionEnv.DefaultColor = pColor;         //设置高亮显示的颜色！

            //pMap.SelectByShape(pGeometry, pSelectionEnv, false); //选择图形！
            //axMapControl1.Refresh(esriViewDrawPhase.esriViewGeoSelection, null, null);
            
        }
        #endregion

        #region 编辑功能
        /// <summary>
        /// 开始编辑
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Star_Edit_Click(object sender, EventArgs e)
        {
            if (flaglayer == null)
            {
                MessageBox.Show("请选取图层");
                return;
            }
            else
            {

                istoolenable(true);
                //for (int i = 0; i < axMapControl1.LayerCount;i++)
                //mlayer = axMapControl1.Map.get_Layer(0);
                m_map = axMapControl1.Map;
                //mlayer = getLayerByName(m_map, comboBox1.SelectedText);
                mfeaturelayer = flaglayer as IFeatureLayer;
                mlayer = flaglayer;

                mfeatureclass = mfeaturelayer.FeatureClass;
                dataset = mfeatureclass as IDataset;
                mworkspace = dataset.Workspace;
                CurWorkspaceEdit = mworkspace as IWorkspaceEdit;

                //CurWorkspaceEdit.StartEditing(true);
                //CurWorkspaceEdit.StartEditOperation();

                mEngineEditor.StartEditing(mworkspace, m_map);
                mEngineEditLayer = mEngineEditor as IEngineEditLayers;
                mEngineEditLayer.SetTargetLayer(mfeaturelayer, 0);
                //((IEngineEditLayers)mEngineEditor).SetTargetLayer(mfeaturelayer, 0);

            }

        }
        private void Stop_Edit_Click(object sender, EventArgs e)
        {
            

            if (mEngineEditor.HasEdits() == false)
            {
                mEngineEditor.StopEditing(false);
            }
            else if(mEngineEditor.HasEdits() == true)
            {
                if (MessageBox.Show("Save Edits?", "Save Prompt", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    mEngineEditor.StopEditing(true);
                }
                else
                {
                    mEngineEditor.StopEditing(false);
                }
            }
           
            //if(CurWorkspaceEdit.IsBeingEdited())
            //{
                //if (MessageBox.Show("是否保存", "保存提示", MessageBoxButtons.OKCancel) == DialogResult.OK)
                //{
                    
                //    CurWorkspaceEdit.StopEditing(true);
                //}
                //else
                //{
                    
                //    CurWorkspaceEdit.StopEditing(false);
                //}
            //}
        }
            
        private void Save_Edit_Click(object sender, EventArgs e)
        {
            ICommand command = new ControlsEditingSaveCommandClass();
            command.OnCreate(axMapControl1.Object);
            command.OnClick();
        }

        private void Add_feature_Click(object sender, EventArgs e)
        {

            
                
            //FlagEdit = "new_vector";
            addfeature();
            

        }

        private void Change_feature_Click(object sender, EventArgs e)
        {
            changefeature();
        }


        private void Delet_feature_Click(object sender, EventArgs e)
        {
            ICommand pCommand = new ControlsEditingClearCommandClass();
            pCommand.OnCreate(axMapControl1.Object);
            pCommand.OnClick();
        }
        private void addfeature()
        {
            ICommand pCommand;
            pCommand = new ControlsClearSelectionCommandClass();
            pCommand.OnCreate(axMapControl1.Object);
            pCommand.OnClick();
            pCommand = new ControlsEditingSketchToolClass();
            pCommand.OnCreate(axMapControl1.Object);
            axMapControl1.CurrentTool = pCommand as ITool;
        }
        private void changefeature()
        {
            ICommand pCommand = new ControlsEditingEditToolClass();
            pCommand.OnCreate(axMapControl1.Object);
            axMapControl1.CurrentTool = pCommand as ITool;
        }
       

        #endregion

        #region 自定义功能
        private void istoolenable(bool enable)
        {
            //控件不显示
            this.Add_feature.Enabled = enable;
            this.Change_feature.Enabled = enable;
            this.Delet_feature.Enabled = enable;
            this.output_image.Enabled = enable;
            this.select_element.Enabled = enable;
            this.clear_select_element.Enabled = enable;
            this.toolStripComboBox1.Enabled = enable;
        }
        

        public ILayer getLayerByName(IMap pMap, string str)
        {
            ILayer pLayer = null;
            for (int i = 0; i < pMap.LayerCount; i++)
            {
                if (str == pMap.get_Layer(i).Name)
                    pLayer = pMap.get_Layer(i);
            }
                return pLayer;
        }

        /// <summary>
        /// 合并元素
        /// </summary>
        /// <param name="pMap">当前操作地图</param>
        /// <param name="layer">当前操作图层</param>
        private void union(IMap pMap,ILayer layer)
        {
            IFeatureLayer pFeatureLayer = layer as IFeatureLayer;
            IDataset pDataset = pFeatureLayer.FeatureClass as IDataset;
            IWorkspaceEdit pWorkspaceEdit = pDataset.Workspace as IWorkspaceEdit;
            pWorkspaceEdit.StartEditOperation();

            IEnumFeature pEnumFeature = pMap.FeatureSelection as IEnumFeature;
            IFeature allFeature = pEnumFeature.Next();

            IFeatureCursor pEF = pFeatureLayer.Search(null, false);
            IFeature pFeature = pEF.NextFeature();
            IGeometry pGeometry = pFeature.Shape;

            IGeometryCollection pGeometrybag=new GeometryBagClass();
            object oMissing = Type.Missing;
            while (allFeature != null)
            {
                IGeometry mGeometry = allFeature.Shape as IGeometry;
                pGeometrybag.AddGeometry(mGeometry, ref oMissing, ref oMissing);
                allFeature.Delete();
                allFeature = pEnumFeature.Next();
            }
            ITopologicalOperator2 pTopOperator = (ITopologicalOperator2)pGeometry;
            IEnumGeometry pEnumGeometry = pGeometrybag as IEnumGeometry;
            pTopOperator.ConstructUnion(pEnumGeometry);
            
            pFeature.Shape = pGeometry;
            pFeature.Store();

            pWorkspaceEdit.StopEditOperation();
           
        }
        /// <summary>
        /// 切割线段
        /// </summary>
        /// <param name="mMap">当前切割的地图</param>
        /// <param name="mLayer">当前切割操作的图层</param>
        private void Cut_Polyline(IMap pMap, ILayer mLayer)
        {
            IActiveView maprefr = (IActiveView)pMap;
            IFeatureLayer pFeatureLayer = (IFeatureLayer)mLayer;
            IDataset pDataset = (IDataset)pFeatureLayer.FeatureClass;
            IWorkspaceEdit pWorkspaceEdit = (IWorkspaceEdit)pDataset.Workspace;
            IEnumFeature pEnumFeature = pMap.FeatureSelection as IEnumFeature;
            IFeature selectFeature = pEnumFeature.Next();

            IFeatureClass pfeatureclass = pFeatureLayer.FeatureClass;

            pWorkspaceEdit.StartEditOperation();


            //IFeatureCursor pEF = pFeatureLayer.Search(null, false);
            //IFeature pFeature = pEF.NextFeature();
            IFeature pFeature = pfeatureclass.CreateFeature();
            IGeometry pGeometry = pFeature.Shape;

          
            //IFeatureClass fClass = pFeatureLayer.FeatureClass;
            //int fCount = fClass.FeatureCount(null);
            IPolyline FirstPolyline = selectFeature.Shape as IPolyline;
            IFeature selectFeature2 = pEnumFeature.Next();
            IPolyline SecondPolyline = selectFeature2.Shape as IPolyline;
            IGeometryCollection geoCollect = new PolylineClass();

            IGeometry left =new PolygonClass();
            IGeometry right=new PolygonClass();

            ITopologicalOperator5 pTopOperator = FirstPolyline as ITopologicalOperator5;
            pTopOperator.IsKnownSimple_2 = false;
            pTopOperator.Simplify();
            try
            {
                
                pTopOperator.Cut(SecondPolyline, out left, out right);
                

            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            selectFeature.Delete();

            
            pGeometry = left;
            pFeature.Shape = pGeometry;
            pFeature.Store();

            pWorkspaceEdit.StopEditOperation();
            maprefr.Refresh();

        }
        /// <summary>
        /// 延长
        /// </summary>
        /// <param name="player">当前操作图层</param>
        /// <param name="pMap">当前操作地图</param>
        private void GetIntersectionPoint(ILayer player, IMap pMap)
        {
            IEnumFeature pEnumFeature = pMap.FeatureSelection as IEnumFeature;
            IFeature selectFeature = pEnumFeature.Next();
            IPolyline extendline = selectFeature.Shape as IPolyline;
            IFeature selectFeature2 = pEnumFeature.Next();
            IPolyline intersectionline = selectFeature2.Shape as IPolyline;

            IActiveView maprefr = (IActiveView)pMap;
            IFeatureLayer pfeaturelayer = (IFeatureLayer)player;
            IFeatureClass pfeatureclass = pfeaturelayer.FeatureClass;
            IDataset pDataset = (IDataset)pfeaturelayer.FeatureClass;
            IWorkspaceEdit pWorkspaceEdit = (IWorkspaceEdit)pDataset.Workspace;
            IFeature addfeature = pfeatureclass.CreateFeature();
            //IPolyline pline = addfeature.Shape as IPolyline;
            IGeometry pGeometry;
            IPoint pPointFrom = new PointClass();
            IPoint pPointTo = new PointClass();
            ILine pLine = new LineClass();
            pLine.PutCoords(pPointFrom, pPointTo);
            //IPolyline pline = new PointClass();

            double X1 = extendline.ToPoint.X;
            double Y1 = extendline.ToPoint.Y;
            double X2 = extendline.FromPoint.X;
            double Y2 = extendline.FromPoint.Y;
            double a1 = Y2 - Y1;
            double b1 = X1 - X2;
            double c1 = X1 * Y2 - X2 * Y1;

            double x1 = intersectionline.ToPoint.X;
            double y1 = intersectionline.ToPoint.Y;
            double x2 = intersectionline.FromPoint.X;
            double y2 = intersectionline.FromPoint.Y;
            double a2 = y2 - y1;
            double b2 = x1 - x2;
            double c2 = x1 * y2 - x2 * y1;
            double isparallline = a1 * b2 - a2 * b1;

            double getpx = (c1 * b2 - c2 * b1) / isparallline;
            double getpy = (a1 * c2 - a2 * c1) / isparallline;

            pWorkspaceEdit.StartEditOperation();

            //pline.ToPoint.X = getpx;
            //pline.ToPoint.Y = getpy;
            //pline.FromPoint = extendline.ToPoint;

            pPointFrom.X = getpx;
            pPointFrom.Y = getpy;
            pPointTo = extendline.ToPoint;
            pLine.PutCoords(pPointFrom, pPointTo);

            ISegmentCollection psegmentcollection = new PolylineClass();
            object oMissing = Type.Missing;
            psegmentcollection.AddSegment((ISegment)pLine, oMissing, oMissing);

            //pline = (IGeometry)psegmentcollection;


            pGeometry = psegmentcollection as IGeometry;
            addfeature.Shape = pGeometry;
            addfeature.Store();

            pWorkspaceEdit.StopEditOperation();

            maprefr.Refresh();

        }
        private void CreateBuffer(IMap mMap,ILayer mLayer)
        {
            IFeatureLayer mFeatureLayer = mLayer as IFeatureLayer;
            IFeatureClass pFeatClass = mFeatureLayer.FeatureClass;
            IDataset pDataset = (IDataset)mFeatureLayer.FeatureClass;
            IWorkspaceEdit pWorkspaceEdit = (IWorkspaceEdit)pDataset.Workspace;
            pWorkspaceEdit.StartEditOperation();

            IFeatureCursor pFeatCursor = pFeatClass.Search(null, false);
            IFeature pFeature=pFeatCursor.NextFeature();

            IGeometry mGeometrty = pFeature.Shape;
            ITopologicalOperator5 mTopoOperator = mGeometrty as ITopologicalOperator5;
            while (pFeature != null)
            {
                //IGeometry pBufferGeo = pFeature.Shape;
                //pBufferGeo = mTopoOperator.Buffer(0.01);
                mTopoOperator.Buffer(0.01);
                pFeature.Store();
                pFeature = pFeatCursor.NextFeature();
                if (pFeature == null)
                {
                    MessageBox.Show("创建完成");
                }
            }
            pWorkspaceEdit.StopEditOperation();
            
        }
        #endregion


        private void axMapControl1_OnMouseMove(object sender, IMapControlEvents2_OnMouseMoveEvent e)
        {
            
            
            ScaleLabel.Text = "1:" + ((long)this.axMapControl1.MapScale).ToString();

            CoordnateLabel.Text = "x=" +e.x.ToString()+ "|" + "y=" + e.y.ToString()+this.axMapControl1.MapUnits;
            
            
        }

        public void axTOCControl1_OnMouseDown(object sender, ITOCControlEvents_OnMouseDownEvent e)
        {
            if (e.button == 2)
            {
                
                esriTOCControlItem Item = esriTOCControlItem.esriTOCControlItemNone;
                IBasicMap pBasicMap = null;
                ILayer pLayer = null;
                object other = null;
                object index = null;
                axTOCControl1.HitTest(e.x, e.y, ref Item, ref pBasicMap, ref pLayer, ref other, ref index);          //实现赋值
                
                if (Item == esriTOCControlItem.esriTOCControlItemLayer)           //点击的是图层的话，就显示右键菜单
                {
                    if (this.toolStripComboBox2.SelectedItem == null)
                    {
                        contextMenuStrip1.Show(MousePosition.X, MousePosition.Y);
                        flaglayer = pLayer;
                        return;
                    }
                    else
                    {
                        if (pLayer.Name == this.toolStripComboBox2.SelectedItem.ToString())

                            //contextMenuStrip1.Show(axTOCControl1, new System.Drawing.Point(e.x, e.y));
                            contextMenuStrip1.Show(MousePosition.X, MousePosition.Y);
                        else
                        {
                            MessageBox.Show("请选择相同图层进行操作");
                            return;
                        }
                        //显示右键菜单，并定义其相对控件的位置，正好在鼠标出显示
                    }
                }
            }
            if (e.button == 1)
            {
                esriTOCControlItem Item = esriTOCControlItem.esriTOCControlItemNone;
                IBasicMap pBasicMap = null;
                ILayer pLayer = null;
                object other = null;
                object index = null;
                axTOCControl1.HitTest(e.x, e.y, ref Item, ref pBasicMap, ref pLayer, ref other, ref index);
                


                mEngineEditor.StopEditing(true);
            }
            istoolenable(false);

        }

        //private void AtrView_Click(object sender, EventArgs e)
        //{
        //    IMap p_map = axMapControl1.Map;
        //    if (comboBox1.SelectedItem == null)
        //    {
        //        MessageBox.Show("请选择图层");
        //    }
        //    else
        //    {
        //        AttributeView frm = new AttributeView();
        //        frm.CreateAttributeTable(getLayerByName(p_map, comboBox1.SelectedItem.ToString()));
        //        frm.Show();
        //    }
        //}
        public ESRI.ArcGIS.Controls.AxMapControl getMapControl()
        {
            return axMapControl1;
        }

        private void tabPage1_MouseLeave(object sender, EventArgs e)
        {
            ScaleLabel.Text="比例尺";
            CoordnateLabel.Text = "坐标";
        }

        private void splitContainer2_Panel2_MouseLeave(object sender, EventArgs e)
        {
            ScaleLabel.Text = "比例尺";
            CoordnateLabel.Text = "坐标";
        }

        private void attributeView_Click(object sender, EventArgs e)
        {
            
            IMap p_map = axMapControl1.Map;
            AttributeView Attr = new AttributeView();
            Attr.fff = this;
            Attr.CreateAttributeTable(flaglayer);
            Attr.Show();
        }

        private void Union_Click(object sender, EventArgs e)
        {
            union(axMapControl1.Map, flaglayer);
        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string flagtext = this.toolStripComboBox1.Text;
            switch (flagtext)
            {
                case "添加":
                    addfeature();
                    break;
                case "编辑":
                    changefeature();
                    break;
            }
        }

        private void toolStripComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            flaglayer = getLayerByName(this.axMapControl1.Map, this.toolStripComboBox2.SelectedItem.ToString());

            istoolenable(false);
        }

        private void Cut_it_Click(object sender, EventArgs e)
        {

            //Cut_Polyline(axMapControl1.Map, flaglayer);
            //ICommand pCommand = new CutFeature();
            //pCommand.OnCreate(axMapControl1.Object);
            //axMapControl1.CurrentTool = pCommand as ITool;
            IMap Cmap = this.axMapControl1.Map;
            ILayer CLayer = getLayerByName(Cmap, toolStripComboBox2.Text);

            IEnumFeature enumFeature = Cmap.FeatureSelection as IEnumFeature;
            IFeature CFeature = enumFeature.Next();
            IPolyline CutPolyline = CFeature.Shape as IPolyline;

            ICommand pCommand = new CutFeature(CLayer, Cmap, CutPolyline);
            pCommand.OnCreate(axMapControl1.Object);
            axMapControl1.CurrentTool = pCommand as ITool;

        }
        private void extend_line_Click(object sender, EventArgs e)
        {
            //extend(axMapControl1.Map);
            
            GetIntersectionPoint(flaglayer,axMapControl1.Map);
            
        }

        

        private void Create_Buffer_Click(object sender, EventArgs e)
        {
            CreateBuffer(axMapControl1.Map, flaglayer);
        }

        
    }
}
