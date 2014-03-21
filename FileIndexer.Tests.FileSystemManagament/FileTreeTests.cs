using FileIndexer.FileSystemManagement;
using NUnit.Framework;
using System.IO;
using System.Threading;

namespace FileIndexer.Tests.FileSystemManagament
{
    [TestFixture]
    public sealed class FileTreeTests
    {
        private static readonly string Folder = Path.GetTempPath();
        private readonly string _tmpFolderPath = Path.Combine(Folder, @"tst\a\b");
        private readonly string _tmpFolderPath2 = Path.Combine(Folder, @"tst\a\g");
        private readonly string _tmpFolderPath3 = Path.Combine(Folder, @"tst2\r");
        private readonly string _tmpFolderPath4 = Path.Combine(Folder, @"tst\o");
        private readonly string _tmpFileName = Path.Combine(Folder, @"tst\a\2.txt");
        private readonly string _anotherTmpFileName = Path.Combine(Folder, @"tst\a\1.txt");
        private readonly string _oneMoreTmpFileName = Path.Combine(Folder, @"tst\a\3.txt");
        private readonly string _tmpFileName4 = Path.Combine(Folder, @"tst\a\g\4.txt");
        private readonly string _tmpFileName5 = Path.Combine(Folder,@"tst2\r\5.txt");
        private readonly string _tmpFileName6 = Path.Combine(Folder, @"tst\o\6.txt");
        private readonly string _rootDirectory = Path.Combine(Folder, "tst");
        private readonly string _rootDirectory2 = Path.Combine(Folder, "tst2");
        

        [TestFixtureSetUp]
        public void SetUpDirectoryStructure()
        {
            if(Directory.Exists(_rootDirectory))
                Directory.Delete(_rootDirectory, true);
            if(Directory.Exists(_rootDirectory2))
                Directory.Delete(_rootDirectory2, true);
            Directory.CreateDirectory(_tmpFolderPath);
            Directory.CreateDirectory(_tmpFolderPath2);
            Directory.CreateDirectory(_tmpFolderPath3);
            Directory.CreateDirectory(_tmpFolderPath4);
            File.WriteAllText(_tmpFileName, "abc");
            File.WriteAllText(_anotherTmpFileName, "cde");
            File.WriteAllText(_oneMoreTmpFileName, "efg");
            File.WriteAllText(_tmpFileName4, "ghi");
            File.WriteAllText(_tmpFileName5, "jkl");
            File.WriteAllText(_tmpFileName6, "uuu");
        }


        // TODO: FileTree.Unhook()

        //[TestFixtureTearDown]
        //public void CleanUp()
        //{
        //    Directory.Delete(_rootDirectory, true);  
        //    Directory.Delete(_rootDirectory2, true);  
        //}

        [Test]
        public void FileAddedInMonitoredFolderTest()
        {
            string added = null;
            var manualEvent = new ManualResetEvent(false);
            var fileTree = new FileTree();
            string addedFilePath = Path.Combine(_tmpFolderPath, "newfile.txt").ToLower();
            fileTree.FileAdded += path =>
                {
                    if (path == addedFilePath)
                    {
                        added = path;
                        manualEvent.Set();
                    }
                };
            string directoryToObserve = _rootDirectory;
            
            fileTree.AddDirectory(directoryToObserve);
            File.WriteAllText(addedFilePath, "!!!");

            manualEvent.WaitOne(1000, false);
            Assert.AreEqual(addedFilePath.ToLower(), added);
        }

        [Test]
        public void FileChangedEventInMonitoredFolderTest()
        {
            string changedFileName = null;
            var manualEvent = new ManualResetEvent(false);
            var fileTree = new FileTree();
            fileTree.FileChanged += path =>
                {
                    changedFileName = path;
                    manualEvent.Set();
                };
            string directoryToObserve = _rootDirectory;
            string fileFullPath = _tmpFileName;
            fileTree.AddDirectory(directoryToObserve);
            File.AppendAllText(fileFullPath, "!");

            manualEvent.WaitOne(1000, false);
            Assert.AreEqual(fileFullPath.ToLower(), changedFileName);
        }

        [Test]
        public void FileChangedEventInNotMonitoredFolderTest()
        {
            string changedFileName = null;
            var manualEvent = new ManualResetEvent(false);
            var fileTree = new FileTree();
            fileTree.FileChanged += path =>
                {
                    changedFileName = path;
                    manualEvent.Set();
                };
            string directoryToObserve = _tmpFolderPath;
            string fileFullPath = _tmpFileName;
            fileTree.AddDirectory(directoryToObserve);
            File.AppendAllText(fileFullPath, "!");

            manualEvent.WaitOne(500, false);
            Assert.AreEqual(null, changedFileName);
        }

