using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Rnd = UnityEngine.Random;
using System;

namespace NumberCruncher
{
    public interface NumInput
    {
        string[] Output();
        void Generate();
    }

    public class SingleNumInput : NumInput
    {
        public long Number { get; private set; }
        public void Generate()
        {
            Number = ((long)Rnd.Range(0, 1000000) * (long)1000000) + (long)Rnd.Range(0, 1000000);
        }

        public string[] Output()
        {
            return new[] { Number.ToString("000000000000"), "" };
        }

        public SingleNumInput()
        {
            Generate();
        }

        public int GetDigitAt(int i)
        {
            return int.Parse(Number.ToString("000000000000")[i].ToString());
        }
    }

    public class DoubleNumInput : NumInput
    {
        public long Number1 { get; private set; }
        public long Number2 { get; private set; }
        public void Generate()
        {
            Number1 = new SingleNumInput().Number;
            Number2 = new SingleNumInput().Number;
        }

        public string[] Output()
        {
            return new[] { Number1.ToString("000000000000"), Number2.ToString("000000000000") };
        }

        public DoubleNumInput()
        {
            Generate();
        }

        public int GetDigitAt(int ix, int i)
        {
            return int.Parse((ix == 0 ? Number1 : Number2).ToString("000000000000")[i].ToString());
        }
    }

    public abstract class Calculation
    {
        public string Name { get; }
        public int Score { get; }
        private string _answer;
        public float SoundLength;
        public bool HasHelpMessage;

        protected Calculation(string name, int score, float soundLength, bool hasHelpMessage)
        {
            Name = name;
            Score = score;
            SoundLength = soundLength;
            HasHelpMessage = hasHelpMessage;
        }

        protected abstract string Calculate();
        public string GetAnswer()
        {
            if (_answer == null)
                _answer = Calculate();
            return _answer;
        }
        public abstract string[] GetInputs();
    }

    public abstract class SingleNumCalculation : Calculation
    {
        private SingleNumInput _num;

        public SingleNumCalculation(string name, int score, float soundLength, bool hasHelpMessage) : base(name, score, soundLength, hasHelpMessage)
        {
            _num = new SingleNumInput();
        }

        public override string[] GetInputs()
        {
            return _num.Output();
        }

        protected override string Calculate()
        {
            return Calculate(_num);
        }

        protected abstract string Calculate(SingleNumInput num);
    }

    public abstract class DoubleNumCalculation : Calculation
    {
        private DoubleNumInput _num;

        protected DoubleNumCalculation(string name, int score, float soundLength, bool hasHelpMessage) : base(name, score, soundLength, hasHelpMessage)
        {
            _num = new DoubleNumInput();
        }

        public override string[] GetInputs()
        {
            return _num.Output();
        }

        protected override string Calculate()
        {
            return Calculate(_num);
        }

        protected abstract string Calculate(DoubleNumInput num);
    }

    public class CalculationFactory
    {
        public int UpperBound { get; private set; }
        private const int testIndex = -1;
        public CalculationFactory()
        {
            while (FindCalcType(UpperBound) != null)
            {
                UpperBound++;
            }
        }

        public Calculation Calculation(int currentScore, int requirement, Calculation prev)
        {
            if (testIndex == -1)
                return FindCandidates(currentScore, requirement, prev).PickRandom();
            return FindCalcType(testIndex);
        }

        public Calculation FindCalcType(int ix)
        {
            switch (ix)
            {
                case 0: return new Equality();
                case 1: return new Reversal();
                case 2: return new Parity();
                case 3: return new Sorting();
                case 4: return new LunarLogic();
                case 5: return new Difference();
                case 6: return new Product();
                case 7: return new Inverse();
                case 8: return new ExclusiveOR();
                case 9: return new PairProducts();
                case 10: return new Sum();
                case 11: return new Lovers();
                case 12: return new NumericWeight();
                case 13: return new PrimeChecker();
                case 14: return new Attendance();
                case 15: return new NeighbourSum();
                case 16: return new Triplity();
                case 17: return new Magnitude();
                case 18: return new XSum();
                case 19: return new MaximumDigit();
                case 20: return new Means();
                case 21: return new Thermometer();
                case 22: return new Altitude();
                case 23: return new Modulo();
                case 24: return new PlusOne();
                default: return null;
            }
        }

