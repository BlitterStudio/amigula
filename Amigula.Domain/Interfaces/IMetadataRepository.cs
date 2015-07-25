using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Amigula.Domain.Interfaces
{
    public interface IMetadataRepository
    {
        string GetGameMetadata(string gameTitle);
        string GetGenre(string gameTitle);
        string GetPublisher(string gameTitle);
        string GetYear(string gameTitle);
    }
}
