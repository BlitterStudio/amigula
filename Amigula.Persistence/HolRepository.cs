using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amigula.Domain.Interfaces;
using HtmlAgilityPack;

namespace Amigula.Persistence
{
    public class HolRepository : IMetadataRepository
    {
        public string GetGameMetadata(string gameTitle)
        {
            throw new NotImplementedException();
        }

        public string GetGenre(string gameTitle)
        {
            throw new NotImplementedException();
        }

        public string GetPublisher(string gameTitle)
        {
            throw new NotImplementedException();
        }

        public string GetYear(string gameTitle)
        {
            throw new NotImplementedException();
        }

        private string ExtractGenre(HtmlDocument document)
        {
            // XPath for Genre: //table[@width='100%']/tr[12]/td/table/tr[2]/td[2]/a
            var fetchedGenre =
                document.DocumentNode.SelectSingleNode("//table[@width='100%']/tr[13]/td[1]/table/tr[2]/td[2]/a")
                    .InnerText;
            return fetchedGenre;
        }

        private string ExtractPublisher(HtmlDocument document)
        {
            // XPath for Publisher: //table[@width='100%']/tr[2]/td[4]/a
            var fetchedPublisher = "";
            if (document.DocumentNode.SelectSingleNode("//table[@width='100%']/tr[2]/td[4]/a") != null)
                fetchedPublisher =
                    document.DocumentNode.SelectSingleNode("//table[@width='100%']/tr[2]/td[4]/a").InnerText;
            return fetchedPublisher;
        }

        private string ExtractYear(HtmlDocument document)
        {
            // XPath for Year: //table[@width='100%']/tr[1]/td[2]/a
            var fetchedYear = "";
            if (document.DocumentNode.SelectSingleNode("//table[@width='100%']/tr[1]/td[2]/a") != null)
                fetchedYear = document.DocumentNode.SelectSingleNode("//table[@width='100%']/tr[1]/td[2]/a").InnerText;
            //MessageBox.Show("The game's Year is: " + fetchedYear);
            return fetchedYear;
        }
    }
}