        private List<Calculation> FindCandidates(int currentScore, int requirement, Calculation prev)
        {
            var candidates = new List<Calculation>();
            for (int i = 0; i < UpperBound; i++)
                if ((prev == null || FindCalcType(i).Name != prev.Name) && currentScore + FindCalcType(i).Score <= requirement)
                    candidates.Add(FindCalcType(i));
            if (candidates.Count() == 0)
                return FindCandidates(currentScore, requirement + 1, prev);
            return candidates;
        }
    }

    public class Equality : SingleNumCalculation
    {
        public Equality() : base("Equality", 2, 4f + (13065f / 44100f), true) { }

        protected override string Calculate(SingleNumInput num)
        {
            return num.Number.ToString("000000000000");
        }
    }

    public class Reversal : SingleNumCalculation
    {
        public Reversal() : base("Reversal", 2, 3f + (43060f / 44100f), true) { }

        protected override string Calculate(SingleNumInput num)
        {
            return num.Number.ToString("000000000000").Reverse().Join("");
        }
    }

    public class Parity : SingleNumCalculation
    {
        public Parity() : base("Parity", 1, 3f + (42110f / 44100f), true) { }

        protected override string Calculate(SingleNumInput num)
        {
            return num.Number.ToString("000000000000").ToList().Select(x => int.Parse(x.ToString()) % 2).Join("");
        }
    }

    public class Sorting : SingleNumCalculation
    {
        public Sorting() : base("Sorting", 4, 4f + (27410f / 44100f), true) { }

        protected override string Calculate(SingleNumInput num)
        {
            var temp = num.Number.ToString("000000000000").ToList();
            temp.Sort();
            return temp.Join("");
        }
    }

    public class Inverse : SingleNumCalculation
    {
        public Inverse() : base("Inverse", 4, 4f + (21696f / 44100f), true) { }

        protected override string Calculate(SingleNumInput num)
        {
            var temp = "";
            for (int i = 0; i < 12; i++)
                temp += 9 - num.GetDigitAt(i);
            return temp;
        }
    }

    public class PairProducts : SingleNumCalculation
    {
        public PairProducts() : base("Pair Products", 4, 7f + (8362f / 44100f), true) { }

        protected override string Calculate(SingleNumInput num)
        {
            var temp = "";
            for (int i = 0; i < 12; i += 2)
                temp += (num.GetDigitAt(i) * num.GetDigitAt(i + 1)).ToString("00");
            return temp;
        }
    }

    public class Sum : SingleNumCalculation
    {
        public Sum() : base("Sum", 4, 7f + (1412f / 44100f), true) { }

        protected override string Calculate(SingleNumInput num)
        {
            var prev = num.GetDigitAt(0);
            var temp = prev.ToString();
            for (int i = 1; i < 12; i++)
            {
                prev = (prev + num.GetDigitAt(i)) % 10;
                temp += prev.ToString();
            }
            return temp;
        }
    }

    public class Attendance : SingleNumCalculation
    {
        public Attendance() : base("Attendance", 4, 6f + (31317f / 44100f), true) { }

        protected override string Calculate(SingleNumInput num)
        {
            var temp = "";
            for (int i = 0; i < 12; i++)
            {
                temp += num.Number.ToString("000000000000").Where(x => x == num.GetDigitAt(i).ToString()[0]).Count() % 10;
            }
            return temp;
        }
    }

    public class NeighbourSum : SingleNumCalculation
    {
        public NeighbourSum() : base("Neighbour Sum", 6, 6f + (37450f / 44100f), true) { }

