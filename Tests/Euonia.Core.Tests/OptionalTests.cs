using System;
using Xunit;

namespace Euonia.Core.Tests
{
    public class OptionalTests
    {
        [Fact]
        public void OfThrowsWhenValueIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => Optional<string>.Of(null!));
        }

        [Fact]
        public void OfCreatesOptionalWithValue()
        {
            var opt = Optional<string>.Of("hello");
            Assert.True(opt.HasValue);
            Assert.False(opt.IsEmpty);
            Assert.Equal("hello", opt.Value);
        }

        [Fact]
        public void OfNullableReturnsEmptyForNullAndValueForNonNull()
        {
            var empty = Optional<string>.OfNullable(null);
            Assert.False(empty.HasValue);

            var val = Optional<string>.OfNullable("x");
            Assert.True(val.HasValue);
            Assert.Equal("x", val.Value);
        }

        [Fact]
        public void ValueThrowsOnEmpty()
        {
            var empty = Optional<int>.Empty;
            Assert.Throws<InvalidOperationException>(() => { var v = empty.Value; });
        }

        [Fact]
        public void WhereReturnsEmptyWhenPredicateFalseAndKeepsWhenTrue()
        {
            var opt = Optional<int>.Of(5);
            var kept = opt.Where(x => x > 0);
            Assert.True(kept.HasValue);

            var filtered = opt.Where(x => x < 0);
            Assert.False(filtered.HasValue);
        }

        [Fact]
        public void WhereOnEmptyReturnsSameEmptyInstance()
        {
            var empty = Optional<string>.Empty;
            var result = empty.Where(s => true);
            Assert.Same(empty, result);
        }

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

        [Fact]
        public void OrReturnsOtherWhenEmptyAndValueWhenPresent()
        {
            var empty = Optional<string>.Empty;
            Assert.Equal("fallback", empty.Or("fallback"));

            var some = Optional<string>.Of("v");
            Assert.Equal("v", some.Or("fallback"));
        }

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

        [Fact]
        public void GetOrThrowThrowsWhenEmptyAndReturnsValueWhenPresent()
        {
            var empty = Optional<int>.Empty;
            Assert.Throws<InvalidOperationException>(() => empty.GetOrThrow());

            var some = Optional<int>.Of(3);
            Assert.Equal(3, some.GetOrThrow());
        }

        [Fact]
        public void GetOrThrowWithSupplierThrowsSuppliedExceptionWhenEmpty()
        {
            var empty = Optional<string>.Empty;
            var ex = Assert.Throws<InvalidOperationException>(() => empty.GetOrThrow(() => new InvalidOperationException("boom")));
            Assert.Equal("boom", ex.Message);
        }

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

        [Fact]
        public void IfPresentWithEmptyActionInvokesEmptyActionWhenEmpty()
        {
            bool emptyCalled = false;
            Optional<string>.Empty.IfPresent(s => { }, () => emptyCalled = true);
            Assert.True(emptyCalled);
        }

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

