using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Tiss_MindRadar.Models;

namespace Tiss_MindRadar.Controllers
{
    public class UserAccountController : Controller
    {
        private TISS_MindRadarEntities _db = new TISS_MindRadarEntities(); //資料庫

        #region 註冊帳號
        public ActionResult Register()
        {
            ViewBag.Teams = _db.Team.ToList(); // 提供隊伍選單
            return View();
        }

        [HttpPost]
        public JsonResult Register(string Jobcode, string UserName, string pwd, string Email, int Age, int TeamID)
        {
            try
            {
                if (Jobcode.Length > 20)
                {
                    return Json(new { success = false, message = "帳號長度不能超過 20 個字！" });
                }

                // 只允許中文和英文，不允許數字和符號
                var jobcodeRegex = new Regex(@"^[\u4e00-\u9fa5a-zA-Z]+$");
                if (!jobcodeRegex.IsMatch(Jobcode))
                {
                    return Json(new { success = false, message = "帳號只能包含中文和英文，不能包含數字或符號！" });
                }

                if (_db.Users.Any(u => u.Jobcode == Jobcode))
                {
                    return Json(new { success = false, message = "該帳號已存在。" });
                }

                var hashedPwd = ComputeSha256Hash(pwd); // 密碼加密

                // 取得 TeamName
                var selectedTeam = _db.Team.FirstOrDefault(t => t.TeamID == TeamID);
                if (selectedTeam == null)
                {
                    return Json(new { success = false, message = "所選隊伍不存在！" });
                }

                var newUser = new Users
                {
                    Jobcode = Jobcode,
                    UserName = UserName,
                    Passwords = hashedPwd,
                    Email = Email,
                    CreatedDate = DateTime.Now,
                    TeamName = selectedTeam.TeamName
                };

                _db.Users.Add(newUser);
                _db.SaveChanges();

                var newUserProfile = new UserProfile
                {
                    UserID = newUser.UserID,
                    Age = Age,
                    TeamID = TeamID
                };

                _db.UserProfile.Add(newUserProfile);
                _db.SaveChanges();

                return Json(new { success = true, message = "帳號註冊成功。" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "發生錯誤：帳號註冊失敗！" + ex.Message });
            }
        }
        #endregion

        #region 密碼加密方法
        private static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        #endregion

        #region 登入
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string Jobcode, string pwd)
        {
            try
            {
                var hashedPwd = ComputeSha256Hash(pwd);

                var user = _db.Users.FirstOrDefault(u => u.Jobcode == Jobcode && u.Passwords == hashedPwd);

                if (user != null)
                {
                    user.LastLoginDate = DateTime.Now; //記錄最後登入時間
                    _db.SaveChanges();

                    var userProfile = _db.UserProfile.FirstOrDefault(up => up.UserID == user.UserID);
                    var teamName = userProfile != null ? _db.Team.FirstOrDefault(t => t.TeamID == userProfile.TeamID)?.TeamName : "未指定";

                    Session["UserID"] = user.UserID;
                    Session["UserName"] = user.UserName;
                    Session["Age"] = userProfile?.Age ?? 0;
                    Session["TeamName"] = teamName;

                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "帳號或密碼錯誤！" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"登入失敗：{ex.Message}" });
            }
        }
        #endregion

        #region 登出
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }
        #endregion

        #region 忘記密碼
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public JsonResult ResetPassword(string jobCode, string newPassword)
        {
            try
            {
                var user = _db.Users.FirstOrDefault(u => u.Jobcode == jobCode);
                if (user == null)
                {
                    return Json(new { success = false, message = "該帳號不存在，請確認後再試。" });
                }

                if (!ValidatePasswordStrength(newPassword))
                {
                    return Json(new { success = false, message = "密碼必須至少包含6碼，並包含大小寫字母和數字。" });
                }

                user.Passwords = ComputeSha256Hash(newPassword);
                _db.SaveChanges();

                return Json(new { success = true, message = "密碼已成功更新。" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"發生錯誤：{ex.Message}" });
            }
        }
        #endregion

        #region 密碼規則驗證方法
        private bool ValidatePasswordStrength(string password)
        {
            var passwordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$");
            return passwordRegex.IsMatch(password);
        }
        #endregion
    }
}