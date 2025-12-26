using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace 專題MVC修正.Models.DTOs
{
    public class ExamBookSummaryVM
    {
        public int? ExamID { get; set; }

        public int TotalQuestions { get; set; }
        public int CorrectCount { get; set; }
        public int? ExamStdPK { get; set; }
        public string StdName { get; set; }
        
        public double? Score { get; set; }
        
        public long AttemptTicks { get; set; }
        
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

}
