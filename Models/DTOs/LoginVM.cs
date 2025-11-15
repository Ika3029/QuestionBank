using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace 專題MVC修正.Models.DTOs
{
    public class LoginVM
    {
        [Required(ErrorMessage = "請輸入帳號")]
        [Display(Name = "帳號（StdPK）")]
        public int StdPK { get; set; }

        [Required(ErrorMessage = "請輸入密碼")]
        [Display(Name = "密碼（預設同 StdPK）")]
        [DataType(DataType.Password)]
        public int Password { get; set; }
    }
}