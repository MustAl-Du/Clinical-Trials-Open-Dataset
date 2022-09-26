using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XmlToParquet
{
    internal class ParentData
    {
        public int ParentId { get; }
        public int  StartIndex { get; }
        public ColumnInfo ColumnInfo { get; }

        public ParentData(int parentId, int startIndex, ColumnInfo columnInfo)
        {
            ParentId = parentId;
            StartIndex = startIndex;
            ColumnInfo = columnInfo;
        }
    }
}
