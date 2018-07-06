using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace YAOJ.Models
{
    public class Problem
    {
        [DisplayName("Problem ID")]
        public string ProblemID { get; set; }

        [Required]
        [DisplayName("Problem Name")]
        public string Name { get; set; }

        [Required]
        [DisplayName("Problem Description")]
        public string Description { get; set; }

        [Required]
        [DisplayName("Data Format")]
        public string Format { get; set; }

        [Required]
        [DisplayName("Sample Input")]
        public string SampleInput { get; set; }

        [Required]
        [DisplayName("Sample Output")]
        public string SampleOutput { get; set; }
        
        [DisplayName("Notes")]
        public string Note { get; set; }
        
        [DisplayName("Problem Limitation")]
        public string Limitation { get; set; }

        public byte[] Data { get; set; }

        [DisplayName("Data Hash")]
        public string DataHash { get; set; }
    }
}
