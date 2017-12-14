using System;
using System.Collections.Generic;
using System.Net.Http;
using DatabaseSupport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TestSupport;
using Timesheets;
using Xunit;
using static TestSupport.TestServers;

namespace TimesheetsTest
{
    [Collection("Timesheets")]
    public class TimeEntryControllerTest
    {
        private readonly DataSourceConfig _dataSourceConfig = new DataSourceConfig();
        private readonly TestDatabaseSupport _support = new TestDatabaseSupport();

        private readonly HttpClient _client = ActiveProjectServer.CreateClient();

        static TimeEntryControllerTest() => TestEnvSupport.SetTimesheetsVcap();
        public TimeEntryControllerTest() => _support.TruncateAllTables();

        private const string _sql = @"insert into time_entries (id, project_id, user_id, date, hours)
        values (1534, 55432, 4765, '2015-05-17', 5), (2534, 55432, 4765, '2015-05-18', 3);";

        [Fact]
        public void TestPost()
        {
            _support.ExecSql(_sql);

            var controller =
                new TimeEntryController(new TimeEntryDataGateway(new DatabaseTemplate(_dataSourceConfig)),
                    new ProjectClient(_client, new LoggerFactory().CreateLogger<ProjectClient>()));

            var value = controller.Post(new TimeEntryInfo(-1, 55432, 4765, DateTime.Parse("2015-05-17"), 8, ""));
            var actual = (TimeEntryInfo) ((ObjectResult) value).Value;

            Assert.True(actual.Id > 0);
            Assert.Equal(55432, actual.ProjectId);
            Assert.Equal(4765, actual.UserId);
            Assert.Equal(17, actual.Date.Day);
            Assert.Equal(8, actual.Hours);
            Assert.Equal("entry info", actual.Info);
        }

        [Fact]
        public void TestGet()
        {
            _support.ExecSql(_sql);

            var controller =
                new TimeEntryController(new TimeEntryDataGateway(new DatabaseTemplate(_dataSourceConfig)),
                    new ProjectClient(_client, new LoggerFactory().CreateLogger<ProjectClient>()));
            var result = controller.Get(4765);

            // todo...
            Assert.Equal(2, ((List<TimeEntryInfo>) ((ObjectResult) result).Value).Count);
        }
    }
}