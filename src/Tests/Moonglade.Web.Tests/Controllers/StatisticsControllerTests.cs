﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Core;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    public class StatisticsControllerTests
    {
        private MockRepository _mockRepository;

        private Mock<IBlogStatistics> _mockBlogStatistics;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockBlogStatistics = _mockRepository.Create<IBlogStatistics>();
        }

        private StatisticsController CreateStatisticsController()
        {
            return new(_mockBlogStatistics.Object);
        }

        [Test]
        public async Task Get_EmptyGuid()
        {
            var ctl = CreateStatisticsController();
            var result = await ctl.Get(Guid.Empty);
            Assert.IsInstanceOf(typeof(BadRequestObjectResult), result);
        }

        [Test]
        public async Task Get_OK()
        {
            _mockBlogStatistics.Setup(p => p.GetStatisticAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult((996, 404)));

            var ctl = CreateStatisticsController();
            var result = await ctl.Get(Guid.Parse("76169567-6ff3-42c0-b163-a883ff2ac4fb"));

            Assert.IsInstanceOf(typeof(OkObjectResult), result);
        }

        [Test]
        public async Task Hit_EmptyGuid()
        {
            var ctl = CreateStatisticsController();
            var result = await ctl.Post(new() { PostId = Guid.Empty, IsLike = false });
            Assert.IsInstanceOf(typeof(BadRequestObjectResult), result);
        }

        [Test]
        public async Task Like_EmptyGuid()
        {
            var ctl = CreateStatisticsController();
            var result = await ctl.Post(new() { PostId = Guid.Empty, IsLike = true });
            Assert.IsInstanceOf(typeof(BadRequestObjectResult), result);
        }

        [Test]
        public async Task Hit_DNTEnabled()
        {
            var ctx = new DefaultHttpContext { Items = { ["DNT"] = true } };
            var ctl = new StatisticsController(_mockBlogStatistics.Object)
            {
                ControllerContext = { HttpContext = ctx }
            };

            var result = await ctl.Post(new() { PostId = Guid.NewGuid(), IsLike = false });
            Assert.IsInstanceOf(typeof(OkResult), result);
        }

        [Test]
        public async Task Like_DNTEnabled()
        {
            var ctx = new DefaultHttpContext { Items = { ["DNT"] = true } };
            var ctl = new StatisticsController(_mockBlogStatistics.Object)
            {
                ControllerContext = { HttpContext = ctx }
            };

            var result = await ctl.Post(new() { PostId = Guid.NewGuid(), IsLike = true });
            Assert.IsInstanceOf(typeof(OkResult), result);
        }

        [Test]
        public async Task Hit_SameCookie()
        {
            var uid = Guid.NewGuid();

            var reqMock = new Mock<HttpRequest>();
            reqMock.SetupGet(r => r.Cookies["Hit"]).Returns(uid.ToString());

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(c => c.Request).Returns(reqMock.Object);
            httpContextMock.Setup(c => c.Items).Returns(
                new Dictionary<object, object> { { "DNT", false } });

            var ctl = new StatisticsController(_mockBlogStatistics.Object)
            {
                ControllerContext = { HttpContext = httpContextMock.Object }
            };

            var result = await ctl.Post(new() { PostId = uid, IsLike = false });
            Assert.IsInstanceOf(typeof(OkResult), result);
        }

        [Test]
        public async Task Hit_NewCookie()
        {
            var uid = Guid.NewGuid();

            var ctx = new DefaultHttpContext { Items = { ["DNT"] = false } };
            var ctl = new StatisticsController(_mockBlogStatistics.Object)
            {
                ControllerContext = { HttpContext = ctx }
            };

            var result = await ctl.Post(new() { PostId = uid, IsLike = false });
            Assert.IsInstanceOf(typeof(OkResult), result);
        }

        [Test]
        public async Task Like_SameCookie()
        {
            var uid = Guid.NewGuid();

            var reqMock = new Mock<HttpRequest>();
            reqMock.SetupGet(r => r.Cookies["Liked"]).Returns(uid.ToString());

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(c => c.Request).Returns(reqMock.Object);
            httpContextMock.Setup(c => c.Items).Returns(
                new Dictionary<object, object> { { "DNT", false } });

            var ctl = new StatisticsController(_mockBlogStatistics.Object)
            {
                ControllerContext = { HttpContext = httpContextMock.Object }
            };

            var result = await ctl.Post(new() { PostId = uid, IsLike = true });
            Assert.IsInstanceOf(typeof(ConflictResult), result);
        }

    }
}