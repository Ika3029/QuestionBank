using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace 專題MVC修正.Models.DTOs
{
    public class ExamReportVM
    {
        public int ExamID { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectCount { get; set; }
        public double Accuracy { get; set; }

        public Dictionary<string, int> ClassCorrect { get; set; }
        public Dictionary<string, int> ClassTotal { get; set; }

        public Dictionary<string, int> TeamCorrect { get; set; }
        public Dictionary<string, int> TeamTotal { get; set; }

        public List<int?> WrongQuestionList { get; set; }
    }
}