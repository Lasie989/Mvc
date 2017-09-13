// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class RequestFormLimitsFilter : IAuthorizationFilter, IRequestFormLimitsPolicy
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new instance of <see cref="RequestSizeLimitFilter"/>.
        /// </summary>
        public RequestFormLimitsFilter(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<RequestFormLimitsFilter>();
        }

        /// <summary>
        /// 
        /// </summary>
        public FormOptions FormOptions { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context">The <see cref="AuthorizationFilterContext"/>.</param>
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (IsClosestPolicy(context.Filters))
            {
                var maxRequestBodySizeFeature = context.HttpContext.Features.Get<IFormFeature>();
                if (maxRequestBodySizeFeature == null)
                {
                }
                else
                {

                }
            }
        }

        private bool IsClosestPolicy(IList<IFilterMetadata> filters)
        {
            // Determine if this instance is the 'effective' request size policy.
            for (var i = filters.Count - 1; i >= 0; i--)
            {
                var filter = filters[i];
                if (filter is IRequestFormLimitsPolicy)
                {
                    return ReferenceEquals(this, filter);
                }
            }

            Debug.Fail("The current instance should be in the list of filters.");
            return false;
        }
    }
}
