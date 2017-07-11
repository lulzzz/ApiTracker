using Elasticsearch.Net;
using System;
using System.Configuration;

namespace ApiTracker
{
    /// <summary>
    /// ElasticClient
    /// </summary>
    public class ElasticClient
    {
        private ElasticLowLevelClient client { get; set; }

        private string IndexName { get; set; }

        /// <summary>
        /// 默认读取webconfig.AppSettings.ElasticConnection
        /// </summary>
        public ElasticClient() : this(ConfigurationManager.AppSettings["ElasticConnection"])
        {
        }

        public ElasticClient(string url)
        {
            var config = new ConnectionConfiguration(new Uri(url));

            client = new ElasticLowLevelClient(config);

            IndexName = GetType().Assembly.GetName().Name.ToLower();
        }

        /// <summary>
        /// 索引文档
        /// </summary>
        /// <param name="document">document</param>
        /// <returns></returns>
        public int Index(string document)
        {
            var result = client.Index<object>(IndexName,
                "apitracker",
                new PostData<Object>(document));

            return result.HttpStatusCode.GetValueOrDefault();
        }
    }
}
