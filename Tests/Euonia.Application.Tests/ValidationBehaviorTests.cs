using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Pipeline;
using Nerosoft.Euonia.Validation;

namespace Nerosoft.Euonia.Application.Tests;

public class ValidationBehaviorTests
{
	private sealed class Payload
	{
		public string Name { get; set; }
	}

	/// <summary>
	/// 记录调用并以可配置行为响应的验证器。
	/// </summary>
	private sealed class TrackingValidator : IValidator
	{
		public int CallCount { get; private set; }

		public bool FailNext { get; set; }

		public void Validate<TObject>(TObject item)
			where TObject : class
		{
			CallCount++;
			if (FailNext)
			{
				throw new ValidationException("invalid");
			}
		}

		public Task ValidateAsync<TObject>(TObject item)
			where TObject : class
		{
			Validate(item);
			return Task.CompletedTask;
		}
	}

	private static ValidationBehavior<IMessageEnvelope, object> CreateBehavior(IValidatorFactory factory)
	{
		var services = new ServiceCollection();
		if (factory != null)
		{
			services.AddSingleton(factory);
		}

		var provider = services.BuildServiceProvider();
		return new ValidationBehavior<IMessageEnvelope, object>(provider);
	}

	private static Mock<IMessageEnvelope> CreateEnvelope(object payload)
	{
		var envelope = new Mock<IMessageEnvelope>();
		envelope.Setup(m => m.Payload).Returns(payload);
		return envelope;
	}

	[Fact]
	public async Task DiValidatorFactory_ShouldBeUsedWhenRegistered()
	{
		var validator = new TrackingValidator();
		var factory = new Mock<IValidatorFactory>();
		factory.Setup(f => f.Create()).Returns(validator);

		var behavior = CreateBehavior(factory.Object);
		var called = false;
		PipelineDelegate<IMessageEnvelope, object> next = _ =>
		{
			called = true;
			return Task.FromResult<object>(null);
		};

		await behavior.HandleAsync(CreateEnvelope(new Payload()).Object, next);

		Assert.Equal(1, validator.CallCount);
		Assert.True(called);
	}

	[Fact]
	public async Task NoValidatorRegistered_ShouldSkipValidation()
	{
		var behavior = CreateBehavior(null);
		var called = false;
		PipelineDelegate<IMessageEnvelope, object> next = _ =>
		{
			called = true;
			return Task.FromResult<object>(null);
		};

		await behavior.HandleAsync(CreateEnvelope(new Payload()).Object, next);

		Assert.True(called);
	}

	[Fact]
	public async Task ValidationFailure_ShouldThrowAndSkipNext()
	{
		var validator = new TrackingValidator { FailNext = true };
		var factory = new Mock<IValidatorFactory>();
		factory.Setup(f => f.Create()).Returns(validator);

		var behavior = CreateBehavior(factory.Object);
		var called = false;
		PipelineDelegate<IMessageEnvelope, object> next = _ =>
		{
			called = true;
			return Task.FromResult<object>(null);
		};

		await Assert.ThrowsAsync<ValidationException>(
			() => behavior.HandleAsync(CreateEnvelope(new Payload()).Object, next));

		Assert.False(called);
	}
}
