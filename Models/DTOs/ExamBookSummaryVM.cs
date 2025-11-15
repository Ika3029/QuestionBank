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

        // ★ 修正：float? 或 double? 都可以
        public double? Score { get; set; }

        // ★ 修正：允許為 null
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

}
