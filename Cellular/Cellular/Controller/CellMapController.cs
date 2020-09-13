using Cellular.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cellular.Controller
{
    /// <summary>
    /// Serves as a tool for computing, in a multithreaded way, the next cell
    /// colors in each square of the grid.
    /// </summary>
    class CellMapController
    {
        public List<CellColor> colors;

        private int rowCount, collumnCount;
        bool firstMap = true;
        private List<List<bool>>[] cellsChanged = new List<List<bool>>[2];
        private List<List<int>>[] map = new List<List<int>>[2];
        public List<List<int>> CurrentMap => firstMap ? map[0] : map[1];
        public List<List<bool>> CurrentChanges => firstMap ? cellsChanged[0] : cellsChanged[1];
        public List<List<int>> NextMap => !firstMap ? map[0] : map[1];
        public List<List<bool>> NextChanges => !firstMap ? cellsChanged[0] : cellsChanged[1];

        private int threadCount;

        public CellMapController(int rowCount, int collumnCount)
        {
            this.rowCount = rowCount;
            this.collumnCount = collumnCount;

            threadCount = Environment.ProcessorCount;
        }

        public void SwitchMap() => firstMap = !firstMap;

        public void Init()
        {
            map[0] = new List<List<int>>(rowCount);
            map[1] = new List<List<int>>(rowCount);
            cellsChanged[0] = new List<List<bool>>(rowCount);
            cellsChanged[1] = new List<List<bool>>(rowCount);
            for (int row = 0; row < rowCount; ++row)
            {
                map[0].Add(new List<int>(collumnCount));
                map[1].Add(new List<int>(collumnCount));
                cellsChanged[0].Add(new List<bool>(collumnCount));
                cellsChanged[1].Add(new List<bool>(collumnCount));
                for (int col = 0; col < collumnCount; ++col)
                {
                    map[0][row].Add(0);
                    map[1][row].Add(0);
                    cellsChanged[0][row].Add(false);
                    cellsChanged[1][row].Add(false);
                }
            }
        }

        public Task ComputeNext()
        {
            var task = new Task(
                () => ComputeNextParallel((threadCount < rowCount) ? threadCount : rowCount, rowCount / threadCount, 0, rowCount));
            task.Start();
            return task;
        }

        /// <summary>
        /// Parallelizes the computation of NextMap and NextChanges by separating rows into
        /// chunks of 'chunkSize' rows and utilizing 'threadCount' threads.
        /// </summary>
        /// <param name="firstRow">
        /// The least row index that should be considered in the computation.
        /// </param>
        /// <param name="limitRow">
        /// The first row index NOT to be considered.
        /// </param>
        private void ComputeNextParallel(int threadCount, int chunkSize, int firstRow, int limitRow)
        {
            if (threadCount > 1)
            {
                int m = firstRow + (threadCount/2) * chunkSize;
                var rightTask = new Task(
                    () => ComputeNextParallel(threadCount / 2, chunkSize, m, limitRow));
                rightTask.Start();
                ComputeNextParallel((threadCount / 2) + (threadCount % 2), chunkSize, firstRow, m);
                rightTask.Wait();
            } else
            {
                for (int r = firstRow; r < limitRow; ++r)
                {
                    for (int c = 0; c < collumnCount; ++c)
                    {
                        NextMap[r][c] = NextColorAt(c, r);
                        NextChanges[r][c] = (NextMap[r][c] != CurrentMap[r][c]);
                    }
                }
            }
        }

        private int NextColorAt(int x, int y)
        {
            var color = colors[CurrentMap[y][x]];
            var rules = color.Rules;
            List<CellColor> cellColors = new List<CellColor>();
            GetRelevantCellColors(x, y, color.RelevantCells, ref cellColors);

            foreach(var rule in rules)
            {
                bool conditionsMet = true;
                foreach (var condition in rule.Conditions)
                {
                    if (!condition.ConditionMet(cellColors))
                    {
                        conditionsMet = false;
                        break;
                    }
                }
                if (!conditionsMet) continue;
                return rule.ResultColor.Index;
            }
            return CurrentMap[y][x];
        }

        private void GetRelevantCellColors(int x, int y, List<Location> cells, ref List<CellColor> colors)
        {
            foreach (var cell in cells)
            {
                int pX = x + cell.diffX;
                int pY = y + cell.diffY;
                if (pX < 0 || pX >= collumnCount || pY < 0 || pY >= rowCount)
                    colors.Add(this.colors[0]);
                else
                    colors.Add(this.colors[CurrentMap[pY][pX]]);
            }
        }
    }
}
