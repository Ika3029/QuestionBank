using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace 專題MVC修正.Models.DTOs
{
    public class Std
    {
        [Key]
        public int StdPK { get; set; } // 主索引

        [Required]
        [Display(Name = "學校代碼")]
        [StringLength(4)]
        public string StdSchoolID { get; set; } // 例：1006、0036、0042

        [Required]
        [Display(Name = "科系代碼")]
        [StringLength(6)]
        public string StdDepID { get; set; } // 例：340401、140806、480109

        [Required]
        [Display(Name = "學號")]
        [StringLength(20)]
        public string StdID { get; set; }

        [Required]
        [Display(Name = "學生姓名")]
        [StringLength(20)]
        public string StdName { get; set; }

        [Required]
        [Display(Name = "性別")]
        [StringLength(1)]
        public string StdGender { get; set; } // M 或 F
    }
}