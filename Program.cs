/*
* FILE : Program.cs
* PROJECT : Tasks
* PROGRAMMER : RUDRA NITESHKUMAR BHATT
* FIRST VERSION : 2024-11-05
* DESCRIPTION :
* This application builds a C# multi-threaded file writer. Until the file reaches a certain size. 
* It sends random strings to the specified file. The application has features for controlling writing 
* tasks using a cancellation signal and delayed tasks, as well as for monitoring file size.
*/

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultiThreadedFileWriter
{
    class Program
    {
        private static readonly object _fileLock = new object(); 

        static async Task Main(string[] args)
        {
            
            if (args.Length != 2 || args[0] == "/?")
            {
                Console.WriteLine("Usage: MultiThreadedFileWriter <filename> <targetSize>");
                Console.WriteLine("       filename: Name of the file to write to.");
                Console.WriteLine("       targetSize: Size of the file to be created (between 1,000 and 20,000,000 characters).");
                return;
            }

            string filename = args[0];
            if (!int.TryParse(args[1], out int targetSize) || targetSize < 1000 || targetSize > 20000000)
            {
                Console.WriteLine("Error: The target size must be between 1,000 and 20,000,000 characters.");
                return;
            }

            // Check if file exists
            if (File.Exists(filename))
            {
                Console.Write($"The file '{filename}' already exists. Do you want to overwrite it? (y/n): ");
                if (Console.ReadLine()?.ToLower() != "y")
                {
                    Console.WriteLine("Operation cancelled.");
                    return;
                }
                File.Delete(filename);
            }

            // Initializing cancellation 
            CancellationTokenSource cts = new CancellationTokenSource();

            try
            {
                // Starting the file size monitor task
                Task monitorTask = Task.Run(() => MonitorFileSize(filename, targetSize, cts.Token));

                
                using (StreamWriter writer = new StreamWriter(filename, true, Encoding.UTF8))
                {
                    Task[] writerTasks = new Task[25];
                    for (int i = 0; i < writerTasks.Length; i++)
                    {
                        writerTasks[i] = Task.Run(() => WriteRandomDataToFile(writer, cts.Token));
                    }

                    
                    await Task.WhenAll(writerTasks);
                    cts.Cancel();
                    await monitorTask;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Target file size reached. Final file size displayed.");
            }
        }

        //
        // FUNCTION : WriteRandomDataToFile
        // DESCRIPTION : This function uses a common StreamWriter to write 36-character random data strings to the file.
        // Until the cancellation signal tells it to stop, it keeps writing.
        // PARAMETERS :
        // StreamWriter writer 
        // CancellationToken token 
        // RETURNS :
        // Task : When writing stops and a delayed task finishes

        static async Task WriteRandomDataToFile(StreamWriter writer, CancellationToken token)
        {
            try
            {
                Random random = new Random();
                while (!token.IsCancellationRequested)
                {
                    string data = GenerateRandomString(36, random);

                    
                    lock (_fileLock)
                    {
                        writer.WriteLine(data);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to file: {ex.Message}");
            }
        }

        //
        // FUNCTION : MonitorFileSize
        // DESCRIPTION :
        // At intervals, this program checks the file size. When the file size reaches or passes the specified target size,
        // it stops displaying the file size.
        // PARAMETERS :
        // string filename 
        // int maxSize 
        // CancellationToken token 
        // RETURNS :
        // Task : A delayed process that ends when the desired file size is reached
        //
        static async Task MonitorFileSize(string filename, int maxSize, CancellationToken token)
        {
            FileInfo file = new FileInfo(filename);
            try
            {
                while (!token.IsCancellationRequested)
                {
                    file.Refresh(); 
                    Console.WriteLine($"Current file size: {file.Length}");

                    if (file.Length >= maxSize)
                    {
                        Console.WriteLine($"File reached target size: {file.Length}");
                        break;
                    }
                    await Task.Delay(100); 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error monitoring file: {ex.Message}");
            }
        }

        //
        // FUNCTION : GenerateRandomString
        // DESCRIPTION : A random alphanumeric string of the given length is produced by this function.
        // PARAMETERS :
        // int length 
        // Random random
        // RETURNS :
        // string : A string of the given length that is created randomly
        //
        static string GenerateRandomString(int length, Random random)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            char[] buffer = new char[length];
            for (int i = 0; i < length; i++)
            {
                buffer[i] = chars[random.Next(chars.Length)];
            }
            return new string(buffer);
        }
    }
}
