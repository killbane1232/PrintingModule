namespace PrintingModule.EDS
{
    public class CECPoint
    {
        public BigInteger a;
        public BigInteger b;
        public BigInteger x;
        public BigInteger y;
        public BigInteger fieldChar;

        public CECPoint()
        {
            a = new();
            b = new();
            x = new();
            y = new();
            fieldChar = new();
        }

        public CECPoint(CECPoint p)
        {
            a = p.a;
            b = p.b;
            x = p.x;
            y = p.y;
            fieldChar = p.fieldChar;
        }

        public CECPoint(string str)
        {
            var arr = str.Split('$');
            a = new BigInteger(arr[0], 16);
            b = new BigInteger(arr[1], 16);
            x = new BigInteger(arr[2], 16);
            y = new BigInteger(arr[3], 16);
            fieldChar = new BigInteger(arr[4], 16);
        }

        //сложение пары точек.
        public static CECPoint operator +(CECPoint p1, CECPoint p2)
        {
            CECPoint res = new()
            {
                a = p1.a,
                b = p1.b,
                fieldChar = p1.fieldChar
            };

            BigInteger dx = p2.x - p1.x;
            BigInteger dy = p2.y - p1.y;

            if (dx < 0)
                dx += p1.fieldChar;
            if (dy < 0)
                dy += p1.fieldChar;

            BigInteger t = dy * dx.modInverse(p1.fieldChar) % p1.fieldChar;

            if (t < 0)
                t += p1.fieldChar;

            res.x = (t * t - p1.x - p2.x) % p1.fieldChar;
            res.y = (t * (p1.x - res.x) - p1.y) % p1.fieldChar;

            if (res.x < 0)
                res.x += p1.fieldChar;
            if (res.y < 0)
                res.y += p1.fieldChar;

            return res;
        }

        //удвоение точки.
        public static CECPoint Doubling(CECPoint p)
        {
            CECPoint res = new()
            {
                a = p.a,
                b = p.b,
                fieldChar = p.fieldChar
            };

            BigInteger dx = 2 * p.y;
            BigInteger dy = 3 * p.x * p.x + p.a;

            if (dx < 0)
                dx += p.fieldChar;
            if (dy < 0)
                dy += p.fieldChar;

            BigInteger t = dy * dx.modInverse(p.fieldChar) % p.fieldChar;
            res.x = (t * t - p.x - p.x) % p.fieldChar;
            res.y = (t * (p.x - res.x) - p.y) % p.fieldChar;

            if (res.x < 0)
                res.x += p.fieldChar;
            if (res.y < 0)
                res.y += p.fieldChar;

            return res;
        }

        //умножение точки на число.
        public static CECPoint operator*(CECPoint p, BigInteger c)
        {
            CECPoint res = p;
            c--;
            while (c != 0)
            {
                if ((c % 2) != 0)
                {
                    if ((res.x == p.x) || (res.y == p.y))
                        res = Doubling(res);
                    else
                        res += p;
                    c--;
                }

                c /= 2;
                p = Doubling(p);
            }

            return res;
        }

        public string ToHexString()
        {
            return a.ToHexString() + "$" + b.ToHexString() + "$" + x.ToHexString() + "$" + y.ToHexString() + "$" + fieldChar.ToHexString();
        }
    }
}
