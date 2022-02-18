using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KN.SafeCommunicationPlatform.EF
{
    public abstract class BaseData
    {
        /// <summary>
        /// 自增Id
        /// </summary>
        public long Id { get; set; }



        public string? Db { get; set; }

        public string? Schema { get; set; }

        public string? Table { get; set; }

        /// <summary>
        /// 数据类型 0 文本  1 二进制
        /// </summary>
        public DataType DataType { get; set; }

        /// <summary>
        /// 文本数据
        /// </summary>
        public string? StringData { get; set; }

        /// <summary>
        /// 二进制数据
        /// </summary>
        public byte[]? BinaryData { get; set; }

        /// <summary>
        /// 是否已处理
        /// </summary>
        public bool Processed { get; set; }

        public DateTimeOffset? ProcessingTime { get; set; }

        /// <summary>
        /// 处理时间
        /// </summary>
        public DateTimeOffset? ProcessTime { get; set; }

        /// <summary>
        /// 添加时间
        /// </summary>
        public DateTimeOffset AddTime { get; set; } = DateTimeOffset.Now;

        public bool IsProcessing(TimeSpan offset)
        {
            if (this.ProcessingTime.HasValue)
            {
                return DateTimeOffset.Now - this.ProcessingTime.Value <= offset;
            }
            return false;
        }
    }
}
