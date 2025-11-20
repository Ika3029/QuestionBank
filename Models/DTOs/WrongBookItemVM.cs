using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace 專題MVC修正.Models.DTOs
{
    public class WrongBookItemVM
    {
        public int MQBPK { get; set; }              // 題庫題目PK
        public string QuestionText { get; set; }    // 題目內容
        public string StdAns { get; set; }          // 我最後一次的答案
        public string CorrectAns { get; set; }      // 正確答案
        public int WrongCount { get; set; }         // 錯了幾次
        public DateTime? LastAnsTime { get; set; }  // 最後作答時間
        public int ExamID { get; set; }
    }
}
