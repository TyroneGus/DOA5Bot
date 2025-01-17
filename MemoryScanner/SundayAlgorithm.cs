﻿namespace MemoryScanner;

using System;


using System.Collections.Generic;


/// <summary>
/// Sunday Algorithm generated by GPT4o
/// </summary>
class SundayAlgorithm
{
    public static int SundaySearch(byte[] text, byte[] pattern)
    {
        int n = text.Length;
        int m = pattern.Length;

        if (m == 0) return 0; // 空模式，默认匹配第一个位置

        // 构建偏移表
        Dictionary<byte, int> shiftTable = BuildShiftTable(pattern);

        int i = 0; // text 中的起始位置
        while (i <= n - m)
        {
            int j = 0;
            // 从左到右比较 pattern 和 text 的当前窗口
            while (j < m && pattern[j] == text[i + j])
            {
                j++;
            }

            // 如果 j 等于模式长度，说明找到了匹配
            if (j == m)
            {
                return i; // 返回匹配的起始位置
            }

            // 如果没有匹配，计算下一个跳跃距离
            if (i + m < n)
            {
                byte nextChar = text[i + m];
                i += shiftTable.ContainsKey(nextChar) ? shiftTable[nextChar] : m + 1;
            }
            else
            {
                break;
            }
        }

        return -1; // 如果没有找到匹配，返回 -1
    }

    // 构建偏移表
    private static Dictionary<byte, int> BuildShiftTable(byte[] pattern)
    {
        int m = pattern.Length;
        var shiftTable = new Dictionary<byte, int>();

        for (int i = 0; i < m; i++)
        {
            shiftTable[pattern[i]] = m - i;
        }

        return shiftTable;
    }

    /*static void Main(string[] args)
    {
        byte[] text = { 0x41, 0x42, 0x43, 0x44, 0x41, 0x42, 0x43 }; // 示例文本，表示 ABCDABC
        byte[] pattern = { 0x42, 0x43 }; // 示例模式，表示 BC

        int result = SundaySearch(text, pattern);
        if (result != -1)
        {
            Console.WriteLine("Pattern found at index " + result);
        }
        else
        {
            Console.WriteLine("Pattern not found");
        }
    }*/
}



/// <summary>
/// DeepSeekCoder V2 generated
/// 
/// 这个程序实现了Sunday查找算法，用于在byte数组中查找子数组。SundaySearchBytes方法接受两个byte数组作为参数：text是要搜索的文本，pattern是要查找的模式。如果找到匹配的子数组，则返回其在文本中的起始索引；如果没有找到，则返回-1。
/// 在Main方法中，我们提供了一个简单的测试用例来验证算法的正确性。
///
/// This program implements the Sunday search algorithm for finding subarrays in byte arrays. The SundaySearchBytes method accepts two byte arrays as parameters: text is the text to be searched, and pattern is the pattern to be found. If a matching subarray is found, its starting index in the text is returned; if not found, -1 is returned.
/// In the Main method, we provide a simple test case to verify the correctness of the algorithm.
/// </summary>
/*public class SundaySearch
{
    public static int SundaySearchBytes(byte[] text, byte[] pattern)
    {
        int textLength = text.Length;
        int patternLength = pattern.Length;

        if (patternLength == 0)
        {
            return 0;
        }

        if (textLength < patternLength)
        {
            return -1;
        }

        // Create a shift table for the pattern
        int[] shiftTable = new int[256];
        for (int i = 0; i < 256; i++)
        {
            shiftTable[i] = patternLength + 1;
        }

        for (int i = 0; i < patternLength; i++)
        {
            shiftTable[pattern[i]] = patternLength - i;
        }

        int textIndex = 0;
        while (textIndex <= textLength - patternLength)
        {
            int patternIndex = 0;
            while (patternIndex < patternLength && text[textIndex + patternIndex] == pattern[patternIndex])
            {
                patternIndex++;
            }

            if (patternIndex == patternLength)
            {
                return textIndex;
            }

            if (textIndex + patternLength < textLength)
            {
                textIndex += shiftTable[text[textIndex + patternLength]];
            }
            else
            {
                break;
            }
        }

        return -1;
    }

    public static void Main(string[] args)
    {
        byte[] text = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 };
        byte[] pattern = { 4, 5, 6, 15 };
        
        int result = SundaySearchBytes(text, pattern);
        if (result != -1)
        {
            Console.WriteLine("Pattern found at index: " + result);
        }
        else
        {
            Console.WriteLine("Pattern not found");
        }
    }
}*/

