using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Threading;

using Umbraco.Core.IO;
using Umbraco.Core.Logging;
using Umbraco.Core.Sync;
using Umbraco.Core;

namespace Umbraco.Web.SyncFileSystem
{
    public class SyncFileSystemWrapper : IFileSystem
    {
        private readonly IFileSystem _wrapped;

        public bool CanAddPhysical => false;

        public SyncFileSystemWrapper(string virtualRoot)
        {
            _wrapped = new PhysicalFileSystem(virtualRoot);
        }

        public IEnumerable<string> GetDirectories(string path)
        {
            return _wrapped.GetDirectories(path);
        }

        public void DeleteDirectory(string path)
        {
            _wrapped.DeleteDirectory(path);

            SendMessages(Action.DeleteDirectory, GetUrl(path), null);
        }

        public void DeleteDirectory(string path, bool recursive)
        {
            _wrapped.DeleteDirectory(path, recursive);

            SendMessages(Action.DeleteDirectory, GetUrl(path), null);
        }

        public bool DirectoryExists(string path)
        {
            return _wrapped.DirectoryExists(path);
        }

        public void AddFile(string path, Stream stream)
        {
            var memoryCopy = new MemoryStream();
            stream.CopyTo(memoryCopy);

            _wrapped.AddFile(path, memoryCopy);

            SendMessages(Action.Add, GetUrl(path), memoryCopy);
        }

        public void AddFile(string path, Stream stream, bool overrideIfExists)
        {
            var memoryCopy = new MemoryStream();
            stream.CopyTo(memoryCopy);

            _wrapped.AddFile(path, memoryCopy, overrideIfExists);

            SendMessages(Action.Add, GetUrl(path), memoryCopy);
        }

        public void AddFile(string path, string physicalPath, bool overrideIfExists = true, bool copy = false)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetFiles(string path)
        {
            return _wrapped.GetFiles(path);
        }

        public IEnumerable<string> GetFiles(string path, string filter)
        {
            return _wrapped.GetFiles(path, filter);
        }

        public Stream OpenFile(string path)
        {
            return _wrapped.OpenFile(path);
        }

        public void DeleteFile(string path)
        {
            _wrapped.DeleteFile(path);

            SendMessages(Action.DeleteFile, GetUrl(path), null);
        }

        public bool FileExists(string path)
        {
            return _wrapped.FileExists(path);
        }

        public string GetRelativePath(string fullPathOrUrl)
        {
            return _wrapped.GetRelativePath(fullPathOrUrl);
        }

        public string GetFullPath(string path)
        {
            return _wrapped.GetFullPath(path);
        }

        public string GetUrl(string path)
        {
            return _wrapped.GetUrl(path);
        }

        public DateTimeOffset GetLastModified(string path)
        {
            return _wrapped.GetLastModified(path);
        }

        public DateTimeOffset GetCreated(string path)
        {
            return _wrapped.GetCreated(path);
        }

        private void SendMessages(Action action, string path, MemoryStream stream)
        {
            var dto = new CacheFileDto
            {
                UtcStamp = DateTime.UtcNow,
                Action = (int)action,
                Path = path,
                Data = stream?.ToArray() ?? new byte[] { }
            };

            using (var scope = Composing.Current.ScopeProvider.CreateScope())
            {
                scope.Database.Insert(dto);
                scope.Complete();
            }

            Composing.Current.Logger.Info(typeof(SyncFileSystemWrapper), "Sending file sync {0} for path {1}...", action, path);

            Composing.Current.DistributedCache.Refresh(Cache.FileSystemCacheRefresher.UniqueId, dto.Id);
        }

        public long GetSize(string path)
        {
            return _wrapped.GetSize(path);
        }

        public enum Action
        {
            Add = 1,
            DeleteFile = 2,
            DeleteDirectory = 3
        }

    }
}
