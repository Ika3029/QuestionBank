using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace 專題MVC修正.Models.std
{
    public class MQBContext : DbContext
    {
        public MQBContext() : base("name=MQBContext") // 對應到 Web.config 裡的連線字串名稱
        {
        }

        public DbSet<Std> Stds { get; set; } // 對應學生資料表
    }
}