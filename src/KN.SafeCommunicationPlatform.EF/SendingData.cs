using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KN.SafeCommunicationPlatform.EF
{
    /// <summary>
    /// 发送队列
    /// </summary>
    [Table(nameof(SendingData))]
    public class SendingData:BaseData
    {

        public ReceiveData ToReceiveData()
        {
            return new ReceiveData
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