        protected override string Calculate(SingleNumInput num)
        {
            var temp = ((num.GetDigitAt(0) + num.GetDigitAt(1)) % 10).ToString();
            for (int i = 1; i < 11; i++)
                temp += ((num.GetDigitAt(i - 1) + num.GetDigitAt(i) + num.GetDigitAt(i + 1)) % 10).ToString();
            temp += ((num.GetDigitAt(10) + num.GetDigitAt(11)) % 10).ToString();
            return temp;
        }
    }

    public class Triplity : SingleNumCalculation
    {
        public Triplity() : base("Triplity", 2, 3f + (37214f / 44100f), true) { }

        protected override string Calculate(SingleNumInput num)
        {
            return num.Number.ToString("000000000000").ToList().Select(x => int.Parse(x.ToString()) % 3).Join("");
        }
    }

    public class Magnitude : SingleNumCalculation
    {
        public Magnitude() : base("Magnitude", 1, 6f + (30247f / 44100f), true) { }

        protected override string Calculate(SingleNumInput num)
        {
            return num.Number.ToString("000000000000").ToList().Select(x => int.Parse(x.ToString()) / 5).Join("");
        }
    }

    public class XSum : SingleNumCalculation
    {
        public XSum() : base("X-Sum", 7, 9f + (4624f / 44100f), true) { }

        protected override string Calculate(SingleNumInput num)
        {
            var temp = "";
            for (int i = 0; i < 12; i++)
            {
                var total = 0;
                for (int j = 0; j < num.GetDigitAt(i); j++)
                    total += num.GetDigitAt(j);
                temp += total % 10;
            }
            return temp;
        }
    }

    public class MaximumDigit : SingleNumCalculation
    {
        public MaximumDigit() : base("Maximum Digit", 2, 7f + (41464f / 44100f), true) { }

        protected override string Calculate(SingleNumInput num)
        {
            var temp = num.GetDigitAt(0) > num.GetDigitAt(1) ? "1" : "0";
            for (int i = 1; i < 11; i++)
                temp += (num.GetDigitAt(i) > num.GetDigitAt(i - 1) && num.GetDigitAt(i) > num.GetDigitAt(i + 1)) ? "1" : "0";
            temp += num.GetDigitAt(10) < num.GetDigitAt(11) ? "1" : "0";
            return temp;
        }
    }

    public class Thermometer : SingleNumCalculation
    {
        public Thermometer() : base("Thermometer", 2, 9f + (15337f / 44100f), true) { }

        protected override string Calculate(SingleNumInput num)
        {
            var temp = "1";
            for (int i = 1; i < 12; i++)
                temp += num.GetDigitAt(i) > num.GetDigitAt(i - 1) ? "1" : "0";
            return temp;
        }
    }

    public class Altitude : SingleNumCalculation
    {
        public Altitude() : base("Altitude", 7, 6f + (31221f / 44100f), true) { }

        protected override string Calculate(SingleNumInput num)
        {
            var temp = "";
            for (int i = 0; i < 12; i++)
                temp += Enumerable.Range(0, 12).Where(x => num.GetDigitAt(x) < num.GetDigitAt(i)).Count() % 10;
            return temp;
        }
    }

    public class PlusOne : SingleNumCalculation
    {
        public PlusOne() : base("Plus One", 6, 6f + (38344f / 44100f), true) { }

        protected override string Calculate(SingleNumInput num)
        {
            var temp = "";
            for (int i = 0; i < 12; i++)
                temp += (num.GetDigitAt(i) + i) % 10;
            return temp;
        }
    }

    public class LunarLogic : DoubleNumCalculation
    {
        public LunarLogic() : base("Lunar Logic", 4, 5f + (16464f / 44100f), true) { }

        protected override string Calculate(DoubleNumInput nums)
        {
            var temp = "";
            for (int i = 0; i < 12; i++)
                temp += Mathf.Max(nums.GetDigitAt(0, i), nums.GetDigitAt(1, i));
            return temp;
        }
    }

