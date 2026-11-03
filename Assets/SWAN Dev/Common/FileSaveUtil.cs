
using System;
using System.IO;
using UnityEngine;

namespace SDev
{
    /// <summary>
    /// Save byte array, Texture2D to file on current platform's Application data path or Windows/Mac special folder.
    /// </summary>
    public class FileSaveUtil
    {
        public enum AppPath
        {
            /// <summary> The directory path where you can store data that you want to be kept between runs. </summary>
            PersistentDataPath = 0,

            /// <summary> The directory path where temporary data can be stored. </summary>
            TemporaryCachePath,

            /// <summary> The folder located at /Assets/StreamingAssets in the project. (Not work with System.IO methods when running on Android/WebGL) </summary>
            StreamingAssetsPath,

            /// <summary> The folder located at /Assets in the project. (Work on the Unity editor only) </summary>
            DataPath
        }

        public string GetAppPath(AppPath appPath)
        {
            string directory = "";
            switch (appPath)
            {
                case AppPath.PersistentDataPath:
                    directory = Application.persistentDataPath;
                    break;
                case AppPath.TemporaryCachePath:
                    directory = Application.temporaryCachePath;
                    break;
                case AppPath.StreamingAssetsPath:
                    directory = Application.streamingAssetsPath;
                    break;
                case AppPath.DataPath:
                    directory = Application.dataPath;
                    break;
            }
            return directory;
        }


        #region ----- Instance -----
        private static FileSaveUtil _instance = null;
        public static FileSaveUtil Instance
        {
            get
            {
                if (_instance == null) _instance = new FileSaveUtil();
                return _instance;
            }
        }
        #endregion


        #region ----- Save Texture2D as EXR -----
        /// <summary>
        /// Saves Texture2D as EXR to accessible path (e.g. Application data path).
        /// </summary>
        public string SaveTextureAsEXR(Texture2D texture2D, string directory, string fileNameWithoutExtension, Texture2D.EXRFlags exrFlags = Texture2D.EXRFlags.None)
        {
            return SaveBytes(texture2D.EncodeToEXR(exrFlags), directory, fileNameWithoutExtension + ".exr");
        }

        /// <summary>
        /// Saves Texture2D as JPG to Application data path.
        /// </summary>
        public string SaveTextureAsEXR(Texture2D texture2D, AppPath appPath, string subFolderName, string fileNameWithoutExtension, Texture2D.EXRFlags exrFlags = Texture2D.EXRFlags.None)
        {
            string directory = GetAppPath(appPath);
            if (!string.IsNullOrEmpty(subFolderName)) directory = Path.Combine(directory, subFolderName);
            return SaveTextureAsEXR(texture2D, directory, fileNameWithoutExtension, exrFlags);
        }

        /// <summary>
        /// Saves Texture2D as JPG to Windows/Mac special folder.
        /// </summary>
        public string SaveTextureAsEXR(Texture2D texture2D, Environment.SpecialFolder specialFolder, string subFolderName, string fileNameWithoutExtension, Texture2D.EXRFlags exrFlags = Texture2D.EXRFlags.None)
        {
            string directory = Environment.GetFolderPath(specialFolder);
            if (!string.IsNullOrEmpty(subFolderName)) directory = Path.Combine(directory, subFolderName);
            return SaveTextureAsEXR(texture2D, directory, fileNameWithoutExtension, exrFlags);
        }
        #endregion


        #region ----- Save Texture2D as JPG -----
        /// <summary>
        /// Saves Texture2D as JPG to accessible path (e.g. Application data path).
        /// </summary>
        public string SaveTextureAsJPG(Texture2D texture2D, string directory, string fileNameWithoutExtension, int quality = 90)
        {
            return SaveBytes(texture2D.EncodeToJPG(quality), directory, fileNameWithoutExtension + ".jpg");
        }

