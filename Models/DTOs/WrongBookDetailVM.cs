using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace 專題MVC修正.Models.DTOs
{
    public class WrongBookDetailVM
    {
        public int MQBPK { get; set; }

        public string QuestionText { get; set; }

        public string OptionA { get; set; }
        public string OptionB { get; set; }
        public string OptionC { get; set; }
        public string OptionD { get; set; }

        public string StdAns { get; set; }      
        public string CorrectAns { get; set; }  
    }
}
