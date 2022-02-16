using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KN.SafeCommunicationPlatform.EF
{
    /// <summary>
    /// 接收队列
    /// </summary>
    [Table(nameof(SendingData))]
    public class ReceiveData :BaseData
    {
       
        public SendingData ToSendingData()
        {
            return new SendingData
            {
                Id = 0,
                Db = Db,
                Schema = Schema,
                Table = Table,
                DataType = DataType,
                StringData = StringData,
                BinaryData = BinaryData,
                AddTime = AddTime,
                Processed = false,
                Processing = false,
                ProcessTime = null
            };
        }
    }
}