        /// <summary>
        /// Saves Texture2D as JPG to Application data path.
        /// </summary>
        public string SaveTextureAsJPG(Texture2D texture2D, AppPath appPath, string subFolderName, string fileNameWithoutExtension, int quality = 90)
        {
            string directory = GetAppPath(appPath);
            if (!string.IsNullOrEmpty(subFolderName)) directory = Path.Combine(directory, subFolderName);
            return SaveTextureAsJPG(texture2D, directory, fileNameWithoutExtension, quality);
        }

        /// <summary>
        /// Saves Texture2D as JPG to Windows/Mac special folder.
        /// </summary>
        public string SaveTextureAsJPG(Texture2D texture2D, Environment.SpecialFolder specialFolder, string subFolderName, string fileNameWithoutExtension, int quality = 90)
        {
            string directory = Environment.GetFolderPath(specialFolder);
            if (!string.IsNullOrEmpty(subFolderName)) directory = Path.Combine(directory, subFolderName);
            return SaveTextureAsJPG(texture2D, directory, fileNameWithoutExtension, quality);
        }
        #endregion


        #region ----- Save Texture2D as PNG -----
        /// <summary>
        /// Saves Texture2D as PNG to accessible path (e.g. Application data path).
        /// </summary>
        public string SaveTextureAsPNG(Texture2D texture2D, string directory, string fileNameWithoutExtension)
        {
            return SaveBytes(texture2D.EncodeToPNG(), directory, fileNameWithoutExtension + ".png");
        }

        /// <summary>
        /// Saves Texture2D as PNG to Application data path.
        /// </summary>
        public string SaveTextureAsPNG(Texture2D texture2D, AppPath appPath, string subFolderName, string fileNameWithoutExtension)
        {
            string directory = GetAppPath(appPath);
            if (!string.IsNullOrEmpty(subFolderName)) directory = Path.Combine(directory, subFolderName);
            return SaveTextureAsPNG(texture2D, directory, fileNameWithoutExtension);
        }

        /// <summary>
        /// Saves Texture2D as PNG to Windows/Mac special folder.
        /// </summary>
        public string SaveTextureAsPNG(Texture2D texture2D, Environment.SpecialFolder specialFolder, string subFolderName, string fileNameWithoutExtension)
        {
            string directory = Environment.GetFolderPath(specialFolder);
            if (!string.IsNullOrEmpty(subFolderName)) directory = Path.Combine(directory, subFolderName);
            return SaveTextureAsPNG(texture2D, directory, fileNameWithoutExtension);
        }
        #endregion


        #region ----- Save byte array to File -----
        /// <summary>
        /// Saves file byte array to accessible path (e.g. Application data path).
        /// </summary>
        public string SaveBytes(byte[] bytes, string directory, string fileNameWithExtension)
        {
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            string savePath = Path.Combine(directory, fileNameWithExtension);
            File.WriteAllBytes(savePath, bytes);
            return savePath;
        }

        /// <summary>
        /// Saves file byte array to Application data path.
        /// </summary>
        public string SaveBytes(byte[] bytes, AppPath appPath, string subFolderName, string fileNameWithExtension)
        {
            string directory = GetAppPath(appPath);
            return SaveBytes(bytes, directory, fileNameWithExtension);
        }

        /// <summary>
        /// Saves file byte array to Windows/Mac special folder.
        /// </summary>
        public string SaveBytes(byte[] bytes, Environment.SpecialFolder specialFolder, string subFolderName, string fileNameWithExtension)
        {
            string directory = Path.Combine(Environment.GetFolderPath(specialFolder), subFolderName);
            return SaveBytes(bytes, directory, fileNameWithExtension);
        }

        /// <summary>
        /// Saves file byte array to Windows/Mac special folder.
        /// </summary>
        public string SaveBytes(byte[] bytes, Environment.SpecialFolder specialFolder, string fileNameWithExtension)
        {
            string directory = Environment.GetFolderPath(specialFolder);
            return SaveBytes(bytes, directory, fileNameWithExtension);
        }
        #endregion
    }
}
