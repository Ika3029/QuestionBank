using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace 專題MVC修正.Models.DTOs
{
    public class MyExamRecordVM
    {
        public int? ExamID { get; set; }

        public int TotalQuestions { get; set; }
        public int CorrectCount { get; set; }
        public double TotalScore { get; set; }
        public DateTime FinishTime { get; set; }

        
        public long AttemptTicks { get; set; }  
    }
}