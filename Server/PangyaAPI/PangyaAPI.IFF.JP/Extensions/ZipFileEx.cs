using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
namespace PangyaAPI.IFF.JP.Extensions
{
    public class ZipFileEx : IDisposable
    {
        private ZipArchive _archive;
        private MemoryStream _stream;
        public ZipArchive GetZip() => _archive;

        public ZipFileEx()
        {
            _stream = new MemoryStream();
            _archive = new ZipArchive(_stream, ZipArchiveMode.Create, leaveOpen: true);
        }

        public ZipFileEx(Stream stream)
        {
            _stream = new MemoryStream();
            stream.Seek(0, SeekOrigin.Begin);
            stream.CopyTo(_stream);
            _archive = new ZipArchive(_stream, ZipArchiveMode.Update, leaveOpen: true);
        }

        public ZipFileEx(string filePath, ZipArchiveMode mode = ZipArchiveMode.Read)
        {
            CheckFile(filePath);
            _stream = new MemoryStream();
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                fileStream.CopyTo(_stream);
            }
            _stream.Seek(0, SeekOrigin.Begin); // Garante o ponteiro no início

            // MODO READ resolve o problema de múltiplas aberturas
            _archive = new ZipArchive(_stream, mode, leaveOpen: true);
        }

        public bool CheckFile(string filePath)
        {
            byte[] headerBytes = new byte[2];
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                fileStream.Read(headerBytes, 0, 2);
            }

            // Converta os bytes para uma string usando a codificação UTF-8
            string headerString = Encoding.UTF8.GetString(headerBytes);

            if (headerString != "PK")
            {
                throw new NotSupportedException("The given IFF file is a ZIP file, please unpack it before attempting to parse it");
            }
            return true;
        }

        public bool CheckFile(byte[] fileData)
        {
            byte[] headerBytes = new byte[2];
            Array.Copy(fileData, 0, headerBytes, 0, 2);

            // Converta os bytes para uma string usando a codificação UTF-8
            string headerString = Encoding.UTF8.GetString(headerBytes);

            if (headerString == "PK")
            {
                throw new NotSupportedException("The given IFF file is a ZIP file, please unpack it before attempting to parse it");
            }
            return true;
        }

        public async Task AddFileAsync(string fileName, Stream stream)
        {
            var entry = _archive.CreateEntry(fileName);
            using (var entryStream = entry.Open())
            {
                await stream.CopyToAsync(entryStream);
            }
        }

        public void AddFile(string fileName, Stream stream)
        {
            var entry = _archive.CreateEntry(fileName);
            using (var entryStream = entry.Open())
            {
                stream.CopyTo(entryStream);
            }
        }

        public void Update(string fileName, Stream stream)
        {
            var entry = _archive.GetEntry(fileName);
            entry.Delete();
            // Criar um arquivo temporário e copiar o conteúdo do stream para ele
            var tempFilePath = Path.GetTempFileName();
            using (var fileStream = File.OpenWrite(tempFilePath))
            {
                stream.CopyTo(fileStream);
            }
            _archive.CreateEntryFromFile(tempFilePath, fileName);
        }

        public async Task AddFileAsync(string fileName, string filePath)
        {
            var entry = _archive.CreateEntry(fileName);
            using (var entryStream = entry.Open())
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open))
                {
                    await fileStream.CopyToAsync(entryStream);
                }
            }
        }

        public void AddFile(string fileName, string filePath)
        {
            var entry = _archive.CreateEntry(fileName);
            using (var entryStream = entry.Open())
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open))
                {
                    fileStream.CopyTo(entryStream);
                }
            }
        }

        public void ExtractToDirectory(string directory)
        {
            _archive.ExtractToDirectory(directory);
        }

        public void ExtractFile(string entry, string directory)
        {
            GetEntry(entry).ExtractToFile(directory);
        }

        public void Save(Stream stream)
        {
            DisposeArchive();
            _stream.Seek(0, SeekOrigin.Begin);
            _stream.CopyTo(stream);
            stream.Seek(0, SeekOrigin.Begin);
        }

        public void Save(string filePath)
        {
            DisposeArchive();
            _stream.Seek(0, SeekOrigin.Begin);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                _stream.CopyTo(fileStream);
            }
        }

        public async Task SaveAsync(Stream stream)
        {
            DisposeArchive();
            _stream.Seek(0, SeekOrigin.Begin);
            await _stream.CopyToAsync(stream);
            stream.Seek(0, SeekOrigin.Begin);
        }

        public async Task SaveAsync(string filePath)
        {
            DisposeArchive();
            _stream.Seek(0, SeekOrigin.Begin);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await _stream.CopyToAsync(fileStream);
            }
        }

        public ZipArchiveEntry GetEntry(string iffName)
        {
            foreach (var entry in _archive.Entries)
            {
                if (entry.FullName.Equals(iffName, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }
            return null;
        }

        public MemoryStream GetEntryStream(string iffName)
        {
            foreach (var entry in _archive.Entries)
            {
                if (entry.FullName.Equals(iffName, StringComparison.OrdinalIgnoreCase))
                {
                    using (var stream = entry.Open())
                    {
                        var memoryStream = new MemoryStream();
                        stream.CopyTo(memoryStream);
                        return memoryStream;
                    }
                }
            }
            return new MemoryStream(new byte[0]);
        }

        public byte[] GetEntryBytes(string iffName)
        {
            foreach (var entry in _archive.Entries)
            {
                if (entry.FullName.Equals(iffName, StringComparison.OrdinalIgnoreCase))
                {
                    using (var stream = entry.Open())
                    {
                        var memoryStream = new MemoryStream();
                        stream.CopyTo(memoryStream);
                        var fileData = memoryStream.ToArray();
                        CheckFile(fileData);
                        return fileData;
                    }
                }
            }
            return new byte[0];
        }

        private void DisposeArchive()
        {
            _archive.Dispose();
            _archive = null;
        }

        public void Dispose()
        {
            if (_archive != null)
            {
                _archive.Dispose();
                _archive = null;
            }

            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
        }
    }
}