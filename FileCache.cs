using System;
using System.IO;
using System.Text;

namespace Ventimiglia.Helpers
{
    /// <summary>
    /// Reusable File Cache. I/O with expiration
    /// </summary>
    public class FileCache
    {

        //TODO COMPRESSION AND ASYNC

        public string Group { get; protected set; }

        public object _syncLock = new object();

        public FileCache(TimeSpan duration, string cacheGroup = "Images")
        {
            Group = cacheGroup;
            RemoveExpired(duration);
        }

        public string NameHash(string fName)
        {
            var bytes = Encoding.UTF8.GetBytes(fName);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Removes an item from the cache
        /// </summary>
        /// <param name="fname"></param>
        public void Remove(string fname)
        {
            var hash = NameHash(fname);
            if (Exists(fname))
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), Group);
                var filePath = Path.Combine(path, NameHash(hash));
                lock (_syncLock)
                {
                    File.Delete(filePath);
                }
            }
        }

        /// <summary>
        /// Deletes all items in this cache group
        /// </summary>
        public void Clear()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), Group);
            lock (_syncLock)
            {
                Directory.Delete(path);
            }
        }

        /// <summary>
        /// Removes all expires items from this cache group
        /// </summary>
        /// <param name="cacheTime"></param>
        public void RemoveExpired(TimeSpan cacheTime)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), Group);
            if (!Directory.Exists(path))
                return;

            string[] files;

            lock (_syncLock)
            {
                files = Directory.GetFiles(path);
            }

            foreach (var file in files)
            {
                var fInfo = new FileInfo(file);

                if (fInfo.LastWriteTimeUtc + cacheTime < DateTime.UtcNow)
                {
                    Remove(file);
                }
            }
        }

        /// <summary>
        /// Does the file exist ?
        /// </summary>
        /// <param name="fname"></param>
        /// <returns></returns>
        public bool Exists(string fname)
        {
            var hash = NameHash(fname);
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), Group);

            if (!Directory.Exists(path))
                return false;

            var filePath = Path.Combine(path, hash);

            if (!File.Exists(filePath))
                return false;

            return true;
        }

        /// <summary>
        /// Last write time of the cache item
        /// </summary>
        /// <param name="fname"></param>
        /// <returns></returns>
        public DateTime? LastWrite(string fname)
        {
            var hash = NameHash(fname);
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), Group);

            if (!Directory.Exists(path))
                return null;

            var filePath = Path.Combine(path, fname);

            if (!File.Exists(filePath))
                return null;

            return File.GetLastWriteTimeUtc(fname);
        }

        /// <summary>
        /// Returns file path
        /// </summary>
        /// <param name="fname"></param>
        /// <returns></returns>
        public string GetPath(string fname)
        {
            var hash = NameHash(fname);
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), Group);
            return Path.Combine(path, hash);
        }

        /// <summary>
        /// Write File
        /// </summary>
        /// <param name="fname"></param>
        /// <param name="data"></param>
        public void Write(string fname, byte[] data)
        {
            var hash = NameHash(fname);
            lock (_syncLock)
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), Group);

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);


                var filePath = Path.Combine(path, hash);

                try
                {
                    File.WriteAllBytes(filePath, data);
                }
                catch (Exception ex)
                {

                    throw ex;
                }
            }
        }

        /// <summary>
        /// Read File
        /// </summary>
        /// <param name="fname"></param>
        /// <returns></returns>
        public byte[] Read(string fname)
        {
            var hash = NameHash(fname);
            lock (_syncLock)
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), Group);

                if (!Directory.Exists(path))
                    return null;

                var filePath = Path.Combine(path, hash);

                if (!File.Exists(filePath))
                    return null;

                //Not working
                return File.ReadAllBytes(filePath);
            }
        }
    }
}
