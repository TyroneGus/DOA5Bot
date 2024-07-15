using System.Diagnostics;
using System.Runtime.InteropServices;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MemoryScanner;

/// <summary>
/// Memory Scanner
/// I add Sunday Algorithm to improve the search speed
/// Thank the tutorial By swedz c# https://youtu.be/-MDF6iMThB0
/// </summary>
public class MemScanner
{
    // class variables
    private Process targetProcess;
    private IntPtr processHandle;

    // desired rights
    private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;

    // constants for memory access
    private const uint MEM_COMMIT = 0x1000;
    private const uint MEM_RESERVE = 0x2000;
    private const uint PAGE_READWRITE = 0x00000004;
    private const uint PAGE_READONLY = 0x00000002;
    private const uint PAGE_EXECUTE_READWRITE = 0x00000040;
    private const uint PAGE_EXECUTE_READ = 0x00000020;

    public MemScanner(Process process)
    {
        // get the process
        targetProcess = process;
        try
        {
            processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, Convert.ToUInt32(targetProcess.Id));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
    }
    // Imports windows API kernal32.dll
    [DllImport("kernel32.dll")]
    public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll")] 
    public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

    public byte[] ReadMem(IntPtr address, int size)
    {
        byte[] buffer = new byte[size];
        if(ReadProcessMemory(processHandle, address, buffer, size, out IntPtr bytesRead))
        {
            return buffer;
        }
        else
        {
            return null;
        }
    }

    
    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORY_BASIC_INFORMATION
    {
        public IntPtr BaseAddress;
        public IntPtr AllocationBase;
        public uint AllocationProtect;
        public IntPtr RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }
    
    // Get info on memory page
    [DllImport("kernel32.dll")]
    private static extern bool VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

    // Convert byte string to byte array
    public static byte[] StringToByteArray(string hex)
    {
        string[] elements = hex.Split(" ");    // FF FF FF, <- split the spaces -> FF, FF, FF
        byte[] convertedBytes = new byte[elements.Length];
        for (int i = 0; i < elements.Length; i++)
        {
            if (elements[i].Contains("?")) // FF ?? FF <- our wildcard (??)
            {
                convertedBytes[i] = 0x0;
            }
            else
            {
                convertedBytes[i] = Convert.ToByte(elements[i], 16);
            }
        }
        return convertedBytes;
    }


    public List<IntPtr> ScanMemory(string byteString)
    {
        var results = new ConcurrentBag<IntPtr>();
        byte[] signatureByteArray = StringToByteArray(byteString);
        int chunkSize = 1024 * 1024; // 1MB chunks

        Parallel.ForEach(GetMemoryRegions(), region =>
        {
            if ((region.Protect == PAGE_READONLY || region.Protect == PAGE_READWRITE) && region.State == MEM_COMMIT)
            {
                IntPtr baseAddress = region.BaseAddress;
                // long regionSize = region.RegionSize.ToInt64();
                int regionSize = region.RegionSize.ToInt32();

                for (int offset = 0; offset < regionSize; offset += chunkSize)
                {
                    int sizeToRead = (int)Math.Min(chunkSize, regionSize - offset);
                    byte[] buffer = new byte[sizeToRead];
                    IntPtr bytesRead;
                    if (ReadProcessMemory(processHandle, baseAddress + offset, buffer, sizeToRead, out bytesRead))
                    {
                        int result = SundayAlgorithm.SundaySearch(buffer, signatureByteArray); 
                        // int result = KmpSearch(buffer, signatureByteArray);
                        if (result != -1)
                        {
                            results.Add(baseAddress + offset + result);
                        }
                        
                        // List<int> matches = SimdSundayScanner.Search(buffer, signatureByteArray);
                        // List<int> matches = SearchWithWildcards(buffer, signatureByteArray);
                        // foreach (int match in matches)
                        // {
                        //     results.Add(baseAddress + offset + match);
                        // }
                    }
                }
            }
        });

        return results.ToList();
    }

private IEnumerable<MEMORY_BASIC_INFORMATION> GetMemoryRegions()
{
    IntPtr address = IntPtr.Zero;
    while (VirtualQueryEx(processHandle, address, out MEMORY_BASIC_INFORMATION mbi, (uint)Marshal.SizeOf<MEMORY_BASIC_INFORMATION>()))
    {
        yield return mbi;
        address = new IntPtr(address.ToInt64() + mbi.RegionSize.ToInt64());
    }
}

private List<int> SearchWithWildcards(byte[] haystack, byte[] needle)
{
    List<int> results = new List<int>();
    for (int i = 0; i <= haystack.Length - needle.Length; i++)
    {
        bool match = true;
        for (int j = 0; j < needle.Length; j++)
        {
            if (needle[j] != 0x0 && needle[j] != haystack[i + j])
            {
                match = false;
                break;
            }
        }
        if (match)
        {
            results.Add(i);
        }
    }
    return results;
}

// KMP search algorithm implementation
/*private static int KmpSearch(byte[] text, byte[] pattern)
{
    int[] lps = ComputeLPSArray(pattern);
    int i = 0, j = 0;

    while (i < text.Length)
    {
        if (pattern[j] == text[i])
        {
            i++;
            j++;
        }

        if (j == pattern.Length)
        {
            return i - j;
        }
        else if (i < text.Length && pattern[j] != text[i])
        {
            if (j != 0)
                j = lps[j - 1];
            else
                i++;
        }
    }

    return -1;
}

private static int[] ComputeLPSArray(byte[] pattern)
{
    int[] lps = new int[pattern.Length];
    int len = 0;
    int i = 1;

    while (i < pattern.Length)
    {
        if (pattern[i] == pattern[len])
        {
            len++;
            lps[i] = len;
            i++;
        }
        else
        {
            if (len != 0)
            {
                len = lps[len - 1];
            }
            else
            {
                lps[i] = 0;
                i++;
            }
        }
    }

    return lps;
}*/
    
