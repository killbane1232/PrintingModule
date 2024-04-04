namespace PrintingModule.EDS
{
    public class EDS(BigInteger p, BigInteger a, BigInteger b, BigInteger n, byte[] xG)
    {
        private readonly BigInteger q = n;
        private readonly CECPoint G = GDecompression(xG, p, b, a);

        //генерация секретного ключа заданной длины.
        public BigInteger GenPrivateKey(int BitSize)
        {
            BigInteger d = new();
            do
            {
                d.genRandomBits(BitSize, new Random());
            } while ((d < 0) || (d > q));
            return d;
        }

        //генерация публичного ключа (с помощью секретного).
        public CECPoint GenPublicKey(BigInteger d)
        {
            CECPoint Q = G * d;
            return Q;
        }

        //формирование цифровой подписи.
        public string GenDS(byte[] h, BigInteger d)
        {
            BigInteger a = new(h);
            BigInteger e = a % q;
            if (e == 0)
                e = 1;
            BigInteger k = new();
            CECPoint C;
            BigInteger r;
            BigInteger s;
            var bitcnt = q.bitCount();

            if (new BigInteger("1" + new string('0', bitcnt - 1), 2) * 1000000000 / q < 1000000000)
                bitcnt--;

            do
            {
                do
                {
                    k.genRandomBits(bitcnt, new Random());
                }
                while ((k < 0) || (k > q));

                C = G * k;
                r = C.x % q;
                s = ((r * d) + (k * e)) % q;
            }
            while ((r == 0) || (s == 0));

            string Rvector = Padding(r.ToHexString(), q.bitCount() / 4);
            string Svector = Padding(s.ToHexString(), q.bitCount() / 4);
            return Rvector + Svector;
        }

        //проверка цифровой подписи.
        public bool VerifyDS(byte[] H, string sign, CECPoint Q)
        {
            string Rvector = sign[..(q.bitCount() / 4)];
            string Svector = sign.Substring(q.bitCount() / 4, q.bitCount() / 4);
            BigInteger r = new(Rvector, 16);
            BigInteger s = new(Svector, 16);

            if ((r < 1) || (r > (q - 1)) || (s < 1) || (s > (q - 1)))
                return false;

            BigInteger a = new(H);
            BigInteger e = a % q;
            if (e == 0)
                e = 1;

            BigInteger v = e.modInverse(q);
            BigInteger z1 = s * v % q;
            BigInteger z2 = q + ((-(r * v)) % q);

            CECPoint A = G * z1;
            CECPoint B = Q * z2;
            CECPoint C = A + B;
            BigInteger R = C.x % q;
            if (R == r)
                return true;
            else
                return false;
        }

        //восстановление координат Y из координаты X и бита четности Y.
        private static CECPoint GDecompression(byte[] xG, BigInteger p, BigInteger b, BigInteger a)
        {
            byte y = xG[0];
            byte[] x = new byte[xG.Length - 1];
            Array.Copy(xG, 1, x, 0, xG.Length - 1);
            BigInteger Xcord = new(x);
            BigInteger temp = (Xcord * Xcord * Xcord + a * Xcord + b) % p;
            BigInteger beta = ModSqrt(temp, p);
            BigInteger Ycord;
            if ((beta % 2) == (y % 2))
                Ycord = beta;
            else
                Ycord = p - beta;
            CECPoint G = new()
            {
                a = a,
                b = b,
                fieldChar = p,
                x = Xcord,
                y = Ycord
            };

            return G;
        }

        //вычисление квадратоного корня по модулю простого числа q.
        private static BigInteger ModSqrt(BigInteger a, BigInteger q)
        {
            BigInteger b = new();
            do
            {
                b.genRandomBits(255, new Random((int)DateTime.Now.Ticks));
            }
            while (LegendreSymbol(b, q) == 1);

            BigInteger s = 0;
            BigInteger t = q - 1;
            while ((t & 1) != 1)
            {
                s++;
                t >>= 1;
            }

            BigInteger InvA = a.modInverse(q);
            BigInteger c = b.modPow(t, q);
            BigInteger r = a.modPow(((t + 1) / 2), q);
            BigInteger d;
            for (int i = 1; i < s; i++)
            {
                BigInteger temp = 2;
                temp = temp.modPow(s - i - 1, q);
                d = (r.modPow(2, q) * InvA).modPow(temp, q);
                if (d == (q - 1))
                    r = (r * c) % q;
                c = c.modPow(2, q);
            }
            return r;
        }

        //вычисление символа Лежандра.
        private static BigInteger LegendreSymbol(BigInteger a, BigInteger q)
        {
            return a.modPow((q - 1) / 2, q);
        }

        //дополнить подпись нулями слева до длины n, 
        // где n - длина модуля в битах.
        private static string Padding(string input, int size)
        {
            if (input.Length < size)
            {
                do
                {
                    input = "0" + input;
                }
                while (input.Length < size);
            }
            return input;
        }

        public static byte[] FromHexStringToByte(string input)
        {
            byte[] data = new byte[input.Length / 2];

            for (int i = 0; i < data.Length; i++)
                data[i] = Convert.ToByte(input.Substring(i * 2, 2), 16);

            return data;
        }
    }
}
