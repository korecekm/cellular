using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cellular.Model
{
    public class Rule
    {
        public List<Condition> Conditions { get; set; }
        public CellColor ResultColor { get; set; }

        public Rule(CellColor resultColor)
        {
            this.Conditions = new List<Condition>();
            this.ResultColor = resultColor;
        }
    }
}
