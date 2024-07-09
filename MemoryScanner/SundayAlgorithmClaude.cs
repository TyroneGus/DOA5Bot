using System;
using System.Collections.Generic;

/// <summary>
/// Claude 3.5 Sonnet generated Sunday Algorithm
/// </summary>
public class SundayAlgorithmClaude
{
    public static List<int> Search(string text, string pattern)
    {
        List<int> result = new List<int>();
        int textLength = text.Length;
        int patternLength = pattern.Length;

        if (patternLength > textLength)
            return result;

        // Preprocessing: Build the shift table
        Dictionary<char, int> shiftTable = new Dictionary<char, int>();
        for (int m = 0; m < patternLength; m++)
        {
            shiftTable[pattern[m]] = patternLength - m;
        }

        int i = 0;
        while (i <= textLength - patternLength)
        {
            int j;
            for (j = 0; j < patternLength; j++)
            {
                if (text[i + j] != pattern[j])
                    break;
            }

            if (j == patternLength)
            {
                result.Add(i);
            }

            if (i + patternLength < textLength)
            {
                char nextChar = text[i + patternLength];
                i += shiftTable.ContainsKey(nextChar) ? shiftTable[nextChar] : patternLength + 1;
            }
            else
            {
                break;
            }
        }

        return result;
    }

    public static void Main(string[] args)
    {
        string text = "GCATCGCAGAGAGTATACAGTACG";
        string pattern = "GCAGAGAG";

        List<int> matches = Search(text, pattern);

        Console.WriteLine($"Pattern '{pattern}' found at positions:");
        foreach (int pos in matches)
        {
            Console.WriteLine(pos);
        }
    }
}
