#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusBus.Middleware
{
    /// <summary>
    /// Middleware pipeline for Windows Forms applications
    /// Provides ASP.NET Core style middleware patterns for desktop apps
    /// </summary>
    public class ApplicationMiddlewarePipeline
    {
        private readonly List<Func<ApplicationContext, Func<Task>, Task>> _middlewares = new();

        public ApplicationMiddlewarePipeline Use(Func<ApplicationContext, Func<Task>, Task> middleware)
        {
            _middlewares.Add(middleware);
            return this;
        }

        public ApplicationMiddlewarePipeline Use<T>() where T : IApplicationMiddleware, new()
        {
            var middleware = new T();
            return Use(middleware.InvokeAsync);
        }

        public async Task ExecuteAsync(ApplicationContext context)
        {
            var index = 0;

            async Task NextAsync()
            {
                if (index < _middlewares.Count)
                {
                    var middleware = _middlewares[index++];
                    await middleware(context, NextAsync);
                }
            }

            await NextAsync();
        }
    }

    /// <summary>
    /// Builder for creating middleware pipelines
    /// </summary>
    public class ApplicationMiddlewareBuilder
    {
        private readonly ApplicationMiddlewarePipeline _pipeline = new();

        public ApplicationMiddlewareBuilder UseExceptionHandling()
        {
            return Use<ExceptionHandlingMiddleware>();
        }

        public ApplicationMiddlewareBuilder UseLogging()
        {
            return Use<LoggingMiddleware>();
        }

        public ApplicationMiddlewareBuilder UseAuthentication()
        {
            return Use<AuthenticationMiddleware>();
        }

        public ApplicationMiddlewareBuilder Use<T>() where T : IApplicationMiddleware, new()
        {
            _pipeline.Use<T>();
            return this;
        }

        public ApplicationMiddlewareBuilder Use(Func<ApplicationContext, Func<Task>, Task> middleware)
        {
            _pipeline.Use(middleware);
            return this;
        }

        public ApplicationMiddlewarePipeline Build()
        {
            return _pipeline;
        }
    }
}
