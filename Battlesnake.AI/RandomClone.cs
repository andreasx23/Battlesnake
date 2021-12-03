using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Battlesnake.AI
{
    [ComVisible(true)]
    [Serializable]
    public class RandomClone
    {
        private int inext;
        private int inextp;
        private readonly int[] SeedArray = new int[56];

        public RandomClone() : this(Environment.TickCount)
        {
        }

        public RandomClone(int Seed)
        {
            int num1 = 161803398 - (Seed == int.MinValue ? int.MaxValue : Math.Abs(Seed));
            this.SeedArray[55] = num1;
            int num2 = 1;
            for (int index1 = 1; index1 < 55; ++index1)
            {
                int index2 = 21 * index1 % 55;
                this.SeedArray[index2] = num2;
                num2 = num1 - num2;
                if (num2 < 0)
                    num2 += int.MaxValue;
                num1 = this.SeedArray[index2];
            }
            for (int index3 = 1; index3 < 5; ++index3)
            {
                for (int index4 = 1; index4 < 56; ++index4)
                {
                    this.SeedArray[index4] -= this.SeedArray[1 + (index4 + 30) % 55];
                    if (this.SeedArray[index4] < 0)
                        this.SeedArray[index4] += int.MaxValue;
                }
            }
            this.inext = 0;
            this.inextp = 21;
            Seed = 1;
        }

        protected virtual double Sample() => (double)this.InternalSample() * 4.6566128752458E-10;

        private int InternalSample()
        {
            int inext = this.inext;
            int inextp = this.inextp;
            int index1;
            if ((index1 = inext + 1) >= 56)
                index1 = 1;
            int index2;
            if ((index2 = inextp + 1) >= 56)
                index2 = 1;
            int num = this.SeedArray[index1] - this.SeedArray[index2];
            if (num == int.MaxValue)
                --num;
            if (num < 0)
                num += int.MaxValue;
            this.SeedArray[index1] = num;
            this.inext = index1;
            this.inextp = index2;
            return num;
        }

        public virtual int Next() => this.InternalSample();

        private double GetSampleForLargeRange()
        {
            int num = this.InternalSample();
            if (this.InternalSample() % 2 == 0)
                num = -num;
            return ((double)num + 2147483646.0) / 4294967293.0;
        }

        public virtual int Next(int minValue, int maxValue)
        {
            //if (minValue > maxValue)
            //    throw new ArgumentOutOfRangeException(nameof(minValue), Environment.GetResourceString("Argument_MinMaxValue", (object)nameof(minValue), (object)nameof(maxValue)));
            long num = (long)maxValue - (long)minValue;
            return num <= (long)int.MaxValue ? (int)(this.Sample() * (double)num) + minValue : (int)((long)(this.GetSampleForLargeRange() * (double)num) + (long)minValue);
        }

        public virtual int Next(int maxValue)
        {
            //if (maxValue < 0)
            //    throw new ArgumentOutOfRangeException(nameof(maxValue), Environment.GetResourceString("ArgumentOutOfRange_MustBePositive", (object)nameof(maxValue)));
            return (int)(this.Sample() * (double)maxValue);
        }

        public virtual double NextDouble() => this.Sample();

        public virtual void NextBytes(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            for (int index = 0; index < buffer.Length; ++index)
                buffer[index] = (byte)(this.InternalSample() % 256);
        }
    }
}
