using System;
using System.ComponentModel;

/// <summary>
/// This structure holds th4e details of a die roll; for
/// example "1d6" or "2d10 + 5"; when you call Roll()
/// we generates
/// </summary>
[Serializable]
public struct DieRoll
{
	/// <summary>
	/// This number of dice to roll; if 0
	/// the roll will always come out as 'Plus'.
	/// 
	/// If negative, the resulting roll is itself negated.
	/// </summary>
	public int DiceCount;

	/// <summary>
	/// DiceSize is the size of the dice to roll. If 0,
	/// the roll will again come out as 'Plus'; If DiceCount
	/// is 1 and Plus is 0, the roll is a random number from
	/// 1 to this.
	/// 
	/// If negative, the resulting roll is itself negated.
	/// </summary>
	public int DiceSize;

	/// <summary>
	/// Plus is an additional number to add to the dice
	/// total.
	/// 
	/// If negative, the roll is offset is a negative direction;
	/// so "1d9-5" is sometimes negative, sometimes positive.
	/// </summary>
	public int Plus;
	
	public DieRoll (int count, int size, int plus = 0)
	{
		this.DiceCount = count;
		this.DiceSize = size;
		this.Plus = plus;
	}

	/// <summary>
	/// Roll() rolls the dice sums DiceCount random numbers,
	/// each from 1 to DiceSize, then adds Plus.
	/// 
	/// If DiceCount or DiceSize is negative, we still roll positive
	/// dice, but negate the result. If both are negative, they cancel out
	/// so it is as if both were positive.
	/// 
	/// default(DiceRoll).Roll() will always return 0.
	/// </summary>
	public int Roll ()
	{
		if (DiceSize != 0 && DiceCount != 0) {
			int count = Math.Abs (DiceCount);
			int size = Math.Abs (DiceSize);

			int total = 0;
			for (int i =0; i < count; ++i) {
				total += UnityEngine.Random.Range (1, size + 1);
			}

			if (DiceCount < 0) {
				total = -total;
			}
			
			if (DiceSize < 0) {
				total = -total;
			}

			return total + Plus;
		} else {
			return Plus;
		}
	}

	public override string ToString ()
	{
		switch (Math.Sign (Plus)) {
		case -1:
			return string.Format ("{0}d{1}-{2}", DiceCount, DiceSize, -Plus);
		case 1:
			return string.Format ("{0}d{1}+{2}", DiceCount, DiceSize, Plus);

		default:
			return string.Format ("{0}d{1}", DiceCount, DiceSize);
		}
	}

	/// <summary>
	/// Parse() parses a text that is in the same form as
	/// ToString() returns, and genrates a DieRoll from it.
	/// 
	/// I have not found way to get the Unity Editor to use
	/// this. Someday!
	/// </summary>
	public static DieRoll Parse (string text)
	{
		int dPos = text.IndexOf ('d');
		int plusPos = text.LastIndexOfAny (new [] { '+', '-' });

		int count = 1;
		if (dPos > 0) {
			string dPart = text.Substring (0, dPos);
			count = int.Parse (dPart);
		}

		int sizeStart = dPos < 0 ? 0 : dPos + 1;
		int sizeEnd = plusPos < sizeStart ? text.Length : plusPos;
		string sizePart = text.Substring (sizeStart, sizeEnd - sizeStart);

		int size = int.Parse (sizePart);
		int plus = 0;

		if (plusPos >= 0) {
			string plusPart = text.Substring (plusPos + 1);
			plus = int.Parse (plusPart);

			if (text [plusPos] == '-') {
				plus = -plus;
			}
		}

		return new DieRoll (count, size, plus);
	}
}