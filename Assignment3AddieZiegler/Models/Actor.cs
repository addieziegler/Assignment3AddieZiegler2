using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace Assignment3AddieZiegler.Models
{
    public class Actor
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Gender { get; set; }
        public int Age {  get; set; }
        public string IMBDUrl { get; set; }
        [DataType(DataType.Upload)] [DisplayName("Photo")]
        public byte[]? Photo { get; set; }
        //public List<MovieActor> MovieActors { get; set; }
    }
}
