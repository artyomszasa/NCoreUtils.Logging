using System.Collections.Generic;
using Xunit;

namespace NCoreUtils.Logging.Unit
{
    public class ReadOnlyDictionaryWrapperTests
    {
        [Fact]
        public void Empty()
        {
            ReadOnlyDictionaryWrapper<string, string> wrapper = default;
            Assert.Equal(0, wrapper.Count);
            Assert.Empty(wrapper.Keys);
            Assert.Empty(wrapper.Values);
            Assert.Throws<KeyNotFoundException>(() => wrapper["x"]);
            Assert.False(wrapper.ContainsKey("x"));
            using (var e = wrapper.GetEnumerator())
            {
                Assert.False(e.MoveNext());
                Assert.False(e.MoveNext());
            }
            Assert.False(wrapper.TryGetValue("x", out var _));
        }
    }
}