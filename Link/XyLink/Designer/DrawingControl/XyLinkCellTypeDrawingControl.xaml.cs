using GrapeCity.Forguncy.CellTypes;
using System.Windows.Controls;

namespace XyLink.Designer.DrawingControl
{
    public partial class XyLinkCellTypeDrawingControl : UserControl
    {
        public XyLinkCellTypeDrawingControl(XyLinkCellType cellType, ICellInfo cellInfo, IDrawingHelper drawingHelper)
        {
            this.DataContext = new XyLinkCellTypeDrawingControlViewModel(cellType, cellInfo, drawingHelper);

            InitializeComponent();
        }

        public class XyLinkCellTypeDrawingControlViewModel
        {
            XyLinkCellType _cellType;
            ICellInfo _cellInfo;
            IDrawingHelper _drawingHelper;
            public XyLinkCellTypeDrawingControlViewModel(XyLinkCellType cellType, ICellInfo cellInfo, IDrawingHelper drawingHelper)
            {
                _cellType = cellType;
                _cellInfo = cellInfo;
                _drawingHelper = drawingHelper;
            }
            public string Text { get => _cellType.ToString(); }
        }
    }
}
