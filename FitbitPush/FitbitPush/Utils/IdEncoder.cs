using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitbitPush.Utils
{
	public static class IdEncoder
    {
	    public static string Encode(Guid id)
	    {
		    var bin = string.Join("", id.ToString("N").Select(CharToBits));
		    var encoded = new StringBuilder();

		    int i;
		    for (i = 0; i < (bin.Length / 6) * 6; i += 6)
		    {
			    encoded.Append(EncodeBitsToChar(bin.Substring(i, 6)));
		    }
		    for (; i < bin.Length; i += 2)
		    {
			    encoded.Append(EncodeBitsToChar(bin.Substring(i, 2)));
		    }


		    return encoded.ToString();
	    }

	    public static Guid Decode(string encoded)
	    {
		    var bin = string.Join("", encoded.Select(DecodeCharTo6Bits));
		    var decoded = new StringBuilder();

		    for (var i = 0; i < bin.Length; i += 4)
		    {
			    decoded.Append(BitsToChar(bin.Substring(i, 4)));
		    }

		    return Guid.Parse(decoded.ToString());
	    }

		private static string CharToBits(char c)
	    {
		    switch (c)
		    {
			    case '0': return "0000";
			    case '1': return "0001";
			    case '2': return "0010";
			    case '3': return "0011";
			    case '4': return "0100";
			    case '5': return "0101";
			    case '6': return "0110";
			    case '7': return "0111";
			    case '8': return "1000";
			    case '9': return "1001";
			    case 'a': return "1010";
			    case 'b': return "1011";
			    case 'c': return "1100";
			    case 'd': return "1101";
			    case 'e': return "1110";
			    case 'f': return "1111";

			    default: throw new ArgumentOutOfRangeException(nameof(c));
		    }
	    }

	    private static char BitsToChar(string c)
	    {
		    switch (c)
		    {
			    case "0000": return '0';
			    case "0001": return '1';
			    case "0010": return '2';
			    case "0011": return '3';
			    case "0100": return '4';
			    case "0101": return '5';
			    case "0110": return '6';
			    case "0111": return '7';
			    case "1000": return '8';
			    case "1001": return '9';
			    case "1010": return 'a';
			    case "1011": return 'b';
			    case "1100": return 'c';
			    case "1101": return 'd';
			    case "1110": return 'e';
			    case "1111": return 'f';

			    default: throw new ArgumentOutOfRangeException(nameof(c));
		    }
	    }

		private static char EncodeBitsToChar(string c)
	    {
		    switch (c)
		    {
			    case "00": return '*';
			    case "01": return '$';
			    case "10": return '=';
			    case "11": return '^';

			    case "000000": return '0';
			    case "000001": return '1';
			    case "000010": return '2';
			    case "000011": return '3';
			    case "000100": return '4';
			    case "000101": return '5';
			    case "000110": return '6';
			    case "000111": return '7';
			    case "001000": return '8';
			    case "001001": return '9';
			    case "001010": return 'A';
			    case "001011": return 'B';
			    case "001100": return 'C';
			    case "001101": return 'D';
			    case "001110": return 'E';
			    case "001111": return 'F';
			    case "010000": return 'G';
			    case "010001": return 'H';
			    case "010010": return 'I';
			    case "010011": return 'J';
			    case "010100": return 'K';
			    case "010101": return 'L';
			    case "010110": return 'M';
			    case "010111": return 'N';
			    case "011000": return 'O';
			    case "011001": return 'P';
			    case "011010": return 'Q';
			    case "011011": return 'R';
			    case "011100": return 'S';
			    case "011101": return 'T';
			    case "011110": return 'U';
			    case "011111": return 'V';
			    case "100000": return 'W';
			    case "100001": return 'X';
			    case "100010": return 'Y';
			    case "100011": return 'Z';
			    case "100100": return 'a';
			    case "100101": return 'b';
			    case "100110": return 'c';
			    case "100111": return 'd';
			    case "101000": return 'e';
			    case "101001": return 'f';
			    case "101010": return 'g';
			    case "101011": return 'h';
			    case "101100": return 'i';
			    case "101101": return 'j';
			    case "101110": return 'k';
			    case "101111": return 'l';
			    case "110000": return 'm';
			    case "110001": return 'n';
			    case "110010": return 'o';
			    case "110011": return 'p';
			    case "110100": return 'q';
			    case "110101": return 'r';
			    case "110110": return 's';
			    case "110111": return 't';
			    case "111000": return 'u';
			    case "111001": return 'v';
			    case "111010": return 'w';
			    case "111011": return 'x';
			    case "111100": return 'y';
			    case "111101": return 'z';
			    case "111110": return '-';
			    case "111111": return '_';

			    default: throw new ArgumentOutOfRangeException(nameof(c));
		    }
	    }

	    private static string DecodeCharTo6Bits(char c)
	    {
		    switch (c)
		    {
			    case '*': return "00";
			    case '$': return "01";
			    case '=': return "10";
			    case '^': return "11";

			    case '0': return "000000";
			    case '1': return "000001";
			    case '2': return "000010";
			    case '3': return "000011";
			    case '4': return "000100";
			    case '5': return "000101";
			    case '6': return "000110";
			    case '7': return "000111";
			    case '8': return "001000";
			    case '9': return "001001";
			    case 'A': return "001010";
			    case 'B': return "001011";
			    case 'C': return "001100";
			    case 'D': return "001101";
			    case 'E': return "001110";
			    case 'F': return "001111";
			    case 'G': return "010000";
			    case 'H': return "010001";
			    case 'I': return "010010";
			    case 'J': return "010011";
			    case 'K': return "010100";
			    case 'L': return "010101";
			    case 'M': return "010110";
			    case 'N': return "010111";
			    case 'O': return "011000";
			    case 'P': return "011001";
			    case 'Q': return "011010";
			    case 'R': return "011011";
			    case 'S': return "011100";
			    case 'T': return "011101";
			    case 'U': return "011110";
			    case 'V': return "011111";
			    case 'W': return "100000";
			    case 'X': return "100001";
			    case 'Y': return "100010";
			    case 'Z': return "100011";
			    case 'a': return "100100";
			    case 'b': return "100101";
			    case 'c': return "100110";
			    case 'd': return "100111";
			    case 'e': return "101000";
			    case 'f': return "101001";
			    case 'g': return "101010";
			    case 'h': return "101011";
			    case 'i': return "101100";
			    case 'j': return "101101";
			    case 'k': return "101110";
			    case 'l': return "101111";
			    case 'm': return "110000";
			    case 'n': return "110001";
			    case 'o': return "110010";
			    case 'p': return "110011";
			    case 'q': return "110100";
			    case 'r': return "110101";
			    case 's': return "110110";
			    case 't': return "110111";
			    case 'u': return "111000";
			    case 'v': return "111001";
			    case 'w': return "111010";
			    case 'x': return "111011";
			    case 'y': return "111100";
			    case 'z': return "111101";
			    case '-': return "111110";
			    case '_': return "111111";

			    default: throw new ArgumentOutOfRangeException(nameof(c));
		    }
	    }
	}
}