   /* public List<IntPtr> ScanMemory(string byteString)
    {
        List<IntPtr> results = new List<IntPtr>();
        IntPtr currentAddress = IntPtr.Zero;
        byte[] signatureByteArray = StringToByteArray(byteString);
        int chunkSize = 4096; // Example chunk size

        while (VirtualQueryEx(progressHandle, currentAddress, out MEMORY_BASIC_INFORMATION memoryInfo, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))))
        {
            if ((memoryInfo.Protect != PAGE_READONLY || memoryInfo.Protect != PAGE_READWRITE) && memoryInfo.State == MEM_COMMIT)
            {
                int regionSize = memoryInfo.RegionSize.ToInt32();
                for (int offset = 0; offset < regionSize; offset += chunkSize)
                {
                    int sizeToRead = (int)Math.Min(chunkSize, regionSize - offset);
                    byte[] buffer = new byte[sizeToRead];
                    if (ReadProcessMemory(progressHandle, currentAddress + offset, buffer, sizeToRead, out IntPtr bytesRead))
                    {
                        int result = SundayAlgorithm.SundaySearch(buffer, signatureByteArray);
                        if (result != -1)
                        {
                            results.Add(currentAddress + offset + result);
                        }
                    }
                }
            }
            currentAddress = new IntPtr(currentAddress.ToInt64() + memoryInfo.RegionSize.ToInt64());
        }

        return results;
    }*/
   
