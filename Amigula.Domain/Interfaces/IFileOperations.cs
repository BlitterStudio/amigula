using Amigula.Domain.Classes;

namespace Amigula.Domain.Interfaces
{
    public interface IFileOperations
    {
        OperationResult Delete(string filename);
        OperationResult CopyFileInPlace(string filename, string destination);
        OperationResult RenameFile(string oldFilename, string newFilename);
        bool FilenameExists(string filenameFullPath);
    }
}