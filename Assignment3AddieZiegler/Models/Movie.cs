using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Assignment3AddieZiegler.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string IMDBUrl { get; set; }
        public string Genre { get; set; }
        public int YearOfRelease { get; set; }
        public string IMBDUrl { get; set; }
        [DataType(DataType.Upload)]
        [DisplayName("Poster")]
        public byte[]? Poster { get; set; }
    }
}
