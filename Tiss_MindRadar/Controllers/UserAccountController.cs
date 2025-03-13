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
        public JsonResult Register(string Jobcode, string UserName, string pwd, string Email, int? Age, int? TeamID, string Role, string InviteCode, string Gender)
        {
            try
            {
                var validationMessage = ValidateRegistrationInputs(Jobcode, Email, Role, InviteCode);
                if (!string.IsNullOrEmpty(validationMessage))
                {
                    return Json(new { success = false, message = validationMessage });
                }

                var hashedPwd = ComputeSha256Hash(pwd);

                // 如果是選手，檢查隊伍是否存在
                string teamName = null;
                if (Role == "Player")
                {
                    var selectedTeam = _db.Team.FirstOrDefault(t => t.TeamID == TeamID);
                    if (selectedTeam == null)
                    {
                        return Json(new { success = false, message = "所選隊伍不存在" });
                    }
                    teamName = selectedTeam.TeamName;
                }

                if (_db.Users.Any(u => u.Jobcode == Jobcode))
                {
                    return Json(new { success = false, message = "帳號已存在，請使用其他帳號" });
                }

                // 建立使用者
                var newUser = new Users
                {
                    Jobcode = Jobcode,
                    UserName = UserName,
                    Passwords = hashedPwd,
                    Email = Email,
                    CreatedDate = DateTime.Now,
                    TeamName = teamName,
                    TeamID = Role == "Player" ? TeamID : null
                };

                _db.Users.Add(newUser);
                _db.SaveChanges();

                var newUserProfile = new UserProfile
                {
                    UserID = newUser.UserID,
                    Age = Role == "Player" ? Age : null,
                    TeamID = Role == "Player" ? TeamID : null,
                    Role = Role,
                    InviteCode = InviteCode,
                    Gender = Gender,
                    IsVerified = true
                };

                _db.UserProfile.Add(newUserProfile);
                _db.SaveChanges();

                return Json(new { success = true, message = "帳號註冊成功" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "帳號註冊失敗：" + ex.Message });
            }
        }
        #endregion

        #region 獨立驗證方法
        private string ValidateRegistrationInputs(string Jobcode, string Email, string Role, string InviteCode)
        {
            //帳號長度驗證
            if (Jobcode.Length > 20)
            {
                return "帳號長度不能超過20個字";
            }

            //帳號格式驗證(只能中文和英文)
            var jobcodeRegex = new Regex(@"^[\u4e00-\u9fa5a-zA-Z]+$");
            if (!jobcodeRegex.IsMatch(Jobcode))
            {
                return "帳號只能包含中文和英文，不能包含數字或符號！";
            }

            //帳號重複檢查
            if (_db.Users.Any(u => u.Jobcode == Jobcode))
            {
                return "該帳號已存在";
            }

            if (Role != "Player" && Role != "Consultant" && Role != "Referee")
            {
                return "角色選擇錯誤，請重新選擇";
            }

            //角色檢查
            if (Role != "Player" && Role != "Consultant" && Role != "Referee")
            {
                return "角色選擇錯誤，請重新選擇";
            }

            //限制訪談員Email
            if (Role == "Consultant" && !Email.EndsWith("@tiss.org.tw"))
            {
                return "註冊訪談員帳號，需使用運科中心電子郵件帳號";
            }

            //邀請碼驗證
            const string consultantInviteCode = "Tiss@dmin!@#";
            const string coachInviteCode = "Referee@123";

            if ((Role == "Consultant" && InviteCode != consultantInviteCode) ||
                (Role == "Referee" && InviteCode != coachInviteCode))
            {
                return "無效的驗證碼！";
            }

            return ""; //透過所有驗證
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

        #region 密碼規則驗證方法
        private bool ValidatePasswordStrength(string password)
        {
            var passwordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$");
            return passwordRegex.IsMatch(password);
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
                    user.LastLoginDate = DateTime.Now; // 記錄最後登入時間
                    _db.SaveChanges();

                    var userProfile = _db.UserProfile.FirstOrDefault(up => up.UserID == user.UserID);
                    var teamName = userProfile != null ? _db.Team.FirstOrDefault(t => t.TeamID == userProfile.TeamID)?.TeamName : "未指定";

                    string userRole = userProfile?.Role ?? "Player"; //預設為選手

                    Session["UserID"] = user.UserID;
                    Session["UserName"] = user.UserName;
                    Session["UserRole"] = userRole; // 設定角色
                    Session["Age"] = userProfile?.Age ?? 0;
                    Session["TeamName"] = teamName;

                    // 根據角色設定跳轉的 URL
                    string redirectUrl;

                    switch (userRole)
                    {
                        case "Consultant":
                            redirectUrl = "/TeamRawData/ChooseTeamState";
                            break;
                        case "Referee":
                            redirectUrl = "/RefereeSurvey/SmoothExperienceSurvey"; // 新增裁判專用跳轉
                            break;
                        default:
                            redirectUrl = "/Survey/MentalPhysicalState"; // 預設跳轉
                            break;
                    }
                    return Json(new { success = true, redirectUrl });
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
    }
}