using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace YAOJ.Models
{
    public static class Extensions
    {
        /// <summary>
        ///     A generic extension method that aids in reflecting 
        ///     and retrieving any attribute that is applied to an `Enum`.
        /// </summary>
        public static TAttribute GetAttribute<TAttribute>(this Enum enumValue)
                where TAttribute : Attribute
        {
            return enumValue.GetType()
                            .GetMember(enumValue.ToString())
                            .First()
                            .GetCustomAttribute<TAttribute>();
        }

        public static string GetDisplayName(this Enum enumValue)
        {
            return enumValue.GetAttribute<DisplayAttribute>().Name;
        }
    }

    public enum Status
    {
        [Display(Name = "Time Limit Exceeded")]
        TLE,
        [Display(Name = "Memory Limit Exceeded")]
        MLE,
        [Display(Name = "Runtime Error")]
        RE,
        [Display(Name = "Compilation Error")]
        CE,
        [Display(Name = "Accepted")]
        AC,
        [Display(Name = "Wrong Answer")]
        WA,
        [Display(Name = "Not Available")]
        NA
    }

    public class Record
    {
        [Key]
        [DisplayName("Record ID")]
        public int RecordID { get; set; }

        [DisplayName("Status")]
        public Status Status { get; set; }

        [DisplayName("User")]
        public User User { get; set; }
        
        [DisplayName("Problem ID")]
        public Problem Problem { get; set; }

        public int UserID { get; set; }
        public string ProblemID { get; set; }

        [DisplayName("Language")]
        public string Language { get; set; }

        [DisplayName("Source Code")]
        public string SourceCode { get; set; }

        [DisplayName("Used Time")]
        public double UsedTime { get; set; }

        [DisplayName("Used Memory")]
        public double UsedMemory { get; set; }
        
        [DisplayName("Judge Text")]
        public string JudgeText { get; set; }
    }
}
