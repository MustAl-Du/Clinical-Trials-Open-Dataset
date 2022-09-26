using Parquet.Data;
using System.Collections;

namespace XmlToParquet
{
    internal class ColumnInfo
    {
        public DataField Field { get; }
        public IList Data { get; }

        private ColumnInfo(DataField field, IList data)
        {
            this.Field = field;
            this.Data = data;
        }

        public static ColumnInfo Create<T>(string name)
        {
            return new ColumnInfo(
                new DataField<T>(name),
                new List<T>());
        }
    }
}
