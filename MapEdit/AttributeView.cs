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
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;

namespace MapEdit
{
    public partial class AttributeView : Form
    {
       //private ILayer  CurLayer;
       //public ILayer p_axMapControl
       // {
       //     get { return CurLayer; }
       // }
       //public AttributeView(ILayer m_Layer)
       // {
       //     CurLayer = m_Layer;
       // }
         
        public AttributeView()
        {
            InitializeComponent();
            
        }

        public int RemoveNumber;
        public string selectcolums;
        public string selectrows;
        public object changedValue;
        public int flag = 0;
        public Form1 fff;

        public ILayer LLL;


        //private ArrayList aa;
        /// <summary>
        /// 根据图层字段创建一个只含字段的空DataTable
        /// </summary>
        /// <param name="pLayer"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        private static DataTable CreateDataTableByLayer(ILayer pLayer, string tableName)
        {
            //创建一个DataTable表
            DataTable pDataTable = new DataTable(tableName);
            //取得ITable接口
            ITable pTable = pLayer as ITable;
            IField pField = null;
            DataColumn pDataColumn;
            //根据每个字段的属性建立DataColumn对象
            for (int i = 0; i < pTable.Fields.FieldCount; i++)
            {
                pField = pTable.Fields.get_Field(i);
                //新建一个DataColumn并设置其属性
                pDataColumn = new DataColumn(pField.Name);
                if (pField.Name == pTable.OIDFieldName)
                {
                    pDataColumn.Unique = true;//字段值是否唯一
                }
                //字段值是否允许为空
                pDataColumn.AllowDBNull = pField.IsNullable;
                //字段别名
                pDataColumn.Caption = pField.AliasName;
                //字段数据类型
                pDataColumn.DataType = System.Type.GetType(ParseFieldType(pField.Type));
                //字段默认值
                pDataColumn.DefaultValue = pField.DefaultValue;
                //当字段为String类型是设置字段长度
                //if (pField.VarType==8)
                //{
                //    pDataColumn.MaxLength = pField.Length;
                //}
                //字段添加到表中
                pDataTable.Columns.Add(pDataColumn);
                pField = null;
                pDataColumn = null;
            }
            return pDataTable;
        }
        /// <summary>
        /// 将GeoDatabase字段类型转换成.Net相应的数据类型
        /// </summary>
        /// <param name="fieldType">字段类型</param>
        /// <returns></returns>
        public static string ParseFieldType(esriFieldType fieldType)
        {
            switch (fieldType)
            {
                case esriFieldType.esriFieldTypeBlob:
                    return "System.String";
                case esriFieldType.esriFieldTypeDate:
                    return "System.DateTime";
                case esriFieldType.esriFieldTypeDouble:
                    return "System.Double";
                case esriFieldType.esriFieldTypeGeometry:
                    return "System.String";
                case esriFieldType.esriFieldTypeGlobalID:
                    return "System.String";
                case esriFieldType.esriFieldTypeGUID:
                    return "System.String";
                case esriFieldType.esriFieldTypeInteger:
                    return "System.Int32";
                case esriFieldType.esriFieldTypeOID:
                    return "System.String";
                case esriFieldType.esriFieldTypeRaster:
                    return "System.String";
                case esriFieldType.esriFieldTypeSingle:
                    return "System.Single";
                case esriFieldType.esriFieldTypeSmallInteger:
                    return "System.Int32";
                case esriFieldType.esriFieldTypeString:
                    return "System.String";
                default:
                    return "System.String";
            }
        }
        /// <summary>
        /// 填充DataTable中的数据
        /// </summary>
        /// <param name="pLayer"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static DataTable CreateDataTable(ILayer pLayer, string tableName)
        {
            //创建空DataTable
            DataTable pDataTable = CreateDataTableByLayer(pLayer, tableName);
            //DataTable dt2 = pDataTable.Clone();
            //取得图层类型
            string shapeType = getShapeType(pLayer);
            //创建DataTable的行对象
            DataRow pDataRow = null;
            //从ILayer查询到ITable
            ITable pTable = pLayer as ITable;
            ICursor pCursor = pTable.Search(null, false);
            
            //取得ITable中的行信息
            IRow pRow = pCursor.NextRow();
            //int n = 0;
            int cout = pRow.Fields.FieldCount;
            while (pRow != null)
            {
                //新建DataTable的行对象
                pDataRow = pDataTable.NewRow();
                for (int i = 0; i <cout; i++)
                {
                    //如果字段类型为esriFieldTypeGeometry，则根据图层类型设置字段值
                    if (pRow.Fields.get_Field(i).Type == esriFieldType.esriFieldTypeGeometry)
                    {
                        pDataRow[i] = shapeType;
                    }
                    //当图层类型为Anotation时，要素类中会有esriFieldTypeBlob类型的数据，
                    //其存储的是标注内容，如此情况需将对应的字段值设置为Element
                    //else if (pRow.Fields.get_Field(i).Type == esriFieldType.esriFieldTypeBlob)
                    //{
                    //    pDataRow[i] = "Element";
                    //}
                    else
                    {
                        pDataRow[i] = pRow.get_Value(i);
                    }
                }

                //添加DataRow到DataTable
                
                 pDataTable.Rows.Add(pDataRow);
                
                pDataRow = null;
                //n++;
                //为保证效率，一次只装载最多条记录
                //if (n == 2000)
                //{
                //    pRow = null;
                //}
                //else
                //{
                pRow = pCursor.NextRow();
                //}

                
            }
            return pDataTable;
        }
        
