using Xunit;
using aggregator.cli;
using System;

namespace unittests_core
{
    public class GitHubVersionResponseTests
    {
        [Fact]
        public void GivenAGitHubVersionResponse_WhenCachingTheResponse_ThenACacheFile_ShouldBeSaved()
        {
            var responseToCache = new GitHubVersionResponse() 
            { 
                Name = "test", 
                ResponseDate = DateTime.Now, 
                Tag = "tag", 
                Url = "url", 
                When = new DateTimeOffset(DateTime.Now) 
            };

            responseToCache.SaveCache();
            var cache = GitHubVersionResponse.TryReadFromCache();
            var cacheIsEqual = (cache != null &&
                cache.Name == responseToCache.Name &&
                cache.ResponseDate == responseToCache.ResponseDate &&
                cache.Tag == responseToCache.Tag &&
                cache.Url == responseToCache.Url &&
                cache.When == responseToCache.When);
            Assert.True(cacheIsEqual);
            GitHubVersionResponse.ClearCache();
        }

        [Fact]
        public void GivenNoCacheFileExists_WhenAttemptingToReadACacheFrom_ThenANullObject_ShouldBeReturned()
        {
            if (System.IO.File.Exists(GitHubVersionResponse.CacheFileName))
                GitHubVersionResponse.ClearCache();

            var cache = GitHubVersionResponse.TryReadFromCache();
            Assert.Null(cache);
        }

        [Fact]
        public void GivenAGitHubVersionResponseCached_WhenClearingTheCache_ThenTheCacheFile_ShouldBeRemoved()
        {
            var responseToCache = new GitHubVersionResponse()
            {
                Name = "test",
                ResponseDate = DateTime.Now,
                Tag = "tag",
                Url = "url",
                When = new DateTimeOffset(DateTime.Now)
            };

            responseToCache.SaveCache();
            Assert.True(System.IO.File.Exists(GitHubVersionResponse.CacheFileName));
            GitHubVersionResponse.ClearCache();
            Assert.False(System.IO.File.Exists(GitHubVersionResponse.CacheFileName));
        }

        [Fact]
        public void GivenAGitHubVersionResponse_WhenReusedWithADay_ThenTheResponse_ShouldBeConsideredValid()
        {
            var responseToCache = new GitHubVersionResponse() { Name = "test", ResponseDate = DateTime.Now, Tag = "tag", Url = "url", When = new DateTimeOffset(DateTime.Now) };
            Assert.True(responseToCache.CacheIsInDate());
        }

        [Fact]
        public void GivenAGitHubVersionResponse_WhenReusedAfterADay_ThenTheResponse_ShouldBeConsideredInvalid()
        {
            var responseToCache = new GitHubVersionResponse() { Name = "test", ResponseDate = DateTime.Now.Subtract(new TimeSpan(7, 0, 0, 0)), Tag = "tag", Url = "url", When = new DateTimeOffset(DateTime.Now) };
            Assert.False(responseToCache.CacheIsInDate());
        }
    }
}
