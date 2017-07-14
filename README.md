# ApiTracker 

[![Build status](https://ci.appveyor.com/api/projects/status/o72ved788wpbabvk?svg=true)](https://ci.appveyor.com/project/seven1986/apitracker)

PM> Install-Package ApiTracker


web.config 配置
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <appSettings>
    <add key="ElasticConnection" value="http://127.0.0.1:9200" />
  </appSettings>
</configuration>
```



Web API Controller 配置

```csharp
using ApiTracker;

// ApiTracker的indexName如果不填，默认为：apitracker
// ApiTrackerOptions 属性可以定制Log的字段
[ApiTracker("WebApplication")]
 public class ValuesController : ApiController
 {
    // GET api/values
    public IEnumerable<string> Get()
    {
       return new string[] { "value1", "value2" };
    }
}
```


