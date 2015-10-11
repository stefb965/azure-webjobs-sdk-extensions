// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.Azure.WebJobs.Extensions.Tests.Common;
using Microsoft.Azure.WebJobs.Host;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Tests.Monitoring
{
    public class ErrorTriggerEndToEndTests
    {
        [Fact]
        public void SlidingWindow()
        {
            ErrorTriggerProgram_SlidingWindow instance = new ErrorTriggerProgram_SlidingWindow();

            JobHostConfiguration config = new JobHostConfiguration()
            {
                TypeLocator = new ExplicitTypeLocator(instance.GetType()),
                JobActivator = new ExplicitJobActivator(instance)
            };
            config.UseCore();
            JobHost host = new JobHost(config);
            host.Start();

            MethodInfo method = instance.GetType().GetMethod("Throw");
            CallSafe(host, method);
            CallSafe(host, method);
            Assert.Null(instance.TraceFilter);

            CallSafe(host, method);
            Assert.NotNull(instance.TraceFilter);

            Assert.Equal("3 events at level 'Error' or lower have occurred within time window 00:05:00.", instance.TraceFilter.Message);
            Assert.Equal(3, instance.TraceFilter.Traces.Count);
            foreach (TraceEvent traceEvent in instance.TraceFilter.Traces)
            {
                FunctionInvocationException functionException = (FunctionInvocationException)traceEvent.Exception;
                Assert.Equal("Kaboom!", functionException.InnerException.Message);
                Assert.Equal("Microsoft.Azure.WebJobs.Extensions.Tests.Monitoring.ErrorTriggerEndToEndTests+ErrorTriggerProgram_SlidingWindow.Throw", functionException.MethodName);
            }
        }

        [Fact]
        public void AllErrors()
        {
            ErrorTriggerProgram_AllErrors instance = new ErrorTriggerProgram_AllErrors();

            JobHostConfiguration config = new JobHostConfiguration()
            {
                TypeLocator = new ExplicitTypeLocator(instance.GetType()),
                JobActivator = new ExplicitJobActivator(instance)
            };
            config.UseCore();
            JobHost host = new JobHost(config);
            host.Start();

            MethodInfo method = instance.GetType().GetMethod("Throw");
            CallSafe(host, method);
            Assert.NotNull(instance.TraceFilter);

            Assert.Equal("WebJob failure detected.", instance.TraceFilter.Message);
            Assert.Equal(1, instance.TraceFilter.Traces.Count);
        }

        [Fact]
        public void FunctionErrors()
        {
            ErrorTriggerProgram_FunctionErrorHandler instance = new ErrorTriggerProgram_FunctionErrorHandler();

            JobHostConfiguration config = new JobHostConfiguration()
            {
                TypeLocator = new ExplicitTypeLocator(instance.GetType()),
                JobActivator = new ExplicitJobActivator(instance)
            };
            config.UseCore();
            JobHost host = new JobHost(config);
            host.Start();

            MethodInfo method = instance.GetType().GetMethod("ThrowA");
            CallSafe(host, method);
            CallSafe(host, method);
            Assert.Null(instance.TraceFilter);

            method = instance.GetType().GetMethod("ThrowB");
            CallSafe(host, method);

            Assert.Equal("Function 'ErrorTriggerProgram_FunctionErrorHandler.ThrowB' failed.", instance.TraceFilter.Message);
            Assert.Equal(1, instance.TraceFilter.Traces.Count);
        }

        private void CallSafe(JobHost host, MethodInfo method)
        {
            try
            {
                host.Call(method);
            }
            catch {}
        }

        public class ErrorTriggerProgram_SlidingWindow
        {
            public TraceFilter TraceFilter { get; set; }

            [NoAutomaticTrigger]
            public void Throw()
            {
                throw new Exception("Kaboom!");
            }

            public void ErrorHandler([ErrorTrigger("00:05:00", 3)] TraceFilter filter)
            {
                TraceFilter = filter;
            }
        }

        public class ErrorTriggerProgram_AllErrors
        {
            public TraceFilter TraceFilter { get; set; }

            [NoAutomaticTrigger]
            public void Throw()
            {
                throw new Exception("Kaboom!");
            }

            public void ErrorHandler([ErrorTrigger] TraceFilter filter)
            {
                TraceFilter = filter;
            }
        }

        public class ErrorTriggerProgram_FunctionErrorHandler
        {
            public TraceFilter TraceFilter { get; set; }

            [NoAutomaticTrigger]
            public void ThrowA()
            {
                throw new Exception("Kaboom!");
            }

            [NoAutomaticTrigger]
            public void ThrowB()
            {
                throw new Exception("Kaboom!");
            }

            public void ThrowBErrorHandler([ErrorTrigger] TraceFilter filter)
            {
                TraceFilter = filter;
            }
        }

        public class ExplicitJobActivator : IJobActivator
        {
            private readonly object _instance;

            public ExplicitJobActivator(object instance)
            {
                _instance = instance;
            }

            public T CreateInstance<T>()
            {
                return (T)_instance;
            }
        }
    }
}
