using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amigula.Domain.DTO
{
    public class GamesDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Genre { get; set; }
        public int Disks { get; set; }
        public string Publisher { get; set; }
        public int Year { get; set; }
        public string UaeConfig { get; set; }
        public int TimesPlayed { get; set; }
        public DateTime? LastPlayed { get; set; }
        public string PathToFile { get; set; }
        public bool Favorite { get; set; }
    }
}
