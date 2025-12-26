using System;
using System.Collections.Generic;
using System.Data.Entity; 
using 專題MVC修正.Models; 

namespace 專題MVC修正.Models.DTOs
{
    
        
     public class ExamDselectlist
     {
            public int MQBClassPK { get; set; }
            public string MQBClassName1 { get; set; }
            public string MQBTeamContent { get; set; }
            public string MQBTeamYN { get; set; }
            public int MQBSort { get; set; }
            public string MQBChapter { get; set; }
            public string MQBSession { get; set; }
            public string QType { get; set; }
            public string QContent { get; set; }
            public string QOptionA { get; set; }
            public string QOptionB { get; set; }
            public string QOptionC { get; set; }
            public string QOptionD { get; set; }
            public double ExamDefaultScore { get; set; }
            public int ExamDetPK { get; set; }
            public int MQBPK { get; set; }
            public int MQBTeamPK { get; set; }
            public int ExamID { get; set; }
     }

    public class ExamDetailRowVM
    {
        public int ExamDetPK { get; set; }   
        public double? ExamDefaultScore { get; set; } 
        public int? ExamMQBPK { get; set; }   
        public string QContent { get; set; }
        public string QAns { get; set; }
    }

    public class ExamDetailsVM
    {
        public ExamMaster Exam { get; set; }
        public List<ExamDetailRowVM> Details { get; set; }
    }

}
