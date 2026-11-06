using System;
using Xunit;

namespace Euonia.Core.Tests
{
    /// <summary>
    /// 验证 <see cref="Optional{T}"/> 的构造、取值、转换与相等性语义。
    /// </summary>
    public class OptionalTests
    {
        /// <summary>
        /// 验证 <c>Of</c> 在传入 <see langword="null"/> 时抛出 <see cref="ArgumentNullException"/>。
        /// </summary>
        [Fact]
        public void OfThrowsWhenValueIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => Optional<string>.Of(null!));
        }

        /// <summary>
        /// 验证 <c>Of</c> 创建包含值的实例，<c>HasValue</c> 为 <see langword="true"/>、<c>IsEmpty</c> 为 <see langword="false"/> 且 <c>Value</c> 返回原值。
        /// </summary>
        [Fact]
        public void OfCreatesOptionalWithValue()
        {
            var opt = Optional<string>.Of("hello");
            Assert.True(opt.HasValue);
            Assert.False(opt.IsEmpty);
            Assert.Equal("hello", opt.Value);
        }

        /// <summary>
        /// 验证 <c>OfNullable</c> 对 <see langword="null"/> 返回空实例，对非空值返回包含该值的实例。
        /// </summary>
        [Fact]
        public void OfNullableReturnsEmptyForNullAndValueForNonNull()
        {
            var empty = Optional<string>.OfNullable(null!);
            Assert.False(empty.HasValue);

            var val = Optional<string>.OfNullable("x");
            Assert.True(val.HasValue);
            Assert.Equal("x", val.Value);
        }

        /// <summary>
        /// 验证访问空实例的 <c>Value</c> 会抛出 <see cref="InvalidOperationException"/>。
        /// </summary>
        [Fact]
        public void ValueThrowsOnEmpty()
        {
            var empty = Optional<int>.Empty;
            Assert.Throws<InvalidOperationException>(() => { var v = empty.Value; });
        }

        /// <summary>
        /// 验证 <c>Where</c> 在谓词为 <see langword="true"/> 时保留值，为 <see langword="false"/> 时返回空实例。
        /// </summary>
        [Fact]
        public void WhereReturnsEmptyWhenPredicateFalseAndKeepsWhenTrue()
        {
            var opt = Optional<int>.Of(5);
            var kept = opt.Where(x => x > 0);
            Assert.True(kept.HasValue);

            var filtered = opt.Where(x => x < 0);
            Assert.False(filtered.HasValue);
        }

        /// <summary>
        /// 验证对空实例调用 <c>Where</c> 返回同一个空实例。
        /// </summary>
        [Fact]
        public void WhereOnEmptyReturnsSameEmptyInstance()
        {
            var empty = Optional<string>.Empty;
            var result = empty.Where(s => true);
            Assert.Same(empty, result);
        }

        /// <summary>
        /// 验证 <c>Select</c> 在有值实例上转换值，在空实例上返回空实例。
        /// </summary>
        [Fact]
        public void SelectTransformsValueOrReturnsEmpty()
        {
            var opt = Optional<string>.Of("a");
            var mapped = opt.Select(s => s + "b");
            Assert.True(mapped.HasValue);
            Assert.Equal("ab", mapped.Value);

            var empty = Optional<string>.Empty;
            var mappedEmpty = empty.Select(s => s + "x");
            Assert.False(mappedEmpty.HasValue);
        }

        /// <summary>
        /// 验证 <c>SelectMany</c> 将嵌套的 <see cref="Optional{T}"/> 扁平化为单层，空实例保持为空。
        /// </summary>
        [Fact]
        public void SelectManyFlattensOptionals()
        {
            var opt = Optional<int>.Of(10);
            var flattened = opt.SelectMany(i => Optional<string>.Of(i.ToString()));
            Assert.True(flattened.HasValue);
            Assert.Equal("10", flattened.Value);

            var empty = Optional<int>.Empty;
            var result = empty.SelectMany(i => Optional<string>.Of(i.ToString()));
            Assert.False(result.HasValue);
        }

        /// <summary>
        /// 验证 <c>Or</c> 在空实例上返回替代值，在有值实例上返回原有值。
        /// </summary>
        [Fact]
        public void OrReturnsOtherWhenEmptyAndValueWhenPresent()
        {
            var empty = Optional<string>.Empty;
            Assert.Equal("fallback", empty.Or("fallback"));

            var some = Optional<string>.Of("v");
            Assert.Equal("v", some.Or("fallback"));
        }

        /// <summary>
        /// 验证带供应器的 <c>Or</c> 仅在空实例时调用供应器，有值时不会调用。
        /// </summary>
        [Fact]
        public void OrWithSupplierUsesSupplierWhenEmptyAndDoesNotCallWhenPresent()
        {
            var empty = Optional<string>.Empty;
            bool called = false;
            string Supplier()
            {
                called = true;
                return "fromSupplier";
            }

            Assert.Equal("fromSupplier", empty.Or(Supplier));
            Assert.True(called);

            called = false;
            var some = Optional<string>.Of("x");
            Assert.Equal("x", some.Or(Supplier));
            Assert.False(called);
        }

        /// <summary>
        /// 验证 <c>GetOrThrow</c> 在空实例上抛出 <see cref="InvalidOperationException"/>，有值时返回该值。
        /// </summary>
        [Fact]
        public void GetOrThrowThrowsWhenEmptyAndReturnsValueWhenPresent()
        {
            var empty = Optional<int>.Empty;
            Assert.Throws<InvalidOperationException>(() => empty.GetOrThrow());

            var some = Optional<int>.Of(3);
            Assert.Equal(3, some.GetOrThrow());
        }

        /// <summary>
        /// 验证带异常工厂的 <c>GetOrThrow</c> 在空实例上抛出工厂提供的异常并保留其消息。
        /// </summary>
        [Fact]
        public void GetOrThrowWithSupplierThrowsSuppliedExceptionWhenEmpty()
        {
            var empty = Optional<string>.Empty;
            var ex = Assert.Throws<InvalidOperationException>(() => empty.GetOrThrow(() => new InvalidOperationException("boom")));
            Assert.Equal("boom", ex.Message);
        }

        /// <summary>
        /// 验证 <c>IfPresent</c> 仅在有值实例上执行动作，空实例不触发动作。
        /// </summary>
        [Fact]
        public void IfPresentInvokesActionOnlyWhenPresent()
        {
            var some = Optional<string>.Of("hello");
            string captured = null!;
            some.IfPresent(s => captured = s);
            Assert.Equal("hello", captured);

            captured = null!;
            Optional<string>.Empty.IfPresent(s => captured = s);
            Assert.Null(captured);
        }

        /// <summary>
        /// 验证空实例调用带空动作重载的 <c>IfPresent</c> 时执行空动作。
        /// </summary>
        [Fact]
        public void IfPresentWithEmptyActionInvokesEmptyActionWhenEmpty()
        {
            bool emptyCalled = false;
            Optional<string>.Empty.IfPresent(s => { }, () => emptyCalled = true);
            Assert.True(emptyCalled);
        }

        /// <summary>
        /// 验证 <c>GetValueOrDefault</c> 在空实例上返回默认值或指定回退值，有值时返回自身值。
        /// </summary>
        [Fact]
        public void GetValueOrDefaultBehavesCorrectly()
        {
            var empty = Optional<int>.Empty;
            Assert.Equal(default(int), empty.GetValueOrDefault());
            Assert.Equal(7, empty.GetValueOrDefault(7));

            var some = Optional<int>.Of(4);
            Assert.Equal(4, some.GetValueOrDefault());
            Assert.Equal(4, some.GetValueOrDefault(7));
        }

        /// <summary>
        /// 验证相等性、哈希码与 <c>ToString</c> 输出的文本格式符合预期。
        /// </summary>
        [Fact]
        public void EqualsAndHashCodeAndToStringBehaveAsExpected()
        {
            var a1 = Optional<string>.Of("v");
            var a2 = Optional<string>.Of("v");
            var b = Optional<string>.Empty;

            Assert.True(a1.Equals(a2));
            Assert.Equal(a1.GetHashCode(), a2.GetHashCode());
            Assert.True(b.Equals(Optional<string>.Empty));
            Assert.Equal("Optional[v]", a1.ToString());
            Assert.Equal("Optional.empty", b.ToString());
        }
    }
}

