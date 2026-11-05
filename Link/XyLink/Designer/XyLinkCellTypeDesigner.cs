using XyLink.Designer.DrawingControl;
using GrapeCity.Forguncy.CellTypes;
using System.Windows;

namespace XyLink.Designer
{
    public class XyLinkCellTypeDesigner : CellTypeDesigner<XyLinkCellType>
    {
        public override FrameworkElement GetDrawingControl(ICellInfo cellInfo, IDrawingHelper drawingHelper)
        {
            return new XyLinkCellTypeDrawingControl(this.CellType, cellInfo, drawingHelper);
        }
    }
}
