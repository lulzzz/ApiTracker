using Elasticsearch.Net;
using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace ApiTracker
{
    /// <summary>
    /// ElasticClient
    /// </summary>
    public class ElasticClient
    {
        private ElasticLowLevelClient client { get; set; }

        private string IndexName { get; set; }

        private int Timeout { get; set; }

        /// <summary>
        /// 默认读取webconfig.AppSettings.ElasticConnection
        /// </summary>
        public ElasticClient(string indexName,int timeout) : this(indexName, timeout,ConfigurationManager.AppSettings["ElasticConnection"])
        {

        }

        public ElasticClient(string indexName,int timeout,string url)
        {
            var config = new ConnectionConfiguration(new Uri(url));

            client = new ElasticLowLevelClient(config);

            IndexName = indexName + "-" + DateTime.UtcNow.ToString("yyyyMMdd");

            Timeout = timeout;
        }

        /// <summary>
        /// 索引文档
        /// </summary>
        /// <param name="document">document</param>
        /// <returns></returns>
        public void Index(string document)
        {
            //记录日志异步操作时间
            var tokenSource = new CancellationTokenSource();

            Task.Run(new Action(() =>
            { 
                // 记录日志的超时时间
                var timeout = new TimeSpan(DateTime.UtcNow.AddMilliseconds(Timeout).Ticks);

                var result = client.Index<object>(IndexName,
                    "apitracker",
                    new PostData<Object>(document), (pms) =>
                    {
                        pms.Timeout(timeout);
                        return pms;
                    });

            }), tokenSource.Token);

            tokenSource.CancelAfter(1000);
        }
    }
}
