using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Backlog;
using DatabaseSupport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TestSupport;
using Xunit;
using static TestSupport.TestServers;

namespace BacklogTest
{
    [Collection("Backlog")]
    public class StoryControllerTest
    {
        private readonly DataSourceConfig _dataSourceConfig = new DataSourceConfig();
        private readonly TestDatabaseSupport _support = new TestDatabaseSupport();

        private readonly HttpClient _client = ActiveProjectServer.CreateClient();

        static StoryControllerTest() => TestEnvSupport.SetBacklogVcap();
        public StoryControllerTest() => _support.TruncateAllTables();

        private const string _sql = @"insert into stories (id, project_id, name) values (876, 55432, 'An epic story'), 
            (877, 55432, 'Another epic story');";

        [Fact]
        public void TestPost()
        {
            _support.ExecSql(_sql);

            var controller =
                new StoryController(new StoryDataGateway(new DatabaseTemplate(_dataSourceConfig)),
                    new ProjectClient(_client, new LoggerFactory().CreateLogger<ProjectClient>(), () => Task.FromResult("anAccessToken")));

            var value = controller.Post(new StoryInfo(-1, 55432, "An epic story", ""));
            var actual = (StoryInfo) ((ObjectResult) value).Value;

            Assert.True(actual.Id > 0);
            Assert.Equal(55432, actual.ProjectId);
            Assert.Equal("An epic story", actual.Name);
            Assert.Equal("story info", actual.Info);
        }

        [Fact]
        public void TestGet()
        {
            _support.ExecSql(_sql);

            var controller =
                new StoryController(new StoryDataGateway(new DatabaseTemplate(_dataSourceConfig)),
                    new ProjectClient(_client, new LoggerFactory().CreateLogger<ProjectClient>(), () => Task.FromResult("anAccessToken")));
            var result = controller.Get(55432);

            // todo...
            Assert.Equal(2, ((List<StoryInfo>) ((ObjectResult) result).Value).Count);
        }
    }
}