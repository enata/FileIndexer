using FileIndexer.Core.Interfaces;
using FileIndexer.Indexer;
using NUnit.Framework;
using Rhino.Mocks;
using System.Linq;

namespace FileIndexer.Tests.Indexer
{
    [TestFixture]
    public sealed class FileIndexManagerTests
    {
        private FileIndexManager InitializeFileIndexManager(IFileManager fileManager)
        {
            var logger = MockRepository.GenerateStub<ILogger>();
            var indexerFactory = MockRepository.GenerateStub<IFileIndexFactory>();
            var index = MockRepository.GenerateStub<IFileIndex>();
            indexerFactory.Stub(i => i.BuildEmptyIndex(Arg<IFileManager>.Is.Anything))
                          .Return(index);

            var fileIndexManager = new FileIndexManager(fileManager, logger, indexerFactory);
            return fileIndexManager;
        }   

        [Test]
        public void AddDirectoriesTest()
        {
            var fileManager = new FileManagerStub();
            FileIndexManager fileIndexManager = InitializeFileIndexManager(fileManager);

            const string directoryPath = "dir1";
            fileIndexManager.AddDirectories(directoryPath);

            Assert.AreEqual(1, fileManager.Directories.Count);
            Assert.IsTrue(fileManager.Directories.Contains(directoryPath));
        }

        [Test]
        public void AddFilesTest()
        {
            var fileManager = new FileManagerStub();
            FileIndexManager fileIndexManager = InitializeFileIndexManager(fileManager);

            const string filePath = "file1";
            fileIndexManager.AddFiles(filePath);

            Assert.AreEqual(1, fileManager.Files.Count);
            Assert.IsTrue(fileManager.Files.Contains(filePath));
        }

        [Test]
        public void TryRemoveDirectoryTest()
        {
            const string directoryName1 = "dir1";
            const string directoryName2 = "dir2";
            var fileManager = new FileManagerStub(new[] {directoryName1, directoryName2}, Enumerable.Empty<string>());
            FileIndexManager fileIndexManager = InitializeFileIndexManager(fileManager);

            bool result = fileIndexManager.TryRemoveDirectory(directoryName1);

            Assert.IsTrue(result);
            Assert.AreEqual(1, fileManager.Directories.Count);
            Assert.IsTrue(fileManager.Directories.Contains(directoryName2));
        }

        [Test]
        public void TryRemoveFileTest()
        {
            const string filename1 = "f1";
            const string filename2 = "f2";
            var fileManager = new FileManagerStub(Enumerable.Empty<string>(), new[] {filename1, filename2});
            FileIndexManager fileIndexManager = InitializeFileIndexManager(fileManager);

            bool result = fileIndexManager.TryRemoveFile(filename1);

            Assert.IsTrue(result);
            Assert.AreEqual(1, fileManager.Files.Count);
            Assert.IsTrue(fileManager.Files.Contains(filename2));
        }
    }
}