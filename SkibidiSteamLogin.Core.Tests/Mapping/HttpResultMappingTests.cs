using System.Net;
using SkibidiSteamLogin.Core.Mapping;

namespace SkibidiSteamLogin.Core.Tests.Mapping
{
    public class HttpResultMappingTests
    {
        [Theory]
        [InlineData(HttpStatusCode.OK, true)]
        [InlineData(HttpStatusCode.Created, true)]
        [InlineData(HttpStatusCode.NoContent, true)]
        [InlineData(HttpStatusCode.BadRequest, false)]
        [InlineData(HttpStatusCode.Unauthorized, false)]
        [InlineData(HttpStatusCode.Forbidden, false)]
        [InlineData(HttpStatusCode.NotFound, false)]
        [InlineData(HttpStatusCode.InternalServerError, false)]
        [InlineData(HttpStatusCode.ServiceUnavailable, false)]
        public void ToHttpResult_VariousStatusCodes_MapsIsSuccessCorrectly(HttpStatusCode statusCode, bool expectedSuccess)
        {
            var response = new HttpResponseMessage(statusCode);

            var result = response.ToHttpResult();

            Assert.Equal(expectedSuccess, result.IsSuccess);
            Assert.Equal(statusCode, result.StatusCode);
        }

        [Fact]
        public void ToHttpDataResult_WithData_MapsCorrectly()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var data = "test data";

            var result = response.ToHttpDataResult(data);

            Assert.True(result.IsSuccess);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal(data, result.Data);
        }

        [Fact]
        public void ToHttpDataResult_WithNullData_MapsCorrectly()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);

            var result = response.ToHttpDataResult<string>(null);

            Assert.True(result.IsSuccess);
            Assert.Null(result.Data);
        }

        [Fact]
        public void ToHttpDataResult_WithDefaultData_DataIsNull()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);

            var result = response.ToHttpDataResult<string>();

            Assert.True(result.IsSuccess);
            Assert.Null(result.Data);
        }

        [Fact]
        public void ToHttpDataResult_FailureResponse_MapsStatusAndData()
        {
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            var data = "error info";

            var result = response.ToHttpDataResult(data);

            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.Equal(data, result.Data);
        }

        [Fact]
        public void ToHttpDataResult_WithComplexObject_PreservesReference()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var data = new List<string> { "a", "b", "c" };

            var result = response.ToHttpDataResult(data);

            Assert.Same(data, result.Data);
        }
    }
}
