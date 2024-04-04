using PrintingModule.EDS;

namespace EDSTest
{
    public class StribogTest
    {
        [Theory]
        [InlineData(["AAABBBCCC", "AAABBBCCA", Stribog.Mode.m256])]
        [InlineData(["AAABBBCCC", "AAABBBCCA", Stribog.Mode.m512])]
        [InlineData(["AAABBBCCC", "AAABBBCC", Stribog.Mode.m256])]
        [InlineData(["AAABBBCCC", "AAABBBCC", Stribog.Mode.m512])]
        public void TestDifferentStringsNotEquals(string data1, string data2, Stribog.Mode mode)
        {
            var hash = new Stribog(mode);
            
            var data1Hash = hash.GetHash(data1.ToArray().Select(x=>((byte)x)).ToArray());
            var data2Hash = hash.GetHash(data2.ToArray().Select(x => ((byte)x)).ToArray());

            var flag = true;
            for (var i = 0; i < data1.Length && flag; i++) 
            {
                if (data1Hash[i] != data2Hash[i])
                    flag = false;
            }

            Assert.False(flag);
        }

        [Theory]
        [InlineData(["AAABBBCCC", Stribog.Mode.m256])]
        [InlineData(["AAABBBCCC", Stribog.Mode.m512])]
        public void TestSameStringsEquals(string data1, Stribog.Mode mode)
        {
            var hash = new Stribog(mode);

            var data1Hash = hash.GetHash(data1.ToArray().Select(x => ((byte)x)).ToArray());

            var hash2 = new Stribog(mode);
            var data2Hash = hash2.GetHash(data1.ToArray().Select(x => ((byte)x)).ToArray());

            var flag = true;
            for (var i = 0; i < data1.Length && flag; i++)
            {
                if (data1Hash[i] != data2Hash[i])
                    flag = false;
            }

            Assert.True(flag);
        }
    }
}