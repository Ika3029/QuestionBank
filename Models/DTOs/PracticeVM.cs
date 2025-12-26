using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace 專題MVC修正.Models.DTOs
{
    public class PracticeVM
    {
        public int MQBPK { get; set; }

        public int ClassId { get; set; }      
        public string ClassName { get; set; } 

        public string Content { get; set; }
        public string ImgContentDataUrl { get; set; }

        public string OptionA { get; set; }
        public string OptionB { get; set; }
        public string OptionC { get; set; }
        public string OptionD { get; set; }
        public string ImgOptionADataUrl { get; set; }
        public string ImgOptionBDataUrl { get; set; }
        public string ImgOptionCDataUrl { get; set; }
        public string ImgOptionDDataUrl { get; set; }

        public string CorrectAnswer { get; set; }
        public string Explanation { get; set; }
        public string ImgExplanationDataUrl { get; set; }

        public string SelectedAnswer { get; set; }
        public bool IsAnswered { get; set; }
        public bool IsCorrect { get; set; }
    }
}
