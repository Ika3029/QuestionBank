using System;
using System.Collections.Generic;
using System.Data.Entity; // 為了 DbContext.Set<T>()
// 命名空間請用你的實際命名空間
namespace 專題MVC修正.Models
{
    // ========= ExamMaster 的易讀輔助 =========
    public partial class ExamMaster
    {
        /// <summary>快速建立一筆測驗（名稱/起迄日期）。</summary>
        public ExamMaster(string examName, DateTime? sDate = null, DateTime? eDate = null)
            : this() // 叫用 EDMX 產生的無參建構子
        {
            this.ExamName = examName;
            // 不管屬性是 DateTime 或 DateTime? 都可賦值（運算後型別為 DateTime）
            this.ExamSDate = sDate ?? DateTime.Now;
            this.ExamEDate = eDate ?? DateTime.Now;
            // 若你的屬性叫 ExamDuraionTime（少 t），請在 Controller 另外指定
        }

        /// <summary>先暫存要新增的明細，最後統一交給 DbContext 追蹤。</summary>
        public IList<ExamDetail> __PendingDetails { get; } = new List<ExamDetail>();

        /// <summary>新增一筆明細（只用純數值 FK，避免導航屬性狀態混亂）。</summary>
        public ExamDetail AddDetail(
            string qMode,
            int mqbpk,
            float score = 1f,
            int? teamPk = null,
            int? qClass = null,
            int? sortOrder = null)
        {
            var det = new ExamDetail
            {
                // ExamID 由儲存時決定；關聯用導航屬性指回自己即可
                ExamMaster = this,
                ExamQMode = qMode,
                ExamMQBPK = mqbpk,
                // 下面三行用 ?? 轉成 int，兼容你的屬性是 int 或 int?
                ExamMQBTeamPK = teamPk ?? default(int),
                ExamQClass = qClass ?? default(int),
                SortOrder = sortOrder ?? default(int),
                ExamDefaultScore = score
            };
            __PendingDetails.Add(det);
            return det;
        }
    }

    // ========= MQBTeam 的易讀輔助 =========
    public partial class MQBTeam
    {
        public MQBTeam(string yn = "Y", string content = null, int? qClass = null) : this()
        {
            this.MQBTeamYN = yn;
            this.MQBTeamContent = content;
            this.QClass = qClass ?? this.QClass; // 若屬性為 int? 可直接賦值；若是 int 則保持原值
        }

        public IList<MoodQuestionBank> __PendingQuestions { get; } = new List<MoodQuestionBank>();

        public MoodQuestionBank AddQuestion(
            string content,
            string ans,
            int sort = 0,
            int? qClass = null,
            int? qType = null,
            string optA = null,
            string optB = null,
            string optC = null,
            string optD = null)
        {
            var q = new MoodQuestionBank
            {
                MQBTeam = this,
                MQBSort = sort,
                // 同樣用 ??，兼容 int / int?
                QClass = qClass ?? default(int),
                QType = qType ?? default(int),
                QContent = content,
                QOptionA = optA,
                QOptionB = optB,
                QOptionC = optC,
                QOptionD = optD,
                QAns = ans
            };
            __PendingQuestions.Add(q);
            return q;
        }
    }

    public partial class MoodQuestionBank
    {
        public MoodQuestionBank(string content, string ans) : this()
        {
            this.QContent = content;
            this.QAns = ans;
        }
    }

    // ========= DbContext 小幫手（不依賴 DbSet 名稱，統一用 Set<T>()） =========
    public static class DbHelpers
    {
        /// <summary>新增 ExamMaster 以及暫存的明細，回傳 ExamID。</summary>
        public static int SaveNewExam(this MQBEntities db, ExamMaster exam)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (exam == null) throw new ArgumentNullException(nameof(exam));

            db.Set<ExamMaster>().Add(exam);
            foreach (var d in exam.__PendingDetails)
                db.Set<ExamDetail>().Add(d);

            db.SaveChanges();
            return exam.ExamID;
        }

        /// <summary>新增 MQBTeam 以及暫存的題目，回傳 MQBTeamPK。</summary>
        public static int SaveNewTeam(this MQBEntities db, MQBTeam team)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (team == null) throw new ArgumentNullException(nameof(team));

            db.Set<MQBTeam>().Add(team);
            foreach (var q in team.__PendingQuestions)
                db.Set<MoodQuestionBank>().Add(q);

            db.SaveChanges();
            return team.MQBTeamPK;
        }
    }
}
