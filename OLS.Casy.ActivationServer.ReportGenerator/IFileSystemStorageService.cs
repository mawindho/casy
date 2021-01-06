using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.ActivationServer.ReportGenerator
{
    public interface IFileSystemStorageService
    {
        string[] GetFiles(string directoryPath);

        /// <summary>
        /// Creates async a file with the passed path and binary data
        /// </summary>
        /// <param name="path">Filepath of the file to be created</param>
        /// <param name="data">Binary data of the newly created file</param>
        Task CreateFileAsync(string path, byte[] data);

        /// <summary>
        /// Copys async a file from source to destination path
        /// </summary>
        /// <param name="sourcePath">Source path</param>
        /// <param name="destinationPath">Destination path</param>
        Task CopyFileAsync(string sourcePath, string destinationPath);

        /// <summary>
        /// Deletes async the file with the passed path
        /// </summary>
        /// <param name="sourcePath">Path of the file to delete</param>
        Task<bool> DeleteFileAsync(string sourcePath);

        /// <summary>
        /// Reads async the binary content of the file with the passed path
        /// </summary>
        /// <param name="path">Path of the file to be read</param>
        /// <returns>Binary content of the file</returns>
        Task<byte[]> ReadFileAsync(string path);

        /// <summary>
        /// Reads async the XML content of the file with the passed path
        /// </summary>
        /// <param name="fileName">Path od the file to be read</param>
        /// <returns>XML content of the file</returns>
        Task<string> ReadXmlFileAsync(string fileName);

        /// <summary>
        /// Deletes async the directory with the passed path
        /// </summary>
        /// <param name="sourcePath">Path of the directory to be delete</param>
        /// <param name="recursive">Delete recursivly or not</param>
        /// <returns>True, if the deletion was successful, false otherwise</returns>
        Task<bool> DeleteDirectoryAsync(string sourcePath, bool recursive);

        /// <summary>
        /// Creates async the directory with the passed path
        /// </summary>
        /// <param name="name">Path of the directory to be created</param>
        /// <returns>True, if the creation was successful, false otherwise</returns>
        Task<bool> CreateDirectoryAsync(string name);

        /// <summary>
        /// Creates async a file with the passed path and XML data
        /// </summary>
        /// <param name="path">Path of the file to be created</param>
        /// <param name="data">XML data of the newly created file</param>
        Task CreateXmlFileAsync(string fileName, string content);
    }
}
