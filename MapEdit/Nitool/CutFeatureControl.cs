using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ESRI.ArcGIS.ADF.BaseClasses;
using ESRI.ArcGIS.ADF.CATIDs;
using ESRI.ArcGIS.Editor;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.Geometry;

namespace MapEdit.Nitool
{
    /// <summary>
    /// Summary description for CutFeatureControl.
    /// </summary>
    [Guid("c26f2e96-5ba1-47ca-a575-2810f01be82b")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("MapEdit.Nitool.CutFeatureControl")]
    public sealed class CutFeatureControl : BaseTool, IShapeConstructorTool, ISketchTool
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
        private IEditor3 m_editor;
        private IEditEvents_Event m_editEvents;
        private IEditEvents5_Event m_editEvents5;
        private IEditSketch3 m_edSketch;
        private IShapeConstructor m_csc;


        public CutFeatureControl()
        {
            //
            // TODO: Define values for the public properties
            //
            base.m_category = ""; //localizable text 
            base.m_caption = "";  //localizable text 
            base.m_message = "";  //localizable text
            base.m_toolTip = "";  //localizable text
            base.m_name = "";   //unique id, non-localizable (e.g. "MyCategory_ArcMapTool")
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

        /// <summary>
        /// Occurs when this tool is created
        /// </summary>
        /// <param name="hook">Instance of the application</param>
        public override void OnCreate(object hook)
        {
            m_application = hook as IApplication;

            //get the editor
            UID editorUid = new UID();
            editorUid.Value = "esriEditor.Editor";
            m_editor = m_application.FindExtensionByCLSID(editorUid) as IEditor3;
            m_editEvents = m_editor as IEditEvents_Event;
            m_editEvents5 = m_editor as IEditEvents5_Event;
        }

        /// <summary>
        /// Criteria for enabling the tool
        /// </summary>
        public override bool Enabled
        {
            // Enable the tool while editing
            get { return (m_editor.EditState == esriEditState.esriStateEditing); }
        }

        /// <summary>
        /// Occurs when this tool is clicked
        /// </summary>
        public override void OnClick()
        {
            m_edSketch = m_editor as IEditSketch3;

            // Activate a shape constructor based on the current sketch geometry
            // Sketch geometry can be set either by the user selecting a template or the developer setting a current layer
            // and/or sketch geometry
            if (m_edSketch.GeometryType == esriGeometryType.esriGeometryPoint)
                m_csc = new PointConstructorClass();
            else
                m_csc = new StraightConstructorClass();
            m_csc.Initialize(m_editor);
            m_edSketch.ShapeConstructor = m_csc;
            m_csc.Activate();

            // Set the current task to null
            m_editor.CurrentTask = null;

            // Setup events
            m_editEvents.OnSketchModified += new IEditEvents_OnSketchModifiedEventHandler(m_editEvents_OnSketchModified);
            m_editEvents5.OnShapeConstructorChanged += new IEditEvents5_OnShapeConstructorChangedEventHandler(m_editEvents5_OnShapeConstructorChanged);
            m_editEvents.OnSketchFinished += new IEditEvents_OnSketchFinishedEventHandler(m_editEvents_OnSketchFinished);
        }

        void m_editEvents_OnSketchFinished()
        {
            // Send a shift-tab to hide the construction toolbar
            OnKeyDown(9, 1);

            // TODO: Add developer code here
        }

        #region Other Events
        private void m_editEvents_OnSketchModified()
        {
            m_csc.SketchModified();
        }

        private void m_editEvents5_OnShapeConstructorChanged()
        {
            // Activate new constructor
            m_csc.Deactivate();
            m_csc = null;
            m_csc = m_edSketch.ShapeConstructor;
            m_csc.Activate();
        }
        #endregion

        #region ISketchTool Members - Pass to shape constructor

        public void AddPoint(IPoint point, bool Clone, bool allowUndo)
        {
            m_csc.AddPoint(point, Clone, allowUndo);
        }

        public IPoint Anchor
        {
            get { return m_csc.Anchor; }
        }

        public double AngleConstraint
        {
            get { return m_csc.AngleConstraint; }
            set { m_csc.AngleConstraint = value; }
        }

        public esriSketchConstraint Constraint
        {
            get { return m_csc.Constraint; }
            set { m_csc.Constraint = value; }
        }

        public double DistanceConstraint
        {
            get { return m_csc.DistanceConstraint; }
            set { m_csc.DistanceConstraint = value; }
        }

        public bool IsStreaming
        {
            get { return m_csc.IsStreaming; }
            set { m_csc.IsStreaming = value; }
        }

        public IPoint Location
        {
            get { return m_csc.Location; }
        }

        #endregion

        #region Tool overriding members - Pass to shape constructor
        public override void OnMouseDown(int Button, int Shift, int X, int Y)
        {
            m_csc.OnMouseDown(Button, Shift, X, Y);
        }

        public override void OnMouseMove(int Button, int Shift, int X, int Y)
        {
            m_csc.OnMouseMove(Button, Shift, X, Y);
        }

        public override void OnMouseUp(int Button, int Shift, int X, int Y)
        {
            m_csc.OnMouseUp(Button, Shift, X, Y);
        }

        public override bool OnContextMenu(int X, int Y)
        {
            return m_csc.OnContextMenu(X, Y);
        }

        public override void OnKeyDown(int keyCode, int Shift)
        {
            m_csc.OnKeyDown(keyCode, Shift);
        }

        public override void OnKeyUp(int keyCode, int Shift)
        {
            m_csc.OnKeyUp(keyCode, Shift);
        }

        public override void Refresh(int hDC)
        {
            m_csc.Refresh(hDC);
        }

        public override int Cursor
        {
            get { return m_csc.Cursor; }
        }

        public override void OnDblClick()
        {
            if (Control.ModifierKeys == Keys.Shift)
            {
                ISketchOperation pso = new SketchOperation();
                pso.MenuString_2 = "Finish Sketch Part";
                pso.Start(m_editor);
                m_edSketch.FinishSketchPart();
                pso.Finish(null);
            }
            else
                m_edSketch.FinishSketch();
        }

        public override bool Deactivate()
        {
            // Unsubscribe events
            m_editEvents.OnSketchModified -= m_editEvents_OnSketchModified;
            m_editEvents5.OnShapeConstructorChanged -= m_editEvents5_OnShapeConstructorChanged;
            m_editEvents.OnSketchFinished -= m_editEvents_OnSketchFinished;
            return base.Deactivate();
        }

        #endregion
    }
}
