using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cellular.Model
{
    public abstract class Condition
    {
        public List<int> Squares { get; set; }
        public List<CellColor> ValidColors { get; set; }

        public abstract bool ConditionMet(List<CellColor> relevantCellColors);
    }

    /// <summary>
    /// Condition concerned with the specific color(s) (ie. kinds) in selected
    /// surrounding cells.
    /// </summary>
    public class KindCondition : Condition
    {
        public KindCondition(List<int> squares, List<CellColor> validColors)
        {
            this.Squares = squares;
            this.ValidColors= validColors;
        }
        public override bool ConditionMet(List<CellColor> relevantCellColors)
        {
            foreach (var cell in Squares)
            {
                bool isValid = false;
                foreach (var validColor in ValidColors)
                {
                    if (validColor.Equals(relevantCellColors[cell]))
                    {
                        isValid = true;
                        break;
                    }
                }
                if (!isValid)
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Condition concerned with the count of specified cells with certain
    /// color(s).
    /// </summary>
    public class CountCondition : Condition
    {
        public int TargetCount { get; set; }
        public Ordering Order { get; set; }

        public CountCondition(List<int> squares, List<CellColor> validColors, int targetCount, Ordering order)
        {
            this.Squares = squares;
            this.ValidColors = validColors;
            this.TargetCount = targetCount;
            this.Order = order;
        }
        public override bool ConditionMet(List<CellColor> relevantCellColors)
        {
            int count = 0;
            foreach (var cell in Squares)
            {
                foreach (var validColor in ValidColors)
                {
                    if (validColor.Equals(relevantCellColors[cell]))
                    {
                        ++count;
                        break;
                    }
                }
            }
            switch (Order)
            {
                case CountCondition.Ordering.EQUAL:
                    return (count == TargetCount);
                case CountCondition.Ordering.LESS:
                    return (count < TargetCount);
                case CountCondition.Ordering.GREATER:
                    return (count > TargetCount);
            }
            return false;
        }

        public enum Ordering
        {
            LESS, EQUAL, GREATER
        }

    }
}