        /// <summary>
        /// 获得图层的Shape类型
        /// </summary>
        /// <param name="pLayer">图层</param>
        /// <returns></returns>
        public static string getShapeType(ILayer pLayer)
        {
            IFeatureLayer pFeatLyr = (IFeatureLayer)pLayer;
            switch (pFeatLyr.FeatureClass.ShapeType)
            {
                case esriGeometryType.esriGeometryPoint:
                    return "Point";
                case esriGeometryType.esriGeometryPolyline:
                    return "Polyline";
                case esriGeometryType.esriGeometryPolygon:
                    return "Polygon";
                default:
                    return "";
            }
        }
        public DataTable attributeTable;

        /// <summary>
        /// 绑定DataTable到DataGridView
        /// </summary>
        /// <param name="player"></param>
        public void CreateAttributeTable(ILayer player)
        {
            string tableName;
            LLL = player;
            tableName = getValidFeatureClassName(player.Name);
            attributeTable = CreateDataTable(player, tableName);

            //desc(CreateDataTable(player, tableName));
            //desc(attributeTable);
            this.dataGridView1.DataSource = attributeTable;
            
            this.Text = "属性表[" + tableName + "]  " + "记录数：" + attributeTable.Rows.Count.ToString();
        }

        //public static DataTable desc(DataTable dt1)
        //{
        //    DataTable dt2 = dt1.Clone();
        //    dt2.Clear();
        //    DataRow[] drs = dt1.Select("", "FID DESC");
        //    for (int i = 0; i <= drs.Length-1; i++)
        //    {
        //        dt2.Rows.Add(drs[i].ItemArray); 
        //    }
        //    return dt2;
        //}

        #region 从大到小排序
        /// <summary>
        /// 从大到小排序
        /// </summary>
        /// <param name="dt1"></param>
        /// <returns>排序结果</returns>
        public static DataTable desc(DataTable dt1)
        {
            DataTable dt2 = dt1.Copy();
            dt2.Clear();
            DataRow ad;

            int MAX = 0;
            int pCount = dt1.Rows.Count;
            int[] aa = new int[pCount];

            for (int i = 0; i < pCount; i++)
            {
                aa[i] = Convert.ToInt32(dt1.Rows[i][0]);
            }

            for (int i = 0; i < pCount; i++)
            {
                MAX = GetMaxValue(aa);
                ad = dt1.Rows[MAX];
                //dt2.Rows.Add(ad);
                dt2.ImportRow(ad);
                aa = aa.Where(x => x != MAX).ToArray();

                //dt1.Rows.RemoveAt(MAX);
            }


            return dt2;
        }



        public static int GetMaxValue(int[] values)
        {
            int max = 0;
            int pmax = 0;
            for (int i = 0; i < values.Length; i++)
            {
                if (max < values[i])
                {
                    max = values[i];
                    pmax = i;
                }
            }
            return pmax;
        }

        #endregion


        #region 替换数据表名中的点
        /// <summary>
        /// 替换数据表名中的点
        /// </summary>
        /// <param name="FCname"></param>
        /// <returns></returns>
        public static string getValidFeatureClassName(string FCname)
        {
            int dot = FCname.IndexOf(".");
            if (dot != -1)
            {
                return FCname.Replace(".", "_");
            }
            return FCname;
        }
        #endregion


