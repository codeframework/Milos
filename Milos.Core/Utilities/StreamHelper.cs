using System;
using System.IO;
using System.Text;

namespace Milos.Core.Utilities
{
    /// <summary>
    /// This class can be used to perform common stream operations, such as converting a stream to a string
    /// </summary>
    public static class StreamHelper
    {
        /// <summary>
        /// Turns a stream into a string
        /// </summary>
        /// <param name="streamToConvert">Input stream</param>
        /// <returns>Output String</returns>
        public static string ToString(Stream streamToConvert)
        {
            var retVal = string.Empty;
            var stream = streamToConvert;

            stream.Position = 0;
            if (stream.CanRead && stream.CanSeek)
            {
                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int) stream.Length);
                retVal = Encoding.UTF8.GetString(buffer);
            }

            return retVal;
        }

        /// <summary>
        /// Turns a stream into a byte array
        /// </summary>
        /// <param name="streamToConvert">Input stream</param>
        /// <returns>Output array</returns>
        public static byte[] ToArray(Stream streamToConvert)
        {
            var stream = streamToConvert;

            stream.Position = 0;
            byte[] retVal = null;
            if (stream.CanRead && stream.CanSeek)
            {
                retVal = new byte[stream.Length];
                stream.Read(retVal, 0, (int) stream.Length);
            }

            return retVal;
        }

        /// <summary>
        /// Turns a string into a stream
        /// </summary>
        /// <param name="stringToConvert">Input string</param>
        /// <returns>Output stream</returns>
        public static Stream FromString(string stringToConvert)
        {
            var bytes = Encoding.UTF8.GetBytes(stringToConvert);
            using (var stream = new MemoryStream(stringToConvert.Length))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Position = 0;
                return stream;
            }
        }

        /// <summary>
        /// Turns a byte array into a stream
        /// </summary>
        /// <param name="arrayToConvert">The array to convert.</param>
        /// <returns>Output stream</returns>
        public static Stream FromArray(byte[] arrayToConvert)
        {
            using (var stream = new MemoryStream(arrayToConvert.Length))
            {
                stream.Write(arrayToConvert, 0, arrayToConvert.Length);
                stream.Position = 0;
                return stream;
            }
        }

        /// <summary>
        /// Writes a stream to file
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="overrideExisting">If set to <c>true</c> override existing file.</param>
        /// <returns>True if successful</returns>
        public static bool ToFile(Stream stream, string fileName, bool overrideExisting = true) => ToFile(stream, fileName, overrideExisting, Encoding.Default);

        /// <summary>
        /// Writes a stream to file
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="overrideExisting">If set to <c>true</c> override existing file.</param>
        /// <param name="encoding">The file encoding.</param>
        /// <returns>True if successful</returns>
        public static bool ToFile(Stream stream, string fileName, bool overrideExisting, Encoding encoding)
        {
            if (File.Exists(fileName))
            {
                if (overrideExisting)
                    //If so then Erase the file first as in this case we are overwriting
                    File.Delete(fileName);
                else
                    throw new AccessViolationException("File '@fileName' already exists.");
            }

            try
            {
                //Create the file if it does not exist and open it
                stream.Position = 0;
                using (var fileStream = new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite))
                using (var reader = new BinaryReader(stream))
                using (var writer = new BinaryWriter(fileStream, encoding))
                {
                    writer.Write(reader.ReadBytes((int) stream.Length));
                    writer.Flush();
                    writer.Close();
                    reader.Close();
                    fileStream.Close();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Loads a stream from a file
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>Stream</returns>
        /// <remarks>
        /// The returned stream is a memory stream that is not connected to the file.
        /// In other words: After this method completes, the file is closed and can be
        /// accessed by other means.
        /// </remarks>
        public static Stream FromFile(string fileName)
        {
            using (var fileStream = File.OpenRead(fileName))
            {
                var fileLength = (int) fileStream.Length;
                var fileBytes = new byte[fileLength];
                fileStream.Read(fileBytes, 0, fileLength);
                fileStream.Close();
                return FromArray(fileBytes);
            }
        }
    }
}