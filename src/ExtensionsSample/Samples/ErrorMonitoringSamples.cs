// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions;

namespace ExtensionsSample.Samples
{
    public static class ErrorMonitoringSamples
    {
        private static TraceNotifier Notifier = new TraceNotifier(new SendGridConfiguration());

        public static void ErrorMonitor(
            [ErrorTrigger("00:05:00", 3)] TraceFilter filter)
        {
            Notifier.WebNotify(filter);
        }
    }
}
