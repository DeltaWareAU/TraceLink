﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TraceLink.Abstractions.Context;
using TraceLink.Abstractions.Forwarder;
using TraceLink.Abstractions.Options;
using TraceLink.Abstractions.Providers;
using TraceLink.AspNetCore.Context.Scopes;
using Xunit;

namespace TraceLink.AspNetCore.Tests
{
    public class CorrelationShould
    {
        [Fact]
        public async Task ReturnBadRequest_WhenIsRequired_AndNoHeaderIsProvided()
        {
            var builder = new WebHostBuilder()
                .Configure(app => app.UseCorrelation())
                .ConfigureServices(sc => sc.AddCorrelation(options => options.IsRequired = true));

            using var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("");

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task NotReturnBadRequest_WhenIsRequired_AndHeaderIsProvided()
        {
            var builder = new WebHostBuilder()
                .Configure(app => app.UseCorrelation())
                .ConfigureServices(sc => sc.AddCorrelation(options => options.IsRequired = true));

            using var server = new TestServer(builder);

            var correlationIdProvider = server.Services.GetRequiredService<IIdProvider<CorrelationContext>>();
            var options = server.Services.GetRequiredService<ITracingOptions<CorrelationContext>>();

            using var client = server.CreateClient();

            client.DefaultRequestHeaders.Add(options.Key, correlationIdProvider.GenerateId());

            var response = await client.GetAsync("");

            response.Headers.TryGetValues(options.Key, out _).ShouldBeFalse();
            response.StatusCode.ShouldNotBe(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task NotReturnBadRequest_WhenIsNotRequired_AndNoHeaderIsProvided()
        {
            var builder = new WebHostBuilder()
                .Configure(app => app.UseCorrelation())
                .ConfigureServices(sc => sc.AddCorrelation(options => options.IsRequired = false));

            using var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("");

            response.StatusCode.ShouldNotBe(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ReturnId_WhenAttachToResponseIsEnabled()
        {
            var builder = new WebHostBuilder()
                .Configure(app => app.UseCorrelation())
                .ConfigureServices(sc => sc.AddCorrelation(options => options.AttachToResponse = true));

            using var server = new TestServer(builder);

            var correlationId = server.Services.GetRequiredService<IIdProvider<CorrelationContext>>().GenerateId();
            var options = server.Services.GetRequiredService<ITracingOptions<CorrelationContext>>();

            var request = new HttpRequestMessage();
            request.Headers.Add(options.Key, correlationId);

            var response = await server.CreateClient().SendAsync(request);

            response.Headers.TryGetValues(options.Key, out var headerValues).ShouldBeTrue();

            headerValues!.Single().ShouldBe(correlationId);

            response.StatusCode.ShouldNotBe(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UseSpecifiedKey()
        {
            string headerValue = "x-testing-header-changed";

            var builder = new WebHostBuilder()
                .Configure(app => app.UseCorrelation())
                .ConfigureServices(sc => sc.AddCorrelation(options =>
                {
                    options.Key = headerValue;
                    options.AttachToResponse = true;
                }));

            using var server = new TestServer(builder);

            var correlationId = server.Services.GetRequiredService<IIdProvider<CorrelationContext>>().GenerateId();
            var options = server.Services.GetRequiredService<ITracingOptions<CorrelationContext>>();

            var request = new HttpRequestMessage();
            request.Headers.Add(options.Key, correlationId);

            var response = await server.CreateClient().SendAsync(request);

            options.Key.ShouldBe(headerValue);

            response.Headers.TryGetValues(options.Key, out var headerValues).ShouldBeTrue();

            headerValues!.Single().ShouldBe(correlationId);

            response.StatusCode.ShouldNotBe(HttpStatusCode.BadRequest);
        }

        [Fact]
        public void GetForwardingId()
        {
            ServiceCollection services = new ServiceCollection();

            services.AddCorrelation();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            string headerKey = serviceProvider.GetRequiredService<ITracingOptions<CorrelationContext>>().Key;
            string correlationId = serviceProvider.GetRequiredService<IIdProvider<CorrelationContext>>().GenerateId();
            IHttpContextAccessor httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();

            httpContextAccessor.HttpContext = new DefaultHttpContext();
            httpContextAccessor.HttpContext.Request.Headers.Add(headerKey, correlationId);

            serviceProvider
                .GetRequiredService<IAspNetTracingScope<CorrelationContext>>()
                .SetId(true);

            serviceProvider
                .GetRequiredService<IIdForwarder<CorrelationContext>>()
                .GetForwardingId()
                .ShouldBe(correlationId);
        }
    }
}