        private void AttributeView_Load(object sender, EventArgs e)
        {
            
            //this.dataGridView1.Sort(this.dataGridView1.Columns["FID"], System.ComponentModel.ListSortDirection.Descending);

            //DataView dv = attributeTable.DefaultView;
            //dv.Sort = "FID DESC";
            //attributeTable.Columns.Add("FID", Type.GetType("System.Int16"));
        }

        private void dataGridView1_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button==MouseButtons.Left)
            {
                if (e.RowIndex >= 0)
                {
                    //若行已是选中状态就不再进行设置
                    if (dataGridView1.Rows[e.RowIndex].Selected == false)
                    {
                        dataGridView1.ClearSelection();
                        dataGridView1.Rows[e.RowIndex].Selected = true;
                    }
                    //只选中一行时设置活动单元格
                    if (dataGridView1.SelectedRows.Count == 1)
                    {
                        dataGridView1.CurrentCell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
                       
                    }
                    //弹出操作菜单
                    selectcolums = dataGridView1.Columns[dataGridView1.CurrentCell.ColumnIndex].HeaderText.ToString();
                    int a=dataGridView1.CurrentRow.Index;
                    selectrows=dataGridView1.Rows[a].Cells[selectcolums].Value.ToString();
                    //contextMenuStrip1.Show(MousePosition.X, MousePosition.Y);
                }
            }
            
        }

        private void Edit_Item_Click(object sender, EventArgs e)
        {
            //dataGridView1.BeginEdit(true);
            //Update(LLL, selectcolums, changedValue);
        }

        public void Update(string strFieldName, object dblValue)
        {

            //ILayer CurLayer = LLL;
            //IFeatureLayer CurFeatureLayer = fff.flaglayer as IFeatureLayer;
            //IFeatureClass CurFeatureClass = fff.mfeaturelayer.FeatureClass;
            //IDataset CurData = fff.mfeatureclass as IDataset;
            //IWorkspace CurWorkspace = CurData.Workspace;
            //IWorkspaceEdit CurWorkspaceEdit = CurWorkspace as IWorkspaceEdit;

            int nIndex = fff.mfeatureclass.FindField(strFieldName);
            IField pField = fff.mfeatureclass.Fields.get_Field(nIndex);//获取列

            IQueryFilter pFilter = new QueryFilter();
            if (pField.Type == esriFieldType.esriFieldTypeString)
            {
                pFilter.WhereClause = selectcolums + "='" + selectrows + "'";
                //获取项
            }
            else
            {
                pFilter.WhereClause = selectcolums + "=" + selectrows;//获取项
            }
            IFeatureCursor pFeatureCursor = fff.mfeatureclass.Search(pFilter, false);
            
            IFeature pFeature = pFeatureCursor.NextFeature();

            //fff.CurWorkspaceEdit.StartEditing(true);
            fff.CurWorkspaceEdit.StartEditOperation();

            pFeature.set_Value(nIndex, dblValue);
            pFeature.Store();
            flag = 1;
            fff.CurWorkspaceEdit.StopEditOperation();
            //System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeature);
            //pFeature = null;
            //pFeature = pFeatureCursor.NextFeature();


            //pFeatureCursor.Flush();
            //System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatureCursor);
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            
                selectcolums = dataGridView1.Columns[dataGridView1.CurrentCell.ColumnIndex].HeaderText.ToString();
                int a = dataGridView1.CurrentRow.Index;

                changedValue = dataGridView1.Rows[a].Cells[selectcolums].Value;
                Update(selectcolums, changedValue);
               
        }
        public void saveedit(ILayer CurLayer)
        {
            IFeatureLayer CurFeatureLayer = CurLayer as IFeatureLayer;
            IFeatureClass CurFeatureClass = CurFeatureLayer.FeatureClass;
            IDataset CurData = CurFeatureClass as IDataset;
            IWorkspace CurWorkspace = CurData.Workspace;
            IWorkspaceEdit CurWorkspaceEdit = CurWorkspace as IWorkspaceEdit;

            if (MessageBox.Show("是否保存", "保存提示", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                CurWorkspaceEdit.StopEditOperation();
                CurWorkspaceEdit.StopEditing(true);
            }
            else
            {
                CurWorkspaceEdit.StopEditOperation();
                CurWorkspaceEdit.StopEditing(false);
            }
            
        }
    }
}
