// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// <see cref="IModelBinder"/> implementation to bind models for types deriving from <see cref="System.Enum"/>.
    /// </summary>
    public class EnumTypeModelBinder : IModelBinder
    {
        private readonly MvcOptions _mvcOptions;

        public EnumTypeModelBinder(MvcOptions mvcOptions)
        {
            _mvcOptions = mvcOptions;
        }

        /// <inheritdoc />
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                // no entry
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);
            var modelType = bindingContext.ModelMetadata.UnderlyingOrModelType;
            var stringValue = valueProviderResult.FirstValue;

            try
            {
                object model;
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    model = null;
                }
                else
                {
                    var typeConverter = TypeDescriptor.GetConverter(modelType);
                    model = typeConverter.ConvertFrom(
                        context: null,
                        culture: valueProviderResult.Culture,
                        value: stringValue);
                }

                // Check if the converted value is indeed defined on the enum as EnumConverter converts value to the backing type (ex: integer)
                // and does not check if the value is indeed defined on the enum.
                if (!_mvcOptions.AllowBindingUndefinedValueToEnumType && model != null)
                {
                    var isFlagsEnum = modelType.IsDefined(typeof(FlagsAttribute), inherit: false);
                    if (isFlagsEnum)
                    {
                        // From EnumDataTypeAttribute.cs in CoreFX
                        var underlying = Convert.ChangeType(model, Enum.GetUnderlyingType(modelType), valueProviderResult.Culture).ToString();
                        var converted = model.ToString();
                        if (string.Equals(underlying, converted, StringComparison.OrdinalIgnoreCase))
                        {
                            model = null;
                        }
                    }
                    else
                    {
                        if(!Enum.IsDefined(modelType, model))
                        {
                            model = null;
                        }
                    }
                }

                // When converting newModel a null value may indicate a failed conversion for an otherwise required
                // model (can't set a ValueType to null). This detects if a null model value is acceptable given the
                // current bindingContext. If not, an error is logged.
                if (model == null && !bindingContext.ModelMetadata.IsReferenceOrNullableType)
                {
                    bindingContext.ModelState.TryAddModelError(
                        bindingContext.ModelName,
                        bindingContext.ModelMetadata.ModelBindingMessageProvider.ValueMustNotBeNullAccessor(
                            valueProviderResult.ToString()));
                }
                else
                {
                    bindingContext.Result = ModelBindingResult.Success(model);
                }
                return Task.CompletedTask;
            }
            catch (Exception exception)
            {
                var isFormatException = exception is FormatException;
                if (!isFormatException && exception.InnerException != null)
                {
                    // TypeConverter throws System.Exception wrapping the FormatException,
                    // so we capture the inner exception.
                    exception = ExceptionDispatchInfo.Capture(exception.InnerException).SourceException;
                }

                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    exception,
                    bindingContext.ModelMetadata);

                // Were able to find a converter for the type but conversion failed.
                return Task.CompletedTask;
            }
        }
    }
}
