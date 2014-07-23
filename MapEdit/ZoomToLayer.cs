using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.ADF.BaseClasses;

namespace MapEdit
{
    class ZoomToLayer:BaseCommand
    {
        //定义指针   
        private IMapControl3 pMapControl;
        public ZoomToLayer()
        {
            base.m_caption = "放大至该图层";
            
        }
        //重写BaseCommand基类的虚拟方法OnClick()   
        public override void OnClick()
        {
            ILayer pLayer = (ILayer)pMapControl.CustomProperty;
            pMapControl.Extent = pLayer.AreaOfInterest;
        }
        //重写BaseCommand基类的抽象方法OnCreate(object hook)   
        public override void OnCreate(object hook)
        {
            pMapControl = (IMapControl3)hook;
        }  
    }
}
