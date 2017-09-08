// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class EnumTypeModelBinderTest
    {
        [Theory]
        [InlineData(typeof(IntEnum?))]
        [InlineData(typeof(FlagsEnum?))]
        public async Task BindModel_SetsModel_ForNullableEnumTypes(Type modelType)
        {
            // Arrange
            var bindingContext = GetBindingContext(modelType);
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", "" }
            };

            var binder = new EnumTypeModelBinder(new MvcOptions());

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
            Assert.Null(bindingContext.Result.Model);
        }

        [Theory]
        [InlineData(typeof(IntEnum))]
        [InlineData(typeof(FlagsEnum))]
        public async Task BindModel_AddsErrorToModelState_ForEmptyValue(Type modelType)
        {
            // Arrange
            var message = "The value '' is invalid.";
            var bindingContext = GetBindingContext(modelType);
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", "" }
            };

            var binder = new EnumTypeModelBinder(new MvcOptions());

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(bindingContext.Result.IsModelSet);
            Assert.Null(bindingContext.Result.Model);
            Assert.False(bindingContext.ModelState.IsValid);
            var error = Assert.Single(bindingContext.ModelState["theModelName"].Errors);
            Assert.Equal(message, error.ErrorMessage);
        }

        [Fact]
        public async Task BindModel_BindsEnumModels_IfArrayElementIsStringKey()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(IntEnum));
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", new object[] { "Value1" } }
            };

            var binder = new EnumTypeModelBinder(new MvcOptions());

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
            var boundModel = Assert.IsType<IntEnum>(bindingContext.Result.Model);
            Assert.Equal(IntEnum.Value1, boundModel);
        }

        [Fact]
        public async Task BindModel_BindsEnumModels_IfArrayElementIsStringValue()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(IntEnum));
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", new object[] { "1" } }
            };

            var binder = new EnumTypeModelBinder(new MvcOptions());

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
            var boundModel = Assert.IsType<IntEnum>(bindingContext.Result.Model);
            Assert.Equal(IntEnum.Value1, boundModel);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BindModel_BindsIntEnumModels(bool allowBindingUndefinedValueToEnumType)
        {
            // Arrange
            var modelType = typeof(IntEnum);
            var bindingContext = GetBindingContext(modelType);
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", "2" }
            };

            var binder = new EnumTypeModelBinder(
                new MvcOptions()
                {
                    AllowBindingUndefinedValueToEnumType = allowBindingUndefinedValueToEnumType
                });

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
            Assert.IsType(modelType, bindingContext.Result.Model);
            Assert.True(Enum.IsDefined(modelType, bindingContext.Result.Model));
        }

        [Theory]
        [InlineData("1", true)]
        [InlineData("8, 1", true)]
        [InlineData("Value2, Value8", true)]
        [InlineData("value8,value4,value2,value1", true)]
        [InlineData("1", false)]
        [InlineData("8, 1", false)]
        [InlineData("Value2, Value8", false)]
        [InlineData("value8,value4,value2,value1", false)]
        public async Task BindModel_BindsFlagsEnumModels(string flagsEnumValue, bool allowBindingUndefinedValueToEnumType)
        {
            // Arrange
            var modelType = typeof(FlagsEnum);
            var enumConverter = TypeDescriptor.GetConverter(modelType);
            var expected = enumConverter.ConvertFrom(flagsEnumValue).ToString();
            var bindingContext = GetBindingContext(modelType);
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", flagsEnumValue }
            };

            var binder = new EnumTypeModelBinder(
                new MvcOptions()
                {
                    AllowBindingUndefinedValueToEnumType = allowBindingUndefinedValueToEnumType
                });

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
            var boundModel = Assert.IsType<FlagsEnum>(bindingContext.Result.Model);
            Assert.Equal(expected, boundModel.ToString());
        }

        [Theory]
        [InlineData(typeof(FlagsEnum), "Value10")]
        [InlineData(typeof(FlagsEnum), "Value1, Value10")]
        [InlineData(typeof(FlagsEnum), "value10, value1")]
        public async Task BindModel_AddsErrorToModelState_ForEnumValues_NotValid(Type modelType, string suppliedValue)
        {
            // Arrange
            var message = $"The value '{suppliedValue}' is not valid.";
            var bindingContext = GetBindingContext(modelType);
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", suppliedValue }
            };

            var binder = new EnumTypeModelBinder(new MvcOptions { AllowBindingUndefinedValueToEnumType = false });

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(bindingContext.Result.IsModelSet);
            Assert.Null(bindingContext.Result.Model);
            Assert.False(bindingContext.ModelState.IsValid);
            var error = Assert.Single(bindingContext.ModelState["theModelName"].Errors);
            Assert.Equal(message, error.ErrorMessage);
        }

        [Theory]
        [InlineData(typeof(IntEnum), "")]
        [InlineData(typeof(IntEnum), "3")]
        [InlineData(typeof(FlagsEnum), "19")]
        [InlineData(typeof(FlagsEnum), "0")]
        [InlineData(typeof(FlagsEnum), "1, 16")]
        // These two values look like big integers but are treated as two separate enum values that are
        // or'd together.
        [InlineData(typeof(FlagsEnum), "32,015")]
        [InlineData(typeof(FlagsEnum), "32,128")]
        public async Task BindModel_AddsErrorToModelState_ForEnumValues_Invalid(Type modelType, string suppliedValue)
        {
            // Arrange
            var message = $"The value '{suppliedValue}' is invalid.";
            var bindingContext = GetBindingContext(modelType);
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", suppliedValue }
            };

            var binder = new EnumTypeModelBinder(new MvcOptions { AllowBindingUndefinedValueToEnumType = false });

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(bindingContext.Result.IsModelSet);
            Assert.Null(bindingContext.Result.Model);
            Assert.False(bindingContext.ModelState.IsValid);
            var error = Assert.Single(bindingContext.ModelState["theModelName"].Errors);
            Assert.Equal(message, error.ErrorMessage);
        }

        [Theory]
        [InlineData(typeof(IntEnum), "3", 3)]
        [InlineData(typeof(FlagsEnum), "19", 19)]
        [InlineData(typeof(FlagsEnum), "0", 0)]
        [InlineData(typeof(FlagsEnum), "1, 16", 17)]
        // These two values look like big integers but are treated as two separate enum values that are
        // or'd together.
        [InlineData(typeof(FlagsEnum), "32,015", 47)]
        [InlineData(typeof(FlagsEnum), "32,128", 160)]
        public async Task BindModel_AllowsBindingUndefinedValues_ToEnumTypes(Type modelType, string suppliedValue, long expected)
        {
            // Arrange
            var bindingContext = GetBindingContext(modelType);
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", suppliedValue }
            };

            var binder = new EnumTypeModelBinder(new MvcOptions());

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
            Assert.IsType(modelType, bindingContext.Result.Model);
            Assert.Equal(expected, Convert.ToInt64(bindingContext.Result.Model));
        }

        private static DefaultModelBindingContext GetBindingContext(Type modelType)
        {
            return new DefaultModelBindingContext
            {
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(modelType),
                ModelName = "theModelName",
                ModelState = new ModelStateDictionary(),
                ValueProvider = new SimpleValueProvider() // empty
            };
        }

        [Flags]
        private enum FlagsEnum
        {
            Value1 = 1,
            Value2 = 2,
            Value4 = 4,
            Value8 = 8,
        }

        private enum IntEnum
        {
            Value0 = 0,
            Value1 = 1,
            Value2 = 2,
            MaxValue = int.MaxValue
        }
    }
}
