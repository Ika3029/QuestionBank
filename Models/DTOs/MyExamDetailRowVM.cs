using System;

namespace 專題MVC修正.Models.DTOs
{
    public class MyExamDetailRowVM
    {
        public int No { get; set; }              
        public int? ExamDetPK { get; set; }

        public double Score { get; set; }

        public string QContent { get; set; }
        public string QOptionA { get; set; }
        public string QOptionB { get; set; }
        public string QOptionC { get; set; }
        public string QOptionD { get; set; }

        public string CorrectAns { get; set; }   
        public string StdAns { get; set; }       
        public bool IsCorrect { get; set; }      
    }
}
