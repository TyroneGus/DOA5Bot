using System.Diagnostics;
using System.Runtime.InteropServices;
using System;

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
    private IntPtr progressHandle;

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
            progressHandle = OpenProcess(PROCESS_ALL_ACCESS, false, Convert.ToUInt32(targetProcess.Id));
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
        if(ReadProcessMemory(progressHandle, address, buffer, size, out IntPtr bytesRead))
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
        List<IntPtr> results = new List<IntPtr>();
        IntPtr currentAddress = IntPtr.Zero;
        byte[] signatureByteArray = StringToByteArray(byteString);
        int chunkSize = 24096; // Example chunk size

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
    }
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
    
    /*
     在C#中，`Dispose`方法用于释放对象占用的资源，特别是那些非托管资源（如文件句柄、数据库连接等）。实现`Dispose`方法通常涉及以下步骤：

        1. **实现IDisposable接口**：你的类需要实现`IDisposable`接口。
        2. **实现Dispose方法**：在`Dispose`方法中，释放对象占用的资源。
        3. **调用Dispose方法**：在使用完对象后，显式调用`Dispose`方法。

        下面是一个简单的示例，演示如何实现和使用`Dispose`方法：
    */
}