        [Test]
        public void FileRemovedInMonitoredFolderTest()
        {
            string removed = null;
            var manualEvent = new ManualResetEvent(false);
            var fileTree = new FileTree();
            fileTree.FileRemoved += path =>
                {
                    removed = path;
                    manualEvent.Set();
                };
            string directoryToObserve = _rootDirectory;
            string fileFullPath = _anotherTmpFileName;
            fileTree.AddDirectory(directoryToObserve);
            File.Delete(fileFullPath);

            manualEvent.WaitOne(1000, false);
            Assert.AreEqual(fileFullPath.ToLower(), removed);
        }

        [Test]
        public void FileRenamedInMonitoredFolderTest()
        {
            string oldPath = null;
            string newPath = null;
            var manualEvent = new ManualResetEvent(false);
            var fileTree = new FileTree();
            fileTree.FileRenamed += (s1, s2) =>
                {
                    oldPath = s1;
                    newPath = s2;
                    manualEvent.Set();
                };
            string directoryToObserve = _rootDirectory;
            string fileFullPath = _oneMoreTmpFileName;
            fileTree.AddDirectory(directoryToObserve);
            string newTmpFileName = Path.Combine(Folder, @"tst\a\z.txt");
            File.Move(fileFullPath, newTmpFileName);

            manualEvent.WaitOne(1000, false);
            Assert.AreEqual(fileFullPath.ToLower(), oldPath);
            Assert.AreEqual(newTmpFileName.ToLower(), newPath);
        }

        [Test]
        public void FolderRemovedFileRemovedEventTest()
        {
            string path = null;
            var manualEvent = new ManualResetEvent(false);
            var fileTree = new FileTree();
            fileTree.FileRemoved += oldPath =>
            {
                path = oldPath;
                manualEvent.Set();
            };
            string directoryToObserve = _rootDirectory2;
            fileTree.AddDirectory(directoryToObserve);
            var removed = _tmpFileName5.ToLower();

            Directory.Delete(_tmpFolderPath3, true);

            manualEvent.WaitOne(1000, false);
            Assert.AreEqual(removed, path);
        }


        [Test]
        public void FolderRenamedFileRenamedEventTest()
        {
            string path = null;
            var manualEvent = new ManualResetEvent(false);
            var fileTree = new FileTree();
            fileTree.FileRenamed += (oldPath, newPath) =>
            {
                path = newPath;
                manualEvent.Set();
            };
            string directoryToObserve = _rootDirectory;
            fileTree.AddDirectory(directoryToObserve);
            var updatedFilePath = Path.Combine(Folder, @"tst\a\q\4.txt").ToLower();

            Directory.Move(_tmpFolderPath2, Path.Combine(Folder, @"tst\a\q"));

            manualEvent.WaitOne(1000, false);
            Assert.AreEqual(updatedFilePath, path);
        }

        [Test]
        public void FolderRenamedFileObservedTest()
        {
            string path = null;
            var manualEvent = new ManualResetEvent(false);
            var fileTree = new FileTree();
            fileTree.FileChanged += changedPath =>
            {
                path = changedPath;
                manualEvent.Set();
            };
            string directoryToObserve = _rootDirectory;
            fileTree.AddDirectory(directoryToObserve);
            var updatedFilePath = Path.Combine(Folder, @"tst\x\6.txt").ToLower();

            Directory.Move(_tmpFolderPath4, Path.Combine(Folder, @"tst\x"));
            Thread.Sleep(200);
            File.AppendAllText(updatedFilePath, "?");

            manualEvent.WaitOne(1000, false);
            Assert.AreEqual(updatedFilePath, path);
        }

        [Test]
        public void StopObservingFolderTest()
        {
            string changedFileName = null;
            var manualEvent = new ManualResetEvent(false);
            var fileTree = new FileTree();
            fileTree.FileChanged += path =>
            {
                changedFileName = path;
                manualEvent.Set();
            };
            string directoryToObserve = _rootDirectory;
            string fileFullPath = _tmpFileName;
            fileTree.AddDirectory(directoryToObserve);
            fileTree.TryRemoveDirectory(directoryToObserve);
            File.AppendAllText(fileFullPath, "!");

            manualEvent.WaitOne(500, false);
            Assert.AreEqual(null, changedFileName);
        }
    }
}