      // memory scanner cause near 800MB memory use
    /*public List<IntPtr> ScanMemory(string byteString)
    {
        List<IntPtr> results = new List<IntPtr>();
        IntPtr currentAddress = IntPtr.Zero;    // starting address in iteration
        IntPtr bytesRead = 0;  // for chunk reading
        byte[] signatureByteArray = StringToByteArray(byteString);  // convert byte string to byte array
        
        // iterate through memory
        while (VirtualQueryEx(progressHandle, currentAddress, out MEMORY_BASIC_INFORMATION memoryInfo, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))))
        {
            if ((memoryInfo.Protect != PAGE_READONLY || memoryInfo.Protect != PAGE_READWRITE) && memoryInfo.State == MEM_COMMIT)
            {
                byte[] buffer = new byte[memoryInfo.RegionSize.ToInt32()];
                if (ReadProcessMemory(progressHandle, memoryInfo.BaseAddress, buffer, buffer.Length, out bytesRead))
                {
                    // Brute force algorithm is very slow
                    // make sure to only read inside the boundaries
                    // for (int i = 0; i < bytesRead - signatureByteArray.Length; i++)
                    // {
                    //     bool match = true;
                    //     for(int j=0; j<signatureByteArray.Length; j++)
                    //     {
                    //         // chunk bytes compared to signature and ignore wildcard (0)
                    //         if (buffer[i + j] != signatureByteArray[j] && signatureByteArray[j] != 0)
                    //         {
                    //             match = false;
                    //             break;
                    //         }
                    //     }
                    //     if (match)
                    //     {
                    //         results.Add(memoryInfo.BaseAddress + i);
                    //     }
                    // }
                    
                    
                    int result = SundayAlgorithm.SundaySearch(buffer, signatureByteArray); // call SundaySearch()
                    if (result != -1)
                    {
                        // Console.WriteLine("Pattern found at index " + result);
                        results.Add(memoryInfo.BaseAddress + result); // add result to list();
                    }
                    // else
                    // {
                    //     Console.WriteLine("Pattern not found");
                    // }
                }
            }
            currentAddress = new IntPtr(currentAddress.ToInt64() + memoryInfo.RegionSize.ToInt64());
        }
        
        return results;
    }*/
    
}


/*
public unsafe class SimdSundayScanner
{
    private const int ALPHABET_SIZE = 256;

    public static List<int> Search(byte[] haystack, byte[] needle)
    {
        List<int> results = new List<int>();
        int needleLength = needle.Length;
        int haystackLength = haystack.Length;

        if (needleLength == 0)
            return results;

        // Preprocess the needle
        int[] shift = new int[ALPHABET_SIZE];
        for (int i = 0; i < ALPHABET_SIZE; i++)
            shift[i] = needleLength + 1;
        for (int i = 0; i < needleLength; i++)
            if (needle[i] != 0x0) // Not a wildcard
                shift[needle[i]] = needleLength - i;

        if (Avx2.IsSupported && needleLength <= 32)
        {
            Vector256<byte> needleVector = Vector256<byte>.Zero;
            Vector256<byte> wildcardMask = Vector256<byte>.Zero;

            fixed (byte* pNeedle = needle)
            {
                for (int i = 0; i < needleLength; i++)
                {
                    if (pNeedle[i] != 0x0) // Not a wildcard
                    {
                        needleVector = Avx2.InsertVector128(needleVector, Vector128.Create(pNeedle[i]), i / 16);
                        wildcardMask = Avx2.InsertVector128(wildcardMask, Vector128.Create((byte)0xFF), i / 16);
                    }
                }
            }

            fixed (byte* pHaystack = haystack)
            {
                int i = 0;
                while (i <= haystackLength - needleLength)
                {
                    if (i + 32 <= haystackLength)
                    {
                        Vector256<byte> haystackVector = Avx.LoadVector256(pHaystack + i);
                        Vector256<byte> comparison = Avx2.And(Avx2.CompareEqual(haystackVector, needleVector), wildcardMask);
                        uint mask = (uint)Avx2.MoveMask(comparison);

                        if ((mask & ((1u << needleLength) - 1)) == ((1u << needleLength) - 1))
                        {
                            results.Add(i);
                        }
                    }
                    else
                    {
                        bool match = true;
                        for (int j = 0; j < needleLength; j++)
                        {
                            if (needle[j] != 0x0 && needle[j] != pHaystack[i + j])
                            {
                                match = false;
                                break;
                            }
                        }
                        if (match)
                        {
                            results.Add(i);
                        }
                    }

                    if (i + needleLength < haystackLength)
                        i += shift[pHaystack[i + needleLength]];
                    else
                        break;
                }
            }
        }
        else
        {
            // Fallback to standard Sunday algorithm for longer patterns or if AVX2 is not supported
            int i = 0;
            while (i <= haystackLength - needleLength)
            {
                bool match = true;
                for (int j = 0; j < needleLength; j++)
                {
                    if (needle[j] != 0x0 && needle[j] != haystack[i + j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    results.Add(i);
                }

                if (i + needleLength < haystackLength)
                    i += shift[haystack[i + needleLength]];
                else
                    break;
            }
        }

        return results;
    }
}*/