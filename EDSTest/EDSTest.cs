using PrintingModule.EDS;
using System.Text;

namespace EDSTest
{
    public class EDSTest
    {
        [Theory]
        [InlineData(["AAddAAbbaepegaasggergzbxcvzsdtawerjoifjbhpoxcifhjbiuozdxfhg"])]
        [InlineData(["VeryLongTestData"])]
        public void TestEDSVerify(string message)
        {
            //А.1 Пример 1 из ГОСТ 34.10-2012
            BigInteger p = new("8000000000000000000000000000000000000000000000000000000000000431", 16);
            BigInteger a = new("7", 10);
            BigInteger b = new("5FBFF498AA938CE739B8E022FBAFEF40563F6E6A3472FC2A514C0CE9DAE23B7E", 16);
            byte[] xG = EDS.FromHexStringToByte("8000000000000000000000000000000150FE8A1892976154C59CFC193ACCF5B3");
            BigInteger n = new("8000000000000000000000000000000150FE8A1892976154C59CFC193ACCF5B3", 16);

            EDS DS = new(p, a, b, n, xG);
            BigInteger d = DS.GenPrivateKey(192);
            CECPoint Q = DS.GenPublicKey(d);
            var str = Q.ToHexString();
            Stribog hash = new(Stribog.Mode.m256);

            byte[] H = hash.GetHash(Encoding.Default.GetBytes(message));
            string sign = DS.GenDS(H, d);
            
            DS = new(p, a, b, n, xG);

            Assert.True(DS.VerifyDS(H, sign, new CECPoint(str)));
        }

        [Theory]
        [InlineData(["AAddAAbbaepegaasggergzbxcvzsdtawerjoifjbhpoxcifhjbiuozdxfhg", "AAddAAbbaepegaasggergzbxcvzsdtawerjpifjbhpoxcifhjbiuozdxfhg"])]
        [InlineData(["VeryLongTestDataAAddAAbbaepegaasggergzbxcvzsdtawerjoifjbhpoxcifhjbiuozdxfhgAAddAAbbaepegaasggergzbxcvzsdtawerjoifjbhpoxcifhjbiuozdxfhg", "VeryLongTestDataAAddAAbbaepegaasggergzbxcvzsdtawerjoifjbhpoxcifhjbiuozdxfhgAAddAAbbaepegaasggergzbxcvzsdtawerjoifjbhpoxcifhjbiuozdxfhh"])]
        public void TestEDSVerifyFalse(string message, string message2)
        {
            //А.1 Пример 1 из ГОСТ 34.10-2012
            BigInteger p = new("8000000000000000000000000000000000000000000000000000000000000431", 16);
            BigInteger a = new("7", 10);
            BigInteger b = new("5FBFF498AA938CE739B8E022FBAFEF40563F6E6A3472FC2A514C0CE9DAE23B7E", 16);
            byte[] xG = EDS.FromHexStringToByte("8000000000000000000000000000000150FE8A1892976154C59CFC193ACCF5B3");
            BigInteger n = new("8000000000000000000000000000000150FE8A1892976154C59CFC193ACCF5B3", 16);

            EDS DS = new(p, a, b, n, xG);
            BigInteger d = DS.GenPrivateKey(192);
            CECPoint Q = DS.GenPublicKey(d);
            Stribog hash = new(Stribog.Mode.m256);

            byte[] H = hash.GetHash(Encoding.Default.GetBytes(message));
            string sign = DS.GenDS(H, d);

            byte[] H2 = hash.GetHash(Encoding.Default.GetBytes(message2));

            Assert.False(DS.VerifyDS(H2, sign, Q));
        }
    }
}