    public class Difference : DoubleNumCalculation
    {
        public Difference() : base("Difference", 4, 4f + (43556f / 44100f), true) { }

        protected override string Calculate(DoubleNumInput nums)
        {
            var temp = "";
            for (int i = 0; i < 12; i++)
                temp += Mathf.Abs(nums.GetDigitAt(0, i) - nums.GetDigitAt(1, i));
            return temp;
        }
    }

    public class Product : DoubleNumCalculation
    {
        public Product() : base("Product", 5, 5f + (17739f / 44100f), true) { }

        protected override string Calculate(DoubleNumInput nums)
        {
            var temp = "";
            for (int i = 0; i < 12; i++)
                temp += (nums.GetDigitAt(0, i) * nums.GetDigitAt(1, i)) % 10;
            return temp;
        }
    }

    public class ExclusiveOR : DoubleNumCalculation
    {
        public ExclusiveOR() : base("Exclusive OR", 4, 6f + (5796f / 44100f), true) { }

        protected override string Calculate(DoubleNumInput nums)
        {
            var temp = "";
            for (int i = 0; i < 12; i++)
            {
                if (nums.GetDigitAt(0, i) % 2 != nums.GetDigitAt(1, i) % 2)
                    temp += "1";
                else
                    temp += "0";
            }
            return temp;
        }
    }

    public class Lovers : DoubleNumCalculation
    {
        public Lovers() : base("Lovers", 5, 7f + (17790f / 44100f), true) { }

        protected override string Calculate(DoubleNumInput nums)
        {
            var temp = "";
            for (int i = 0; i < 12; i++)
                temp += Mathf.Abs((nums.GetDigitAt(0, i) % 5) - (nums.GetDigitAt(1, i) % 5)) + Mathf.Abs((nums.GetDigitAt(0, i) / 5) - (nums.GetDigitAt(1, i) / 5));
            return temp;
        }
    }

    public class NumericWeight : DoubleNumCalculation
    {
        public NumericWeight() : base("Numeric Weight", 7, 7f + (20442f / 44100f), true) { }

        protected override string Calculate(DoubleNumInput nums)
        {
            var temp = "";
            var binary = new[] { 0, 1, 1, 2, 1, 2, 2, 3, 1, 2 };
            for (int i = 0; i < 12; i++)
                temp += (binary[nums.GetDigitAt(0, i)] + binary[nums.GetDigitAt(1, i)]);
            return temp;
        }
    }

    public class PrimeChecker : DoubleNumCalculation
    {
        public PrimeChecker() : base("Prime Checker", 3, 6f + (34713f / 44100f), true) { }

        protected override string Calculate(DoubleNumInput nums)
        {
            var temp = "";
            var primes = new[] { 2, 3, 5, 7, 11, 13, 17, 19 };
            for (int i = 0; i < 12; i++)
                temp += primes.Contains(nums.GetDigitAt(0, i) + nums.GetDigitAt(1, i)) ? "1" : "0";
            return temp;
        }
    }

    public class Means : DoubleNumCalculation
    {
        public Means() : base("Means", 4, 8f + (28328f / 44100f), true) { }

        protected override string Calculate(DoubleNumInput nums)
        {
            var temp = "";
            for (int i = 0; i < 12; i++)
                temp += (nums.GetDigitAt(0, i) + nums.GetDigitAt(1, i)) / 2;
            return temp;
        }
    }

    public class Modulo : DoubleNumCalculation
    {
        public Modulo() : base("Modulo", 4, 9f + (4677f / 44100f), true) { }

        protected override string Calculate(DoubleNumInput nums)
        {
            var temp = "";
            for (int i = 0; i < 12; i++)
            {
                var modifiedNums = new[] { nums.GetDigitAt(0, i), nums.GetDigitAt(1, i) }.Select(x => x == 0 ? 10 : x).ToList();
                temp += modifiedNums[0] % modifiedNums[1];
            }
            return temp;
        }
    }
}