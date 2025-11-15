using System;

namespace 專題MVC修正.Models.DTOs
{
    public class MyExamDetailRowVM
    {
        public int No { get; set; }              // 題號(1-based)
        public int? ExamDetPK { get; set; }

        public double Score { get; set; }

        public string QContent { get; set; }
        public string QOptionA { get; set; }
        public string QOptionB { get; set; }
        public string QOptionC { get; set; }
        public string QOptionD { get; set; }

        public string CorrectAns { get; set; }   // 標準答案
        public string StdAns { get; set; }       // 學生作答
        public bool IsCorrect { get; set; }      // 是否答對
    }
}
