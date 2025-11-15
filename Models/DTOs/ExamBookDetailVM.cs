using System;
using System.Collections.Generic;

namespace 專題MVC修正.Models.DTOs
{
    public class ExamBookDetailRowVM
    {
        public int No { get; set; }
        public string QuestionText { get; set; }
        public string StdAns { get; set; }
        public string CorrectAns { get; set; }
        public bool IsCorrect { get; set; }
    }

    public class ExamBookDetailVM
    {
        public int ExamId { get; set; }
        public string ExamName { get; set; }

        public int TotalQuestions { get; set; }
        public int CorrectCount { get; set; }
        public double? TotalScore { get; set; }

        public IList<ExamBookDetailRowVM> Rows { get; set; }
    }
}
