using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace 專題MVC修正.Models.DTOs
{
    public class WrongBookItemVM
    {
        public int MQBPK { get; set; }              
        public string QuestionText { get; set; }    
        public string StdAns { get; set; }          
        public string CorrectAns { get; set; }      
        public int WrongCount { get; set; }         
        public DateTime? LastAnsTime { get; set; }  
        public int ExamID { get; set; }
    }
}
